using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Business.Catalog.Models;

namespace Grand.Web.Admin.Services;

public class ProductCombinationExportService
{
    private readonly IProductAttributeFormatter _productAttributeFormatter;
    private readonly IPictureService _pictureService;

    public ProductCombinationExportService(
        IProductAttributeFormatter productAttributeFormatter,
        IPictureService pictureService)
    {
        _productAttributeFormatter = productAttributeFormatter;
        _pictureService = pictureService;
    }

    /// <summary>
    /// Transforms a list of products into flattened export models
    /// Products without combinations = 1 row
    /// Products with combinations = N rows (1 per combination)
    /// </summary>
    public async Task<List<ProductCombinationExportModel>> TransformProductsForExport(IEnumerable<Product> products)
    {
        var exportModels = new List<ProductCombinationExportModel>();

        foreach (var product in products)
        {
            if (product.ProductAttributeCombinations?.Any() == true)
            {
                // Product HAS combinations - create one row per combination
                foreach (var combination in product.ProductAttributeCombinations)
                {
                    var exportModel = await MapProductToExportModel(product);
                    await MapCombinationToExportModel(exportModel, product, combination);
                    exportModels.Add(exportModel);
                }
            }
            else
            {
                // Product has NO combinations - create single row
                var exportModel = await MapProductToExportModel(product);
                exportModel.HasCombinations = false;
                exportModels.Add(exportModel);
            }
        }

        return exportModels;
    }

    /// <summary>
    /// Maps core product properties to export model
    /// </summary>
    private async Task<ProductCombinationExportModel> MapProductToExportModel(Product product)
    {
        var pictures = await GetPictures(product);
        
        return new ProductCombinationExportModel
        {
            // Core Product Identity
            Id = product.Id,
            ProductTypeId = product.ProductTypeId,
            ParentGroupedProductId = product.ParentGroupedProductId,
            VisibleIndividually = product.VisibleIndividually,
            Name = product.Name,
            ShortDescription = product.ShortDescription,
            FullDescription = product.FullDescription,
            Flag = product.Flag,
            BrandId = product.BrandId,
            VendorId = product.VendorId,
            ShowOnHomePage = product.ShowOnHomePage,
            BestSeller = product.BestSeller,
            
            // SEO Properties
            MetaKeywords = product.MetaKeywords,
            MetaDescription = product.MetaDescription,
            MetaTitle = product.MetaTitle,
            SeName = product.GetSeName(""),
            AllowCustomerReviews = product.AllowCustomerReviews,
            Published = product.Published,
            
            // Base Product SKU/Pricing
            BaseSku = product.Sku,
            BaseMpn = product.Mpn,
            BaseGtin = product.Gtin,
            BasePrice = product.Price,
            BaseOldPrice = product.OldPrice,
            BaseCatalogPrice = product.CatalogPrice,
            BaseProductCost = product.ProductCost,
            BaseStockQuantity = product.StockQuantity,
            
            // Gift Voucher Properties
            IsGiftVoucher = product.IsGiftVoucher,
            GiftVoucherTypeId = product.GiftVoucherTypeId,
            OverGiftAmount = product.OverGiftAmount,
            
            // Download Properties
            IsDownload = product.IsDownload,
            DownloadId = product.DownloadId,
            UnlimitedDownloads = product.UnlimitedDownloads,
            MaxNumberOfDownloads = product.MaxNumberOfDownloads,
            DownloadActivationTypeId = product.DownloadActivationTypeId,
            HasSampleDownload = product.HasSampleDownload,
            SampleDownloadId = product.SampleDownloadId,
            
            // Shipping Properties
            IsShipEnabled = product.IsShipEnabled,
            IsFreeShipping = product.IsFreeShipping,
            ShipSeparately = product.ShipSeparately,
            AdditionalShippingCharge = product.AdditionalShippingCharge,
            DeliveryDateId = product.DeliveryDateId,
            
            // Tax Properties
            TaxCategoryId = product.TaxCategoryId,
            IsTaxExempt = product.IsTaxExempt,
            IsTele = product.IsTele,
            
            // Inventory Properties
            ManageInventoryMethodId = product.ManageInventoryMethodId,
            UseMultipleWarehouses = product.UseMultipleWarehouses,
            WarehouseId = product.WarehouseId,
            BaseReservedQuantity = product.ReservedQuantity,
            DisplayStockQuantity = product.DisplayStockQuantity,
            MinStockQuantity = product.MinStockQuantity,
            LowStockActivityId = product.LowStockActivityId,
            NotifyAdminForQuantityBelow = product.NotifyAdminForQuantityBelow,
            BackorderModeId = product.BackorderModeId,
            AllowOutOfStockSubscriptions = product.AllowOutOfStockSubscriptions,
            OrderMinimumQuantity = product.OrderMinimumQuantity,
            OrderMaximumQuantity = product.OrderMaximumQuantity,
            AllowedQuantities = product.AllowedQuantities,
            DisableBuyButton = product.DisableBuyButton,
            DisableWishlistButton = product.DisableWishlistButton,
            
            // Pricing Properties
            CallForPrice = product.CallForPrice,
            EnteredPrice = product.EnteredPrice,
            MinEnteredPrice = product.MinEnteredPrice,
            MaxEnteredPrice = product.MaxEnteredPrice,
            BasepriceEnabled = product.BasepriceEnabled,
            BasepriceAmount = product.BasepriceAmount,
            BasepriceUnitId = product.BasepriceUnitId,
            BasepriceBaseAmount = product.BasepriceBaseAmount,
            BasepriceBaseUnitId = product.BasepriceBaseUnitId,
            
            // Display Properties
            MarkAsNew = product.MarkAsNew,
            MarkAsNewStartDateTimeUtc = product.MarkAsNewStartDateTimeUtc,
            MarkAsNewEndDateTimeUtc = product.MarkAsNewEndDateTimeUtc,
            UnitId = product.UnitId,
            Weight = product.Weight,
            Length = product.Length,
            Width = product.Width,
            Height = product.Height,
            DisplayOrder = product.DisplayOrder,
            DisplayOrderCategory = product.DisplayOrderCategory,
            DisplayOrderBrand = product.DisplayOrderBrand,
            DisplayOrderCollection = product.DisplayOrderCollection,
            OnSale = product.OnSale,
            
            // Category and Collection IDs
            CategoryIds = string.Join(";", product.ProductCategories.Select(n => n.CategoryId).ToArray()),
            CollectionIds = string.Join(";", product.ProductCollections.Select(n => n.CollectionId).ToArray()),
            
            // Pictures
            Picture1 = pictures[0],
            Picture2 = pictures[1],
            Picture3 = pictures[2]
        };
    }

