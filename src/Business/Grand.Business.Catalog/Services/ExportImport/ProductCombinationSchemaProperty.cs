using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.ExportImport;
using Grand.Business.Catalog.Models;

namespace Grand.Business.Catalog.Services.ExportImport;

public class ProductCombinationSchemaProperty : ISchemaProperty<ProductCombinationExportModel>
{
    private readonly IPictureService _pictureService;

    public ProductCombinationSchemaProperty(IPictureService pictureService)
    {
        _pictureService = pictureService;
    }

    public virtual async Task<PropertyByName<ProductCombinationExportModel>[]> GetProperties()
    {
        var properties = new[] {
            // Core Product Identity
            new PropertyByName<ProductCombinationExportModel>("Id", p => p.Id),
            new PropertyByName<ProductCombinationExportModel>("ProductTypeId", p => (int)p.ProductTypeId),
            new PropertyByName<ProductCombinationExportModel>("ParentGroupedProductId", p => p.ParentGroupedProductId),
            new PropertyByName<ProductCombinationExportModel>("VisibleIndividually", p => p.VisibleIndividually),
            new PropertyByName<ProductCombinationExportModel>("Name", p => p.Name),
            new PropertyByName<ProductCombinationExportModel>("ShortDescription", p => p.ShortDescription),
            new PropertyByName<ProductCombinationExportModel>("FullDescription", p => p.FullDescription),
            new PropertyByName<ProductCombinationExportModel>("Flag", p => p.Flag),
            new PropertyByName<ProductCombinationExportModel>("BrandId", p => p.BrandId),
            new PropertyByName<ProductCombinationExportModel>("VendorId", p => p.VendorId),
            new PropertyByName<ProductCombinationExportModel>("ShowOnHomePage", p => p.ShowOnHomePage),
            new PropertyByName<ProductCombinationExportModel>("BestSeller", p => p.BestSeller),
            new PropertyByName<ProductCombinationExportModel>("Published", p => p.Published),
            
            // Base Product SKU/Pricing (for reference)
            new PropertyByName<ProductCombinationExportModel>("BaseSKU", p => p.BaseSku),
            new PropertyByName<ProductCombinationExportModel>("BaseMpn", p => p.BaseMpn),
            new PropertyByName<ProductCombinationExportModel>("BaseGtin", p => p.BaseGtin),
            new PropertyByName<ProductCombinationExportModel>("BasePrice", p => p.BasePrice),
            new PropertyByName<ProductCombinationExportModel>("BaseOldPrice", p => p.BaseOldPrice),
            new PropertyByName<ProductCombinationExportModel>("BaseCatalogPrice", p => p.BaseCatalogPrice),
            new PropertyByName<ProductCombinationExportModel>("BaseProductCost", p => p.BaseProductCost),
            new PropertyByName<ProductCombinationExportModel>("BaseStockQuantity", p => p.BaseStockQuantity),
            new PropertyByName<ProductCombinationExportModel>("BaseReservedQuantity", p => p.BaseReservedQuantity),
            
            // Essential Product Properties
            new PropertyByName<ProductCombinationExportModel>("IsGiftVoucher", p => p.IsGiftVoucher),
            new PropertyByName<ProductCombinationExportModel>("GiftVoucherTypeId", p => (int)p.GiftVoucherTypeId),
            new PropertyByName<ProductCombinationExportModel>("OverGiftAmount", p => p.OverGiftAmount),
            new PropertyByName<ProductCombinationExportModel>("IsDownload", p => p.IsDownload),
            new PropertyByName<ProductCombinationExportModel>("DownloadId", p => p.DownloadId),
            new PropertyByName<ProductCombinationExportModel>("UnlimitedDownloads", p => p.UnlimitedDownloads),
            new PropertyByName<ProductCombinationExportModel>("MaxNumberOfDownloads", p => p.MaxNumberOfDownloads),
            new PropertyByName<ProductCombinationExportModel>("DownloadActivationTypeId", p => (int)p.DownloadActivationTypeId),
            new PropertyByName<ProductCombinationExportModel>("HasSampleDownload", p => p.HasSampleDownload),
            new PropertyByName<ProductCombinationExportModel>("SampleDownloadId", p => p.SampleDownloadId),
            new PropertyByName<ProductCombinationExportModel>("IsShipEnabled", p => p.IsShipEnabled),
            new PropertyByName<ProductCombinationExportModel>("IsFreeShipping", p => p.IsFreeShipping),
            new PropertyByName<ProductCombinationExportModel>("ShipSeparately", p => p.ShipSeparately),
            new PropertyByName<ProductCombinationExportModel>("AdditionalShippingCharge", p => p.AdditionalShippingCharge),
            new PropertyByName<ProductCombinationExportModel>("DeliveryDateId", p => p.DeliveryDateId),
            new PropertyByName<ProductCombinationExportModel>("IsTaxExempt", p => p.IsTaxExempt),
            new PropertyByName<ProductCombinationExportModel>("TaxCategoryId", p => p.TaxCategoryId),
            new PropertyByName<ProductCombinationExportModel>("IsTele", p => p.IsTele),
            new PropertyByName<ProductCombinationExportModel>("ManageInventoryMethodId", p => (int)p.ManageInventoryMethodId),
            new PropertyByName<ProductCombinationExportModel>("UseMultipleWarehouses", p => p.UseMultipleWarehouses),
            new PropertyByName<ProductCombinationExportModel>("WarehouseId", p => p.WarehouseId),
            new PropertyByName<ProductCombinationExportModel>("DisplayStockQuantity", p => p.DisplayStockQuantity),
            new PropertyByName<ProductCombinationExportModel>("MinStockQuantity", p => p.MinStockQuantity),
            new PropertyByName<ProductCombinationExportModel>("LowStockActivityId", p => (int)p.LowStockActivityId),
            new PropertyByName<ProductCombinationExportModel>("NotifyAdminForQuantityBelow", p => p.NotifyAdminForQuantityBelow),
            new PropertyByName<ProductCombinationExportModel>("BackorderModeId", p => (int)p.BackorderModeId),
            new PropertyByName<ProductCombinationExportModel>("AllowOutOfStockSubscriptions", p => p.AllowOutOfStockSubscriptions),
            new PropertyByName<ProductCombinationExportModel>("OrderMinimumQuantity", p => p.OrderMinimumQuantity),
            new PropertyByName<ProductCombinationExportModel>("OrderMaximumQuantity", p => p.OrderMaximumQuantity),
            new PropertyByName<ProductCombinationExportModel>("AllowedQuantities", p => p.AllowedQuantities),
            new PropertyByName<ProductCombinationExportModel>("DisableBuyButton", p => p.DisableBuyButton),
            new PropertyByName<ProductCombinationExportModel>("DisableWishlistButton", p => p.DisableWishlistButton),
            new PropertyByName<ProductCombinationExportModel>("CallForPrice", p => p.CallForPrice),
            new PropertyByName<ProductCombinationExportModel>("EnteredPrice", p => p.EnteredPrice),
            new PropertyByName<ProductCombinationExportModel>("MinEnteredPrice", p => p.MinEnteredPrice),
            new PropertyByName<ProductCombinationExportModel>("MaxEnteredPrice", p => p.MaxEnteredPrice),
            new PropertyByName<ProductCombinationExportModel>("BasepriceEnabled", p => p.BasepriceEnabled),
            new PropertyByName<ProductCombinationExportModel>("BasepriceAmount", p => p.BasepriceAmount),
            new PropertyByName<ProductCombinationExportModel>("BasepriceUnitId", p => p.BasepriceUnitId),
            new PropertyByName<ProductCombinationExportModel>("BasepriceBaseAmount", p => p.BasepriceBaseAmount),
            new PropertyByName<ProductCombinationExportModel>("BasepriceBaseUnitId", p => p.BasepriceBaseUnitId),
            new PropertyByName<ProductCombinationExportModel>("MarkAsNew", p => p.MarkAsNew),
            new PropertyByName<ProductCombinationExportModel>("MarkAsNewStartDateTimeUtc", p => p.MarkAsNewStartDateTimeUtc),
            new PropertyByName<ProductCombinationExportModel>("MarkAsNewEndDateTimeUtc", p => p.MarkAsNewEndDateTimeUtc),
            new PropertyByName<ProductCombinationExportModel>("UnitId", p => p.UnitId),
            new PropertyByName<ProductCombinationExportModel>("Weight", p => p.Weight),
            new PropertyByName<ProductCombinationExportModel>("Length", p => p.Length),
            new PropertyByName<ProductCombinationExportModel>("Width", p => p.Width),
            new PropertyByName<ProductCombinationExportModel>("Height", p => p.Height),
            new PropertyByName<ProductCombinationExportModel>("DisplayOrder", p => p.DisplayOrder),
            new PropertyByName<ProductCombinationExportModel>("DisplayOrderCategory", p => p.DisplayOrderCategory),
            new PropertyByName<ProductCombinationExportModel>("DisplayOrderBrand", p => p.DisplayOrderBrand),
            new PropertyByName<ProductCombinationExportModel>("DisplayOrderCollection", p => p.DisplayOrderCollection),
            new PropertyByName<ProductCombinationExportModel>("OnSale", p => p.OnSale),
            
            // Category and Collection IDs
            new PropertyByName<ProductCombinationExportModel>("CategoryIds", p => p.CategoryIds),
            new PropertyByName<ProductCombinationExportModel>("CollectionIds", p => p.CollectionIds),
            
            // Pictures
            new PropertyByName<ProductCombinationExportModel>("Picture1", p => p.Picture1),
            new PropertyByName<ProductCombinationExportModel>("Picture2", p => p.Picture2),
            new PropertyByName<ProductCombinationExportModel>("Picture3", p => p.Picture3),
            
            // COMBINATION-SPECIFIC FIELDS (Key Enhancement)
            new PropertyByName<ProductCombinationExportModel>("HasCombinations", p => p.HasCombinations),
            new PropertyByName<ProductCombinationExportModel>("CombinationId", p => p.CombinationId),
            new PropertyByName<ProductCombinationExportModel>("CombinationSKU", p => p.CombinationSku),
            new PropertyByName<ProductCombinationExportModel>("CombinationMpn", p => p.CombinationMpn),
            new PropertyByName<ProductCombinationExportModel>("CombinationGtin", p => p.CombinationGtin),
            new PropertyByName<ProductCombinationExportModel>("CombinationPrice", p => p.CombinationPrice?.ToString() ?? ""),
            new PropertyByName<ProductCombinationExportModel>("CombinationStockQuantity", p => p.CombinationStockQuantity),
            new PropertyByName<ProductCombinationExportModel>("CombinationReservedQuantity", p => p.CombinationReservedQuantity),
            new PropertyByName<ProductCombinationExportModel>("CombinationAllowOutOfStockOrders", p => p.CombinationAllowOutOfStockOrders),
            new PropertyByName<ProductCombinationExportModel>("CombinationAttributes", p => p.CombinationAttributes),
            new PropertyByName<ProductCombinationExportModel>("CombinationText", p => p.CombinationText),
            new PropertyByName<ProductCombinationExportModel>("CombinationPictureId", p => p.CombinationPictureId),
            new PropertyByName<ProductCombinationExportModel>("CombinationNotifyAdminForQuantityBelow", p => p.CombinationNotifyAdminForQuantityBelow),
            
            // FINAL COMPUTED FIELDS (What user probably cares about most)
            new PropertyByName<ProductCombinationExportModel>("FinalSKU", p => p.FinalSku),
            new PropertyByName<ProductCombinationExportModel>("FinalPrice", p => p.FinalPrice),
            new PropertyByName<ProductCombinationExportModel>("FinalStockQuantity", p => p.FinalStockQuantity)
        };
        
        return await Task.FromResult(properties);
    }
}