using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Domain.Permissions;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Components;

public class UnpaidOrdersViewComponent : BaseAdminViewComponent
{
    private readonly IPermissionService _permissionService;

    public UnpaidOrdersViewComponent(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        System.Console.WriteLine("[DEBUG] UnpaidOrders ViewComponent invoked");
        
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
        {
            System.Console.WriteLine("[DEBUG] UnpaidOrders ViewComponent - No permission");
            return Content("");
        }

        System.Console.WriteLine("[DEBUG] UnpaidOrders ViewComponent - Rendering view");
        return View();
    }
}