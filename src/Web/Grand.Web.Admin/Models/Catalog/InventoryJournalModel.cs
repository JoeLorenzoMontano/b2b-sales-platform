using Grand.Domain.Common;
using Grand.Infrastructure.Models;
using System;
using System.Collections.Generic;

namespace Grand.Web.Admin.Models.Catalog
{
    public class InventoryJournalModel : BaseEntityModel
    {
        public string ObjectId { get; set; }
        public string ObjectType { get; set; }
        public string PositionId { get; set; }
        public DateTime CreateDateUtc { get; set; }
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public IList<CustomAttribute> Attributes { get; set; } = new List<CustomAttribute>();
        public string WarehouseId { get; set; }
        public string WarehouseName { get; set; }
        public int InQty { get; set; }
        public int OutQty { get; set; }
        public string Comments { get; set; }
        public string Reference { get; set; }
    }
}