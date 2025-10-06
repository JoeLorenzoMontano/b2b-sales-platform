using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Admin.Models.Orders;

public class BulkAddProductsToOrderModel : BaseModel
{
    public BulkAddProductsToOrderModel()
    {
        AvailableCategories = new List<SelectListItem>();
        AvailableBrands = new List<SelectListItem>();
        AvailableCollections = new List<SelectListItem>();
        AvailableProductTypes = new List<SelectListItem>();
        SelectedProductIds = new List<string>();
        Products = new List<BulkProductConfigModel>();
    }

    public string OrderId { get; set; }
    public string OrderNumber { get; set; }

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

    public IList<string> SelectedProductIds { get; set; }
    public IList<BulkProductConfigModel> Products { get; set; }
}

public class BulkProductConfigModel : BaseModel
{
    public BulkProductConfigModel()
    {
        AvailableWarehouses = new List<SelectListItem>();
        AttributeCombinations = new List<AttributeCombinationModel>();
        Attributes = new List<BulkProductAttributeModel>();
    }

    public string ProductId { get; set; }
    public string ProductName { get; set; }
    public string Sku { get; set; }

    [GrandResourceDisplayName("Admin.Orders.Products.Quantity")]
    [Required]
    public decimal Quantity { get; set; } = 1;

    [GrandResourceDisplayName("Admin.Orders.Products.UnitPrice")]
    public decimal UnitPrice { get; set; }

    [GrandResourceDisplayName("Admin.Orders.Products.Warehouse")]
    public string WarehouseId { get; set; }

    public bool NeedsWarehouse { get; set; }
    public bool HasAttributes { get; set; }

    public IList<SelectListItem> AvailableWarehouses { get; set; }

    // For products with attribute combinations
    public string SelectedCombinationId { get; set; }
    public IList<AttributeCombinationModel> AttributeCombinations { get; set; }

    // For products with individual attributes
    public IList<BulkProductAttributeModel> Attributes { get; set; }
}

public class AttributeCombinationModel : BaseModel
{
    public string Id { get; set; }
    public string AttributesInfo { get; set; }
    public string Sku { get; set; }
    public decimal StockQuantity { get; set; }
    public decimal? OverriddenPrice { get; set; }
}

public class BulkProductAttributeModel : BaseModel
{
    public BulkProductAttributeModel()
    {
        Values = new List<BulkProductAttributeValueModel>();
    }

    public string ProductAttributeId { get; set; }
    public string ProductAttributeMappingId { get; set; }
    public string Name { get; set; }
    public bool IsRequired { get; set; }
    public int AttributeControlTypeId { get; set; }
    public IList<BulkProductAttributeValueModel> Values { get; set; }
}

public class BulkProductAttributeValueModel : BaseModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public bool IsPreSelected { get; set; }
    public decimal PriceAdjustment { get; set; }
}