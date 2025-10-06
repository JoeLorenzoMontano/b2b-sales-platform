using Grand.Domain.Catalog;

namespace Grand.Business.Catalog.Models;

/// <summary>
/// Flattened model that combines product data with attribute combination data for export
/// </summary>
public class ProductCombinationExportModel
{
    // Core Product Properties (Essential fields)
    public string Id { get; set; }
    public ProductType ProductTypeId { get; set; }
    public string ParentGroupedProductId { get; set; }
    public bool VisibleIndividually { get; set; }
    public string Name { get; set; }
    public string ShortDescription { get; set; }
    public string FullDescription { get; set; }
    public string Flag { get; set; }
    public string BrandId { get; set; }
    public string VendorId { get; set; }
    public bool ShowOnHomePage { get; set; }
    public bool BestSeller { get; set; }
    public string MetaKeywords { get; set; }
    public string MetaDescription { get; set; }
    public string MetaTitle { get; set; }
    public string SeName { get; set; }
    public bool AllowCustomerReviews { get; set; }
    public bool Published { get; set; }
    
    // Base Product SKU/Pricing (from Product entity)
    public string BaseSku { get; set; }
    public string BaseMpn { get; set; }
    public string BaseGtin { get; set; }
    public double BasePrice { get; set; }
    public double BaseOldPrice { get; set; }
    public double BaseCatalogPrice { get; set; }
    public double BaseProductCost { get; set; }
    public double BaseStockQuantity { get; set; }
    public double BaseReservedQuantity { get; set; }
    
    // Essential Product Properties
    public bool IsGiftVoucher { get; set; }
    public GiftVoucherType GiftVoucherTypeId { get; set; }
    public double? OverGiftAmount { get; set; }
    public bool IsDownload { get; set; }
    public string DownloadId { get; set; }
    public bool UnlimitedDownloads { get; set; }
    public int MaxNumberOfDownloads { get; set; }
    public DownloadActivationType DownloadActivationTypeId { get; set; }
    public bool HasSampleDownload { get; set; }
    public string SampleDownloadId { get; set; }
    public bool IsShipEnabled { get; set; }
    public bool IsFreeShipping { get; set; }
    public bool ShipSeparately { get; set; }
    public double AdditionalShippingCharge { get; set; }
    public string DeliveryDateId { get; set; }
    public bool IsTaxExempt { get; set; }
    public string TaxCategoryId { get; set; }
    public bool IsTele { get; set; }
    public ManageInventoryMethod ManageInventoryMethodId { get; set; }
    public bool UseMultipleWarehouses { get; set; }
    public string WarehouseId { get; set; }
    public bool DisplayStockQuantity { get; set; }
    public int MinStockQuantity { get; set; }
    public LowStockActivity LowStockActivityId { get; set; }
    public int NotifyAdminForQuantityBelow { get; set; }
    public BackorderMode BackorderModeId { get; set; }
    public bool AllowOutOfStockSubscriptions { get; set; }
    public int OrderMinimumQuantity { get; set; }
    public int OrderMaximumQuantity { get; set; }
    public string AllowedQuantities { get; set; }
    public bool DisableBuyButton { get; set; }
    public bool DisableWishlistButton { get; set; }
    public bool CallForPrice { get; set; }
    public bool EnteredPrice { get; set; }
    public double MinEnteredPrice { get; set; }
    public double MaxEnteredPrice { get; set; }
    public bool BasepriceEnabled { get; set; }
    public double BasepriceAmount { get; set; }
    public string BasepriceUnitId { get; set; }
    public double BasepriceBaseAmount { get; set; }
    public string BasepriceBaseUnitId { get; set; }
    public bool MarkAsNew { get; set; }
    public DateTime? MarkAsNewStartDateTimeUtc { get; set; }
    public DateTime? MarkAsNewEndDateTimeUtc { get; set; }
    public string UnitId { get; set; }
    public double Weight { get; set; }
    public double Length { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public int DisplayOrder { get; set; }
    public int DisplayOrderCategory { get; set; }
    public int DisplayOrderBrand { get; set; }
    public int DisplayOrderCollection { get; set; }
    public int OnSale { get; set; }
    
    // Category and Collection IDs
    public string CategoryIds { get; set; }
    public string CollectionIds { get; set; }
    
    // Pictures
    public string Picture1 { get; set; }
    public string Picture2 { get; set; }
    public string Picture3 { get; set; }
    
    // === COMBINATION-SPECIFIC PROPERTIES ===
    
    /// <summary>
    /// Indicates if this row represents a product with attribute combinations
    /// </summary>
    public bool HasCombinations { get; set; }
    
    /// <summary>
    /// Combination ID (empty for base product without combinations)
    /// </summary>
    public string CombinationId { get; set; }
    
    /// <summary>
    /// SKU specific to this combination (overrides BaseSku when present)
    /// </summary>
    public string CombinationSku { get; set; }
    
    /// <summary>
    /// MPN specific to this combination
    /// </summary>
    public string CombinationMpn { get; set; }
    
    /// <summary>
    /// GTIN specific to this combination
    /// </summary>
    public string CombinationGtin { get; set; }
    
    /// <summary>
    /// Price override for this combination (null means use base product price)
    /// </summary>
    public double? CombinationPrice { get; set; }
    
    /// <summary>
    /// Stock quantity specific to this combination
    /// </summary>
    public double CombinationStockQuantity { get; set; }
    
    /// <summary>
    /// Reserved quantity for this combination
    /// </summary>
    public double CombinationReservedQuantity { get; set; }
    
    /// <summary>
    /// Allow out of stock orders for this combination
    /// </summary>
    public bool CombinationAllowOutOfStockOrders { get; set; }
    
    /// <summary>
    /// Formatted string of attribute values (e.g., "Color: Red, Size: Large")
    /// </summary>
    public string CombinationAttributes { get; set; }
    
    /// <summary>
    /// Text description for this combination
    /// </summary>
    public string CombinationText { get; set; }
    
    /// <summary>
    /// Picture ID specific to this combination
    /// </summary>
    public string CombinationPictureId { get; set; }
    
    /// <summary>
    /// Notify admin threshold for this combination
    /// </summary>
    public int CombinationNotifyAdminForQuantityBelow { get; set; }
    
    /// <summary>
    /// The final SKU to use (combination SKU if present, otherwise base SKU)
    /// </summary>
    public string FinalSku => !string.IsNullOrEmpty(CombinationSku) ? CombinationSku : BaseSku;
    
    /// <summary>
    /// The final price to use (combination price if present, otherwise base price)
    /// </summary>
    public double FinalPrice => CombinationPrice ?? BasePrice;
    
    /// <summary>
    /// The final stock quantity (combination stock if has combinations, otherwise base stock)
    /// </summary>
    public double FinalStockQuantity => HasCombinations ? CombinationStockQuantity : BaseStockQuantity;
}