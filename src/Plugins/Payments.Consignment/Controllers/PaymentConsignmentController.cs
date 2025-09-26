using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Infrastructure;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Mvc;
using Payments.Consignment.Models;

namespace Payments.Consignment.Controllers;

public class PaymentConsignmentController : BasePaymentController
{
    private readonly ISettingService _settingService;
    private readonly IContextAccessor _contextAccessor;

    public PaymentConsignmentController(
        IContextAccessor contextAccessor,
        ISettingService settingService)
    {
        _contextAccessor = contextAccessor;
        _settingService = settingService;
    }

    public async Task<IActionResult> PaymentInfo()
    {
        var consignmentPaymentSettings = await _settingService.LoadSetting<ConsignmentPaymentSettings>(_contextAccessor.StoreContext.CurrentStore.Id);

        var model = new PaymentInfoModel {
            DescriptionText = consignmentPaymentSettings.DescriptionText
        };

        return View(model);
    }
}