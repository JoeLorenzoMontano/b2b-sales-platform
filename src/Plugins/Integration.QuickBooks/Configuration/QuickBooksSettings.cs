using Grand.Domain.Configuration;

namespace Integration.QuickBooks.Configuration
{
    public class QuickBooksSettings : ISettings
    {
        /// <summary>
        /// Gets or sets the QuickBooks Client ID
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the QuickBooks Client Secret
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets the QuickBooks Redirect URI
        /// </summary>
        public string RedirectUri { get; set; }

        /// <summary>
        /// Gets or sets the QuickBooks Environment (Sandbox/Production)
        /// </summary>
        public string Environment { get; set; } = "Sandbox";

        /// <summary>
        /// Gets or sets the Company ID (Realm ID)
        /// </summary>
        public string RealmId { get; set; }

        /// <summary>
        /// Gets or sets the Access Token
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the Refresh Token
        /// </summary>
        public string RefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the Access Token Expiry
        /// </summary>
        public long AccessTokenExpiresIn { get; set; }

        /// <summary>
        /// Gets or sets whether the QuickBooks integration is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the webhook token
        /// </summary>
        public string WebhookToken { get; set; }

        /// <summary>
        /// Gets or sets the last sync date for customers
        /// </summary>
        public DateTime? LastCustomerSyncDate { get; set; }

        /// <summary>
        /// Gets or sets the last sync date for invoices
        /// </summary>
        public DateTime? LastInvoiceSyncDate { get; set; }
    }
}