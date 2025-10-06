using Microsoft.AspNetCore.Mvc.Rendering;

namespace DiscountRules.Standard.Models;

public class RequirementSpecificCustomerModel
{
    public RequirementSpecificCustomerModel()
    {
        AvailableCustomers = new List<SelectListItem>();
        AvailableCustomerGroups = new List<SelectListItem>();
    }

    public string CustomerId { get; set; }
    public string CustomerEmail { get; set; }
    public string CustomerInfo { get; set; }

    public string DiscountId { get; set; }
    public string RequirementId { get; set; }

    public IList<SelectListItem> AvailableCustomers { get; set; }
    public IList<SelectListItem> AvailableCustomerGroups { get; set; }
}

public class CustomerSelectorModel
{
    public string DiscountId { get; set; }
    public string DiscountRequirementId { get; set; }
    
    // Search properties
    public string SearchEmail { get; set; }
    public string SearchUsername { get; set; }
    public string SearchFirstName { get; set; }
    public string SearchLastName { get; set; }
    public string SearchCustomerGroupId { get; set; }
    
    public IList<SelectListItem> AvailableCustomerGroups { get; set; } = new List<SelectListItem>();
}