using Grand.Domain.Common;

namespace Grand.Domain.Catalog;

/// <summary>
///     Represents a product attribute combination
/// </summary>
public class ProductAttributeCombination : SubBaseEntity, ICloneable
{
    private ICollection<ProductCombinationTierPrices> _tierPrices;
    private ICollection<ProductCombinationWarehouseInventory> _warehouseInventory;

    /// <summary>
    ///     Gets or sets the custom attributes (see "ProductAttribute" entity for more info)
    /// </summary>
    public IList<CustomAttribute> Attributes { get; set; } = new List<CustomAttribute>();

    /// <summary>
    ///     Gets or sets the stock quantity
    /// </summary>
    public double StockQuantity { get; set; }

    /// <summary>
    ///     Gets or sets the reserved quantity (ordered but not shipped yet)
    /// </summary>
    public double ReservedQuantity { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to allow orders when out of stock
    /// </summary>
    public bool AllowOutOfStockOrders { get; set; }
    
    /// <summary>
    ///     Gets or sets a value indicating whether to allow a sample (quantity of 1) regardless of allowed quantities
    /// </summary>
    public bool AllowSample { get; set; }
    
    /// <summary>
    ///     Gets or sets comma-separated sample quantities that should be offered as free samples (e.g., "1,3,5")
    /// </summary>
    public string SampleQuantities { get; set; }
    
    /// <summary>
    ///     Gets or sets a value indicating whether this attribute combination is marked as new
    /// </summary>
    public bool MarkAsNew { get; set; }
    
    /// <summary>
    ///     Gets or sets the start date and time of the new attribute combination (set as "New" from date). Leave empty to ignore
    /// </summary>
    public DateTime? MarkAsNewStartDateTimeUtc { get; set; }
    
    /// <summary>
    ///     Gets or sets the end date and time of the new attribute combination (set as "New" to date). Leave empty to ignore
    /// </summary>
    public DateTime? MarkAsNewEndDateTimeUtc { get; set; }

    /// <summary>
    ///     Gets or sets the text
    /// </summary>
    public string Text { get; set; }

    /// <summary>
    ///     Gets or sets the SKU
    /// </summary>
    public string Sku { get; set; }

    /// <summary>
    ///     Gets or sets the collection part number
    /// </summary>
    public string Mpn { get; set; }

    /// <summary>
    ///     Gets or sets the Global Trade Item Number (GTIN). These identifiers include UPC (in North America), EAN (in
    ///     Europe), JAN (in Japan), and ISBN (for books).
    /// </summary>
    public string Gtin { get; set; }

    /// <summary>
    ///     Gets or sets the attribute combination price. This way a store owner can override the default product price when
    ///     this attribute combination is added to the cart. For example, you can give a discount this way.
    /// </summary>
    public double? OverriddenPrice { get; set; }

    /// <summary>
    ///     Gets or sets the quantity when admin should be notified
    /// </summary>
    public int NotifyAdminForQuantityBelow { get; set; }

    /// <summary>
    ///     Gets or sets the identifier of picture associated with this combination
    /// </summary>
    public string PictureId { get; set; }

    /// <summary>
    ///     Gets or sets the collection of "ProductCombinationWarehouseInventory". We use it only when "UseMultipleWarehouses"
    ///     is set to "true"
    /// </summary>
    public virtual ICollection<ProductCombinationWarehouseInventory> WarehouseInventory {
        get => _warehouseInventory ??= new List<ProductCombinationWarehouseInventory>();
        protected set => _warehouseInventory = value;
    }

    /// <summary>
    ///     Gets or sets the collection of "ProductCombinationTierPrices".
    /// </summary>
    public virtual ICollection<ProductCombinationTierPrices> TierPrices {
        get => _tierPrices ??= new List<ProductCombinationTierPrices>();
        protected set => _tierPrices = value;
    }

    /// <summary>
    ///     Gets or sets the case size (number of units per case). When greater than 0, case quantities will be displayed to customers.
    /// </summary>
    public double CaseSize { get; set; }

    public object Clone()
    {
        return MemberwiseClone();
    }
    
    /// <summary>
    /// Gets the parsed sample quantities as an array of doubles
    /// </summary>
    /// <returns>Array of sample quantities, or empty array if none defined</returns>
    public double[] GetSampleQuantities()
    {
        if (string.IsNullOrWhiteSpace(SampleQuantities))
            return Array.Empty<double>();
            
        return SampleQuantities
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(x => double.TryParse(x.Trim(), out _))
            .Select(x => double.Parse(x.Trim()))
            .ToArray();
    }
    
    /// <summary>
    /// Checks if the specified quantity is a valid sample quantity
    /// </summary>
    /// <param name="quantity">The quantity to check</param>
    /// <returns>True if it's a valid sample quantity</returns>
    public bool IsSampleQuantity(double quantity)
    {
        // Check new SampleQuantities property first
        var sampleQuantities = GetSampleQuantities();
        if (sampleQuantities.Length > 0)
        {
            return sampleQuantities.Contains(quantity);
        }
        
        // Fallback to legacy AllowSample behavior for backward compatibility
        return AllowSample && quantity == 1;
    }
}