    /// <summary>
    /// Maps combination-specific properties to export model
    /// </summary>
    private async Task MapCombinationToExportModel(ProductCombinationExportModel exportModel, Product product, ProductAttributeCombination combination)
    {
        // Mark this as having combinations
        exportModel.HasCombinations = true;
        
        // Map combination-specific properties
        exportModel.CombinationId = combination.Id;
        exportModel.CombinationSku = combination.Sku;
        exportModel.CombinationMpn = combination.Mpn;
        exportModel.CombinationGtin = combination.Gtin;
        exportModel.CombinationPrice = combination.OverriddenPrice;
        exportModel.CombinationStockQuantity = product.UseMultipleWarehouses 
            ? combination.WarehouseInventory.Sum(w => w.StockQuantity - w.ReservedQuantity)
            : combination.StockQuantity;
        exportModel.CombinationReservedQuantity = combination.ReservedQuantity;
        exportModel.CombinationAllowOutOfStockOrders = combination.AllowOutOfStockOrders;
        exportModel.CombinationText = combination.Text;
        exportModel.CombinationPictureId = combination.PictureId;
        exportModel.CombinationNotifyAdminForQuantityBelow = combination.NotifyAdminForQuantityBelow;
        
        // Format attributes for this combination (e.g., "Color: Red, Size: Large")
        exportModel.CombinationAttributes = await _productAttributeFormatter.FormatAttributes(
            product, 
            combination.Attributes, 
            null, // customer - null for admin export
            ", ", // separator
            true, // renderPrices
            true, // renderProductAttributes
            true, // renderGiftVoucherAttributes
            false, // allowHyperlinks
            true, // renderImages
            false // shortFormat
        );
        
        // Clean up the formatted attributes (remove HTML if present)
        if (!string.IsNullOrEmpty(exportModel.CombinationAttributes))
        {
            exportModel.CombinationAttributes = exportModel.CombinationAttributes
                .Replace("<br />", ", ")
                .Replace("<br/>", ", ")
                .Replace("<br>", ", ")
                .Trim();
            
            // Remove any remaining HTML tags
            exportModel.CombinationAttributes = System.Text.RegularExpressions.Regex.Replace(
                exportModel.CombinationAttributes, "<.*?>", string.Empty);
        }
        
        if (string.IsNullOrEmpty(exportModel.CombinationAttributes))
        {
            exportModel.CombinationAttributes = "(no attributes)";
        }
    }

    /// <summary>
    /// Gets picture URLs for the product (mirrors existing ProductSchemaProperty logic)
    /// </summary>
    private async Task<string[]> GetPictures(Product product)
    {
        var pictures = new string[3];
        for (var i = 0; i < 3; i++)
        {
            var picture = product.ProductPictures.ElementAtOrDefault(i);
            if (picture != null)
            {
                var pic = await _pictureService.GetPictureById(picture.PictureId);
                if (pic != null)
                {
                    pictures[i] = await _pictureService.GetPictureUrl(pic, 300);
                }
            }
            pictures[i] ??= string.Empty;
        }
        return pictures;
    }
}