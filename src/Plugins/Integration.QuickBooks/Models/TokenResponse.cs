using Newtonsoft.Json;

namespace Integration.QuickBooks.Models
{
    public class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("expires_in")]
        public int AccessTokenExpiresIn { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("x_refresh_token_expires_in")]
        public int RefreshTokenExpiresIn { get; set; }

        [JsonProperty("id_token")]
        public string IdToken { get; set; }
    }
}