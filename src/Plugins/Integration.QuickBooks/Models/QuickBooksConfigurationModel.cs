using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Integration.QuickBooks.Models
{
    public class QuickBooksConfigurationModel : BaseModel
    {
        [GrandResourceDisplayName("Plugins.Integration.QuickBooks.Fields.ClientId")]
        public string ClientId { get; set; }

        [GrandResourceDisplayName("Plugins.Integration.QuickBooks.Fields.ClientSecret")]
        public string ClientSecret { get; set; }

        [GrandResourceDisplayName("Plugins.Integration.QuickBooks.Fields.RedirectUri")]
        public string RedirectUri { get; set; }

        [GrandResourceDisplayName("Plugins.Integration.QuickBooks.Fields.Environment")]
        public string Environment { get; set; }

        [GrandResourceDisplayName("Plugins.Integration.QuickBooks.Fields.Enabled")]
        public bool Enabled { get; set; }

        [GrandResourceDisplayName("Plugins.Integration.QuickBooks.Fields.WebhookToken")]
        public string WebhookToken { get; set; }

        [GrandResourceDisplayName("Plugins.Integration.QuickBooks.Fields.IsConnected")]
        public bool IsConnected { get; set; }

        public string RealmId { get; set; }
    }
}