using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;
using Integration.QuickBooks.Configuration;
using System.Threading.Tasks;

namespace Integration.QuickBooks
{
    /// <summary>
    /// QuickBooks plugin
    /// </summary>
    public class QuickBooksPlugin : BasePlugin, IPlugin
    {
        private readonly ISettingService _settingService;
        private readonly IPluginTranslateResource _pluginTranslateResource;

        public QuickBooksPlugin(
            ISettingService settingService,
            IPluginTranslateResource pluginTranslateResource)
        {
            _settingService = settingService;
            _pluginTranslateResource = pluginTranslateResource;
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override async Task Install()
        {
            await _settingService.SaveSetting(new QuickBooksSettings {
                Enabled = false,
                Environment = "Sandbox"
            });
            
            // Add translations
            await _pluginTranslateResource.AddOrUpdatePluginTranslateResource("Integration.QuickBooks.FriendlyName", "QuickBooks Integration");
            await _pluginTranslateResource.AddOrUpdatePluginTranslateResource("Integration.QuickBooks.Configuration", "Configuration");
            await _pluginTranslateResource.AddOrUpdatePluginTranslateResource("Integration.QuickBooks.ClientId", "Client ID");
            await _pluginTranslateResource.AddOrUpdatePluginTranslateResource("Integration.QuickBooks.ClientId.Hint", "Enter your QuickBooks API Client ID");
            await _pluginTranslateResource.AddOrUpdatePluginTranslateResource("Integration.QuickBooks.ClientSecret", "Client Secret");
            await _pluginTranslateResource.AddOrUpdatePluginTranslateResource("Integration.QuickBooks.ClientSecret.Hint", "Enter your QuickBooks API Client Secret");
            await _pluginTranslateResource.AddOrUpdatePluginTranslateResource("Integration.QuickBooks.RedirectUri", "Redirect URI");
            await _pluginTranslateResource.AddOrUpdatePluginTranslateResource("Integration.QuickBooks.RedirectUri.Hint", "The URI to redirect to after authorization");
            await _pluginTranslateResource.AddOrUpdatePluginTranslateResource("Integration.QuickBooks.Environment", "Environment");
            await _pluginTranslateResource.AddOrUpdatePluginTranslateResource("Integration.QuickBooks.Environment.Hint", "Choose the QuickBooks environment");
            await _pluginTranslateResource.AddOrUpdatePluginTranslateResource("Integration.QuickBooks.Enabled", "Enabled");
            await _pluginTranslateResource.AddOrUpdatePluginTranslateResource("Integration.QuickBooks.Enabled.Hint", "Enable QuickBooks integration");
            
            await base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override async Task Uninstall()
        {
            await _settingService.DeleteSetting<QuickBooksSettings>();
            
            // Delete translations
            await _pluginTranslateResource.DeletePluginTranslationResource("Integration.QuickBooks.FriendlyName");
            await _pluginTranslateResource.DeletePluginTranslationResource("Integration.QuickBooks.Configuration");
            await _pluginTranslateResource.DeletePluginTranslationResource("Integration.QuickBooks.ClientId");
            await _pluginTranslateResource.DeletePluginTranslationResource("Integration.QuickBooks.ClientId.Hint");
            await _pluginTranslateResource.DeletePluginTranslationResource("Integration.QuickBooks.ClientSecret");
            await _pluginTranslateResource.DeletePluginTranslationResource("Integration.QuickBooks.ClientSecret.Hint");
            await _pluginTranslateResource.DeletePluginTranslationResource("Integration.QuickBooks.RedirectUri");
            await _pluginTranslateResource.DeletePluginTranslationResource("Integration.QuickBooks.RedirectUri.Hint");
            await _pluginTranslateResource.DeletePluginTranslationResource("Integration.QuickBooks.Environment");
            await _pluginTranslateResource.DeletePluginTranslationResource("Integration.QuickBooks.Environment.Hint");
            await _pluginTranslateResource.DeletePluginTranslationResource("Integration.QuickBooks.Enabled");
            await _pluginTranslateResource.DeletePluginTranslationResource("Integration.QuickBooks.Enabled.Hint");
            
            await base.Uninstall();
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string ConfigurationUrl()
        {
            return "/Admin/QuickBooks/Configure";
        }
    }
}