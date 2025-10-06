using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Plugins;

namespace Payments.Consignment;

/// <summary>
///     Consignment payment processor
/// </summary>
public class ConsignmentPaymentPlugin(
    ISettingService settingService,
    IPluginTranslateResource pluginTranslateResource)
    : BasePlugin, IPlugin
{
    #region Methods

    /// <summary>
    ///     Gets a configuration page URL
    /// </summary>
    public override string ConfigurationUrl()
    {
        return ConsignmentPaymentDefaults.ConfigurationUrl;
    }

    public override async Task Install()
    {
        var settings = new ConsignmentPaymentSettings {
            DescriptionText =
                "<p>Items will be delivered on consignment basis. Payment will be processed after goods are delivered and accepted.<br />Our representative will contact you to arrange delivery and payment terms.<br />Consignment terms and conditions apply.</p><p>P.S. You can edit this text from admin panel.</p>"
        };
        await settingService.SaveSetting(settings);
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Payments.Consignment.FriendlyName", "Consignment");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.Consignment.DescriptionText", "Description");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.Consignment.DescriptionText.Hint",
            "Enter info that will be shown to customers during checkout");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.Consignment.PaymentMethodDescription", "Consignment");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.Consignment.AdditionalFee", "Additional fee");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.Consignment.AdditionalFee.Hint", "The additional fee.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.Consignment.AdditionalFeePercentage", "Additional fee. Use percentage");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.Consignment.AdditionalFeePercentage.Hint",
            "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.Consignment.ShippableProductRequired", "Shippable product required");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.Consignment.ShippableProductRequired.Hint",
            "An option indicating whether shippable products are required in order to display this payment method during checkout.");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.Consignment.SkipPaymentInfo", "Skip payment info");
        await pluginTranslateResource.AddOrUpdatePluginTranslateResource("Plugins.Payment.Consignment.DisplayOrder", "Display order");

        await base.Install();
    }

    public override async Task Uninstall()
    {
        //settings
        await settingService.DeleteSetting<ConsignmentPaymentSettings>();

        //locales
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.Consignment.DescriptionText");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.Consignment.DescriptionText.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.Consignment.PaymentMethodDescription");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.Consignment.AdditionalFee");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.Consignment.AdditionalFee.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.Consignment.AdditionalFeePercentage");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.Consignment.AdditionalFeePercentage.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.Consignment.ShippableProductRequired");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.Consignment.ShippableProductRequired.Hint");
        await pluginTranslateResource.DeletePluginTranslationResource("Plugins.Payment.Consignment.SkipPaymentInfo");

        await base.Uninstall();
    }

    #endregion
}