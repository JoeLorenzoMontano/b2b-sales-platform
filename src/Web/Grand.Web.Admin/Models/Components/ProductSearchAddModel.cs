using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Components;

public class ProductSearchAddModel : BaseModel
{
    public ProductSearchAddModel()
    {
        AvailableCategories = new List<SelectListItem>();
        AvailableBrands = new List<SelectListItem>();
        AvailableCollections = new List<SelectListItem>();
        AvailableProductTypes = new List<SelectListItem>();
    }

    /// <summary>
    /// Context identifier (e.g., OrderId)
    /// </summary>
    public string ContextId { get; set; }

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

    [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCategory")]
    public string SearchCategoryId { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchBrand")]
    public string SearchBrandId { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchCollection")]
    public string SearchCollectionId { get; set; }

    [GrandResourceDisplayName("Admin.Catalog.Products.List.SearchProductType")]
    public int SearchProductTypeId { get; set; }

    public IList<SelectListItem> AvailableCategories { get; set; }
    public IList<SelectListItem> AvailableBrands { get; set; }
    public IList<SelectListItem> AvailableCollections { get; set; }
    public IList<SelectListItem> AvailableProductTypes { get; set; }
}