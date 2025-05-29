namespace Grand.Domain.Catalog;

/// <summary>
///     Manage product inventory per warehouse
/// </summary>
public class ProductCombinationWarehouseInventory : SubBaseEntity
{
    /// <summary>
    ///     Gets or sets the warehouse identifier
    /// </summary>
    public string WarehouseId { get; set; }

    /// <summary>
    ///     Gets or sets the stock quantity
    /// </summary>
    public double StockQuantity { get; set; }

    /// <summary>
    ///     Gets or sets the reserved quantity (ordered but not shipped yet)
    /// </summary>
    public double ReservedQuantity { get; set; }
}