using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Permissions;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Helpers;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payments.Consignment.Models;

namespace Payments.Consignment.Areas.Admin.Controllers;

[AuthorizeAdmin]
[Area("Admin")]
[PermissionAuthorize(PermissionSystemName.PaymentMethods)]
public class PaymentConsignmentController : BasePaymentController
{
    private readonly ISettingService _settingService;
    private readonly ITranslationService _translationService;
    private readonly IAdminStoreService _adminStoreService;

    public PaymentConsignmentController(
        ISettingService settingService,
        ITranslationService translationService,
        IAdminStoreService adminStoreService)
    {
        _settingService = settingService;
        _translationService = translationService;
        _adminStoreService = adminStoreService;
    }

    public async Task<IActionResult> Configure()
    {
        //load settings for a chosen store scope
        var storeScope = await _adminStoreService.GetActiveStore();
        var consignmentPaymentSettings = await _settingService.LoadSetting<ConsignmentPaymentSettings>(storeScope);

        var model = new ConfigurationModel {
            DescriptionText = consignmentPaymentSettings.DescriptionText,
            AdditionalFee = consignmentPaymentSettings.AdditionalFee,
            AdditionalFeePercentage = consignmentPaymentSettings.AdditionalFeePercentage,
            ShippableProductRequired = consignmentPaymentSettings.ShippableProductRequired,
            DisplayOrder = consignmentPaymentSettings.DisplayOrder,
            SkipPaymentInfo = consignmentPaymentSettings.SkipPaymentInfo
        };
        model.DescriptionText = consignmentPaymentSettings.DescriptionText;

        model.ActiveStore = storeScope;

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Configure(ConfigurationModel model)
    {
        if (!ModelState.IsValid)
            return await Configure();

        //load settings for a chosen store scope
        var storeScope = await _adminStoreService.GetActiveStore();
        var consignmentPaymentSettings = await _settingService.LoadSetting<ConsignmentPaymentSettings>(storeScope);

        //save settings
        consignmentPaymentSettings.DescriptionText = model.DescriptionText;
        consignmentPaymentSettings.AdditionalFee = model.AdditionalFee;
        consignmentPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
        consignmentPaymentSettings.ShippableProductRequired = model.ShippableProductRequired;
        consignmentPaymentSettings.DisplayOrder = model.DisplayOrder;
        consignmentPaymentSettings.SkipPaymentInfo = model.SkipPaymentInfo;

        await _settingService.SaveSetting(consignmentPaymentSettings, storeScope);

        //now clear settings cache
        await _settingService.ClearCache();

        Success(_translationService.GetResource("Admin.Plugins.Saved"));

        return await Configure();
    }
}