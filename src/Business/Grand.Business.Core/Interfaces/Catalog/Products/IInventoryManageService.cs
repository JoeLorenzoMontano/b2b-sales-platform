using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Shipping;

namespace Grand.Business.Core.Interfaces.Catalog.Products;

public interface IInventoryManageService
{
    #region Inventory management methods

    /// <summary>
    ///     Updates stock the product
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="mediator">Notification</param>
    /// <param name="trackInventory">Whether to track the inventory change in journal</param>
    /// <param name="previousStockQuantity">Previous stock quantity (required when trackInventory is true)</param>
    /// <param name="warehouseId">Warehouse ID (for multi-warehouse scenarios)</param>
    /// <param name="userId">User ID who made the change</param>
    Task UpdateStockProduct(Product product, bool mediator = true, bool trackInventory = false, int? previousStockQuantity = null, string warehouseId = null, string userId = null);

    /// <summary>
    ///     Adjust reserved inventory
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="quantityToChange">Quantity to increase or decrease</param>
    /// <param name="attributes">Attributes</param>
    /// <param name="warehouseId">Warehouse ident</param>
    Task AdjustReserved(Product product, int quantityToChange, IList<CustomAttribute> attributes = null,
        string warehouseId = "");


    /// <summary>
    ///     Book the reserved quantity
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="shipment">Shipment</param>
    /// <param name="shipmentItem">Shipment item</param>
    Task BookReservedInventory(Product product, Shipment shipment, ShipmentItem shipmentItem);

    /// <summary>
    ///     Reverse booked inventory
    /// </summary>
    /// <param name="shipment">Shipment</param>
    /// <param name="shipmentItem">Shipment item</param>
    /// <returns>Quantity reversed</returns>
    Task ReverseBookedInventory(Shipment shipment, ShipmentItem shipmentItem);
    
    /// <summary>
    /// Gets inventory journal entries for a product
    /// </summary>
    /// <param name="productId">Product ID (optional)</param>
    /// <param name="warehouseId">Warehouse ID (optional)</param>
    /// <param name="pageIndex">Page index</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Inventory journal entries</returns>
    Task<IPagedList<InventoryJournal>> GetInventoryJournal(string productId = "", string warehouseId = "", int pageIndex = 0, int pageSize = int.MaxValue);

    #endregion
}