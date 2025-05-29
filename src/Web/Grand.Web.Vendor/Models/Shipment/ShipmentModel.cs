using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Vendor.Models.Shipment;

public class ShipmentModel : BaseEntityModel
{
    [GrandResourceDisplayName("Vendor.Orders.Shipments.ID")]
    public override string Id { get; set; }

    public int ShipmentNumber { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Shipments.OrderID")]
    public string OrderId { get; set; }

    public int OrderNumber { get; set; }
    public string OrderCode { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Shipments.TotalWeight")]
    public string TotalWeight { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Shipments.TrackingNumber")]
    public string TrackingNumber { get; set; }

    public string TrackingNumberUrl { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Shipments.ShippedDate")]
    public DateTime? ShippedDate { get; set; }

    public bool CanShip { get; set; }
    public DateTime? ShippedDateUtc { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Shipments.DeliveryDate")]
    public DateTime? DeliveryDate { get; set; }

    public bool CanDeliver { get; set; }
    public DateTime? DeliveryDateUtc { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Shipments.AdminComment")]
    public string AdminComment { get; set; }

    public List<ShipmentItemModel> Items { get; set; } = new();

    public IList<ShipmentStatusEventModel> ShipmentStatusEvents { get; set; } = new List<ShipmentStatusEventModel>();

    //shipment notes
    [GrandResourceDisplayName("Vendor.Orders.Shipments.ShipmentNotes.Fields.DisplayToCustomer")]
    public bool AddShipmentNoteDisplayToCustomer { get; set; }

    [GrandResourceDisplayName("Vendor.Orders.Shipments.ShipmentNotes.Fields.Note")]

    public string AddShipmentNoteMessage { get; set; }


    #region Nested classes

    public class ShipmentItemModel : BaseEntityModel
    {
        public string OrderItemId { get; set; }
        public string ProductId { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.Shipments.Products.ProductName")]
        public string ProductName { get; set; }

        public string Sku { get; set; }
        public string AttributeInfo { get; set; }
        public string RentalInfo { get; set; }
        public bool ShipSeparately { get; set; }

        //weight of one item (product)
        [GrandResourceDisplayName("Vendor.Orders.Shipments.Products.ItemWeight")]
        public string ItemWeight { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.Shipments.Products.ItemDimensions")]
        public string ItemDimensions { get; set; }

        [UIHint("DecimalN2")]
        public double QuantityToAdd { get; set; }
        [UIHint("DecimalN2")]
        public double QuantityOrdered { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.Shipments.Products.QtyShipped")]
        [UIHint("DecimalN2")]
        public double QuantityInThisShipment { get; set; }

        [UIHint("DecimalN2")]
        public double QuantityInAllShipments { get; set; }

        public string ShippedFromWarehouse { get; set; }

        public bool AllowToChooseWarehouse { get; set; }

        //used before a shipment is created
        public List<WarehouseInfo> AvailableWarehouses { get; set; } = new();
        public string WarehouseId { get; set; }

        #region Nested Classes

        public class WarehouseInfo : BaseModel
        {
            public string WarehouseId { get; set; }
            public string WarehouseCode { get; set; }
            public string WarehouseName { get; set; }
            [UIHint("DecimalN2")]
            public double StockQuantity { get; set; }
            [UIHint("DecimalN2")]
            public double ReservedQuantity { get; set; }
        }

        #endregion
    }

    public class ShipmentNote : BaseEntityModel
    {
        public string ShipmentId { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.Shipments.ShipmentNotes.Fields.DisplayToCustomer")]
        public bool DisplayToCustomer { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.Shipments.ShipmentNotes.Fields.Note")]
        public string Note { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.Shipments.ShipmentNotes.Fields.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.Shipments.ShipmentNotes.Fields.CreatedByCustomer")]
        public bool CreatedByCustomer { get; set; }
    }

    public class ShipmentStatusEventModel : BaseModel
    {
        public string EventName { get; set; }
        public string Location { get; set; }
        public DateTime? Date { get; set; }
    }

    #endregion
}