using Integration.QuickBooks.Configuration;
using Integration.QuickBooks.Models;

namespace Integration.QuickBooks.Services
{
    /// <summary>
    /// Base QuickBooks service interface
    /// </summary>
    public interface IQuickBooksService
    {
        /// <summary>
        /// Get authorization URL for OAuth2 flow
        /// </summary>
        /// <returns>Authorization URL</returns>
        string GetAuthorizationUrl();

        /// <summary>
        /// Exchange authorization code for tokens
        /// </summary>
        /// <param name="code">Authorization code</param>
        /// <param name="realmId">Realm ID</param>
        /// <returns>Token response</returns>
        Task<TokenResponse> ExchangeAuthorizationCodeAsync(string code, string realmId);

        /// <summary>
        /// Refresh the access token if expired
        /// </summary>
        /// <returns>True if token was refreshed</returns>
        Task<bool> RefreshTokenIfNeededAsync();

        /// <summary>
        /// Send a GET request to QuickBooks API
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="endpoint">API endpoint</param>
        /// <returns>API response</returns>
        Task<T> GetAsync<T>(string endpoint);

        /// <summary>
        /// Send a POST request to QuickBooks API
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="endpoint">API endpoint</param>
        /// <param name="data">Request data</param>
        /// <returns>API response</returns>
        Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest data);

        /// <summary>
        /// Check if the integration is configured and connected
        /// </summary>
        /// <returns>True if connected</returns>
        bool IsConnected();

        /// <summary>
        /// Get the QuickBooks settings
        /// </summary>
        /// <returns>QuickBooks settings</returns>
        QuickBooksSettings GetSettings();

        /// <summary>
        /// Save the QuickBooks settings
        /// </summary>
        /// <param name="settings">QuickBooks settings</param>
        /// <returns>Task</returns>
        Task SaveSettingsAsync(QuickBooksSettings settings);

        /// <summary>
        /// Disconnect from QuickBooks
        /// </summary>
        /// <returns>Task</returns>
        Task DisconnectAsync();
    }
}