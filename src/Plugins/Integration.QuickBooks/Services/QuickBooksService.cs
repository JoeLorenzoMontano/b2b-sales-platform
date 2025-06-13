using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Infrastructure.Caching;
using Integration.QuickBooks.Configuration;
using Integration.QuickBooks.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Integration.QuickBooks.Services
{
    /// <summary>
    /// Implementation of QuickBooks service
    /// </summary>
    public class QuickBooksService : IQuickBooksService
    {
        private readonly ISettingService _settingService;
        private readonly ICacheBase _cacheBase;
        private readonly ILogger<QuickBooksService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private QuickBooksSettings _settings;

        private const string SETTINGS_KEY = "QuickBooks.Settings";
        private const string OAUTH_BASE_URL = "https://oauth.platform.intuit.com/oauth2/v1";
        private const string API_BASE_URL_SANDBOX = "https://sandbox-quickbooks.api.intuit.com/v3/company/";
        private const string API_BASE_URL_PRODUCTION = "https://quickbooks.api.intuit.com/v3/company/";

        public QuickBooksService(
            ISettingService settingService,
            ICacheBase cacheBase,
            ILogger<QuickBooksService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _settingService = settingService;
            _cacheBase = cacheBase;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Get the QuickBooks settings
        /// </summary>
        public QuickBooksSettings GetSettings()
        {
            if (_settings != null)
                return _settings;

            _settings = _cacheBase.Get(SETTINGS_KEY, () =>
            {
                var settings = _settingService.LoadSetting<QuickBooksSettings>().GetAwaiter().GetResult();
                return settings;
            });

            return _settings;
        }

        /// <summary>
        /// Save the QuickBooks settings
        /// </summary>
        public async Task SaveSettingsAsync(QuickBooksSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            await _settingService.SaveSetting(settings);

            _settings = settings;
            await _cacheBase.RemoveAsync(SETTINGS_KEY);
        }

        /// <summary>
        /// Get authorization URL for OAuth2 flow
        /// </summary>
        public string GetAuthorizationUrl()
        {
            var settings = GetSettings();
            
            var queryParams = new Dictionary<string, string>
            {
                ["client_id"] = settings.ClientId,
                ["response_type"] = "code",
                ["redirect_uri"] = settings.RedirectUri,
                ["scope"] = "com.intuit.quickbooks.accounting openid profile email phone address",
                ["state"] = Guid.NewGuid().ToString()
            };

            var queryString = string.Join("&", queryParams.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"));
            return $"{OAUTH_BASE_URL}/authorize?{queryString}";
        }

        /// <summary>
        /// Exchange authorization code for tokens
        /// </summary>
        public async Task<TokenResponse> ExchangeAuthorizationCodeAsync(string code, string realmId)
        {
            var settings = GetSettings();
            
            var client = _httpClientFactory.CreateClient();
            
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", settings.RedirectUri)
            });
            
            // Add basic auth header
            var authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.ClientId}:{settings.ClientSecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            
            var response = await client.PostAsync($"{OAUTH_BASE_URL}/token", content);
            response.EnsureSuccessStatusCode();
            
            var responseString = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseString);
            
            settings.AccessToken = tokenResponse.AccessToken;
            settings.RefreshToken = tokenResponse.RefreshToken;
            settings.AccessTokenExpiresIn = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.AccessTokenExpiresIn).ToUnixTimeSeconds();
            settings.RealmId = realmId;
            
            await SaveSettingsAsync(settings);
            
            return tokenResponse;
        }

        /// <summary>
        /// Refresh the access token if expired
        /// </summary>
        public async Task<bool> RefreshTokenIfNeededAsync()
        {
            var settings = GetSettings();
            
            if (string.IsNullOrEmpty(settings.RefreshToken))
                return false;

            try
            {
                // If token is still valid, don't refresh
                if (!string.IsNullOrEmpty(settings.AccessToken) && 
                    settings.AccessTokenExpiresIn > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    return false;
                }

                _logger.LogInformation("Refreshing QuickBooks access token");
                
                var client = _httpClientFactory.CreateClient();
                
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", settings.RefreshToken)
                });
                
                // Add basic auth header
                var authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.ClientId}:{settings.ClientSecret}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
                
                var response = await client.PostAsync($"{OAUTH_BASE_URL}/token", content);
                response.EnsureSuccessStatusCode();
                
                var responseString = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonConvert.DeserializeObject<TokenResponse>(responseString);
                
                settings.AccessToken = tokenResponse.AccessToken;
                settings.RefreshToken = tokenResponse.RefreshToken;
                settings.AccessTokenExpiresIn = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.AccessTokenExpiresIn).ToUnixTimeSeconds();
                
                await SaveSettingsAsync(settings);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing QuickBooks token");
                return false;
            }
        }

        /// <summary>
        /// Send a GET request to QuickBooks API
        /// </summary>
        public async Task<T> GetAsync<T>(string endpoint)
        {
            if (!IsConnected())
                throw new Exception("QuickBooks is not connected");

            await RefreshTokenIfNeededAsync();
            
            var settings = GetSettings();
            var client = _httpClientFactory.CreateClient();
            
            // Set authorization header
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            // Determine base URL based on environment
            var baseUrl = settings.Environment.Equals("Production", StringComparison.InvariantCultureIgnoreCase) 
                ? API_BASE_URL_PRODUCTION 
                : API_BASE_URL_SANDBOX;
            
            var url = $"{baseUrl}{settings.RealmId}/{endpoint}";
            
            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(content);
        }

        /// <summary>
        /// Send a POST request to QuickBooks API
        /// </summary>
        public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest data)
        {
            if (!IsConnected())
                throw new Exception("QuickBooks is not connected");

            await RefreshTokenIfNeededAsync();
            
            var settings = GetSettings();
            var client = _httpClientFactory.CreateClient();
            
            // Set authorization header
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.AccessToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            
            // Determine base URL based on environment
            var baseUrl = settings.Environment.Equals("Production", StringComparison.InvariantCultureIgnoreCase) 
                ? API_BASE_URL_PRODUCTION 
                : API_BASE_URL_SANDBOX;
            
            var url = $"{baseUrl}{settings.RealmId}/{endpoint}";
            
            var json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TResponse>(responseContent);
        }

        /// <summary>
        /// Check if the integration is configured and connected
        /// </summary>
        public bool IsConnected()
        {
            var settings = GetSettings();
            
            return settings.Enabled &&
                   !string.IsNullOrEmpty(settings.ClientId) &&
                   !string.IsNullOrEmpty(settings.ClientSecret) &&
                   !string.IsNullOrEmpty(settings.RedirectUri) &&
                   !string.IsNullOrEmpty(settings.RealmId) &&
                   !string.IsNullOrEmpty(settings.AccessToken) &&
                   !string.IsNullOrEmpty(settings.RefreshToken);
        }

        /// <summary>
        /// Disconnect from QuickBooks
        /// </summary>
        public async Task DisconnectAsync()
        {
            var settings = GetSettings();
            
            if (string.IsNullOrEmpty(settings.RefreshToken))
                return;

            try
            {
                var client = _httpClientFactory.CreateClient();
                
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("token", settings.RefreshToken)
                });
                
                // Add basic auth header
                var authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{settings.ClientId}:{settings.ClientSecret}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
                
                var response = await client.PostAsync($"{OAUTH_BASE_URL}/revoke", content);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from QuickBooks");
            }
            
            settings.AccessToken = null;
            settings.RefreshToken = null;
            settings.AccessTokenExpiresIn = 0;
            
            await SaveSettingsAsync(settings);
        }
    }
}