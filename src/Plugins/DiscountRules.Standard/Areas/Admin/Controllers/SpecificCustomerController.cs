using DiscountRules.Standard.Models;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Permissions;
using Grand.Domain.Discounts;
using Grand.Domain.Customers;
using Grand.Web.Common.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Grand.Web.Common.DataSource;

namespace DiscountRules.Standard.Areas.Admin.Controllers;

public class SpecificCustomerController : BaseAdminPluginController
{
    private readonly IDiscountService _discountService;
    private readonly ICustomerService _customerService;
    private readonly IPermissionService _permissionService;
    private readonly IGroupService _groupService;

    public SpecificCustomerController(
        IDiscountService discountService,
        ICustomerService customerService,
        IPermissionService permissionService,
        IGroupService groupService)
    {
        _discountService = discountService;
        _customerService = customerService;
        _permissionService = permissionService;
        _groupService = groupService;
    }

    public async Task<IActionResult> Configure(string discountId, string discountRequirementId)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageDiscounts))
            return Content("Access denied");

        var discount = await _discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new ArgumentException("Discount could not be loaded");

        DiscountRule discountRequirement = null;
        if (!string.IsNullOrEmpty(discountRequirementId))
        {
            discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);
            if (discountRequirement == null)
                return Content("Failed to load requirement.");
        }

        var customerId = discountRequirement?.Metadata;
        var customer = await _customerService.GetCustomerById(customerId);

        var model = new RequirementSpecificCustomerModel {
            RequirementId = !string.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : "",
            DiscountId = discountId,
            CustomerId = customerId
        };

        if (customer != null)
        {
            model.CustomerEmail = customer.Email;
            model.CustomerInfo = $"{customer.GetFullName()} ({customer.Email})";
        }

        // Prepare customer groups for filtering
        model.AvailableCustomerGroups.Add(new SelectListItem { Text = "Select customer group", Value = "" });
        foreach (var cr in await _groupService.GetAllCustomerGroups(showHidden: true))
            model.AvailableCustomerGroups.Add(new SelectListItem { Text = cr.Name, Value = cr.Id });

        //add a prefix
        ViewData.TemplateInfo.HtmlFieldPrefix =
            $"DiscountRulesSpecificCustomer{(!string.IsNullOrEmpty(discountRequirementId) ? discountRequirementId : "")}";

        return View(model);
    }

    [HttpPost]
    [AutoValidateAntiforgeryToken]
    public async Task<IActionResult> Configure(string discountId, string discountRequirementId, string customerId)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageDiscounts))
            return Content("Access denied");

        var discount = await _discountService.GetDiscountById(discountId);
        if (discount == null)
            throw new ArgumentException("Discount could not be loaded");

        DiscountRule discountRequirement = null;
        if (!string.IsNullOrEmpty(discountRequirementId))
            discountRequirement = discount.DiscountRules.FirstOrDefault(dr => dr.Id == discountRequirementId);

        if (discountRequirement != null)
        {
            //update existing rule
            discountRequirement.Metadata = customerId;
            await _discountService.UpdateDiscount(discount);
        }
        else
        {
            //save new rule
            discountRequirement = new DiscountRule {
                DiscountRequirementRuleSystemName = "DiscountRules.Standard.MustBeSpecificCustomer",
                Metadata = customerId
            };
            discount.DiscountRules.Add(discountRequirement);
            await _discountService.UpdateDiscount(discount);
        }

        return Json(new { Result = true, NewRequirementId = discountRequirement.Id });
    }
    
    // Customer popup selector
    public IActionResult CustomerSelector(string discountId, string discountRequirementId)
    {
        var model = new CustomerSelectorModel {
            DiscountId = discountId,
            DiscountRequirementId = discountRequirementId
        };
        return View(model);
    }

    // For autocomplete search
    public async Task<IActionResult> CustomerList(string term)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageDiscounts))
            return Content("Access denied");

        var customers = await _customerService.GetAllCustomers(
            email: term,
            pageIndex: 0,
            pageSize: 15);

        var result = (
            from c in customers
            select new { id = c.Id, text = c.Email + " (" + c.GetFullName() + ")" }
        ).ToList();

        return Json(result);
    }
}