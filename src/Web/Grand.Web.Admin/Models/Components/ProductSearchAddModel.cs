using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Components;

public class ProductSearchAddModel : BaseModel
{
    public ProductSearchAddModel()
    {
        AvailableBrands = new List<SelectListItem>();
        AvailableCategories = new List<SelectListItem>();
    }

    /// <summary>
    /// Context identifier for UI isolation (e.g., 'incoming-ORDER123')
    /// </summary>
    public string ContextId { get; set; }

    /// <summary>
    /// Actual order ID for API calls (e.g., 'ORDER123')
    /// </summary>
    public string OrderId { get; set; }

    /// <summary>
    /// Context type (e.g., "order", "dashboard")
    /// </summary>
    public string ContextType { get; set; }

    /// <summary>
    /// Search endpoint URL
    /// </summary>
    public string SearchEndpoint { get; set; }

    /// <summary>
    /// Add product endpoint URL
    /// </summary>
    public string AddEndpoint { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductName")]
    public string SearchProductName { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchBrand")]
    public string SearchBrandId { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCategory")]
    public string SearchCategoryId { get; set; }

    public IList<SelectListItem> AvailableBrands { get; set; }

    public IList<SelectListItem> AvailableCategories { get; set; }
}