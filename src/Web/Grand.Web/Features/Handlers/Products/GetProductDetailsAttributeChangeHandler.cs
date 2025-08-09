using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Permissions;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Web.Extensions;
using Grand.Web.Features.Models.Products;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.Catalog;
using Grand.Web.Models.Media;
using MediatR;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Features.Handlers.Products;

public class GetProductDetailsAttributeChangeHandler : IRequestHandler<GetProductDetailsAttributeChange,
    ProductDetailsAttributeChangeModel>
{
    private readonly MediaSettings _mediaSettings;
    private readonly IMediator _mediator;
    private readonly IOutOfStockSubscriptionService _outOfStockSubscriptionService;
    private readonly IPermissionService _permissionService;
    private readonly IPictureService _pictureService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IPricingService _pricingService;
    private readonly ShoppingCartSettings _shoppingCartSettings;
    private readonly IStockQuantityService _stockQuantityService;
    private readonly ITaxService _taxService;
    private readonly ITranslationService _translationService;

    public GetProductDetailsAttributeChangeHandler(
        IMediator mediator,
        IStockQuantityService stockQuantityService,
        IPermissionService permissionService,
        IPricingService priceCalculationService,
        IOutOfStockSubscriptionService outOfStockSubscriptionService,
        ITaxService taxService,
        IPriceFormatter priceFormatter,
        ITranslationService translationService,
        IPictureService pictureService,
        ShoppingCartSettings shoppingCartSettings,
        MediaSettings mediaSettings)
    {
        _mediator = mediator;
        _stockQuantityService = stockQuantityService;
        _permissionService = permissionService;
        _pricingService = priceCalculationService;
        _outOfStockSubscriptionService = outOfStockSubscriptionService;
        _taxService = taxService;
        _priceFormatter = priceFormatter;
        _translationService = translationService;
        _pictureService = pictureService;
        _shoppingCartSettings = shoppingCartSettings;
        _mediaSettings = mediaSettings;
    }

    public async Task<ProductDetailsAttributeChangeModel> Handle(GetProductDetailsAttributeChange request,
        CancellationToken cancellationToken)
    {
        var model = new ProductDetailsAttributeChangeModel();

        var customAttributes =
            await _mediator.Send(
                new GetParseProductAttributes { Product = request.Product, Attributes = request.Model.Attributes },
                cancellationToken);

        var warehouseId = _shoppingCartSettings.AllowToSelectWarehouse ? request.Model.WarehouseId :
            request.Product.UseMultipleWarehouses ? request.Store.DefaultWarehouseId :
            string.IsNullOrEmpty(request.Store.DefaultWarehouseId) ? request.Product.WarehouseId :
            request.Store.DefaultWarehouseId;

        //rental attributes
        DateTime? rentalStartDate = null;
        DateTime? rentalEndDate = null;
        if (request.Product.ProductTypeId == ProductType.Reservation)
            request.Product.ParseReservationDates(request.Model.ReservationDatepickerFrom,
                request.Model.ReservationDatepickerTo, out rentalStartDate, out rentalEndDate);

        model.Sku = request.Product.FormatSku(customAttributes);
        model.Mpn = request.Product.FormatMpn(customAttributes);
        model.Gtin = request.Product.FormatGtin(customAttributes);

        if (await _permissionService.Authorize(StandardPermission.DisplayPrices) && !request.Product.EnteredPrice &&
            request.Product.ProductTypeId != ProductType.Auction)
        {
            //we do not calculate price of "customer enters price" option is enabled
            // Get quantity from the request, default to 1 if not specified
            int quantity = 1;
            if (request.Quantity > 0)
            {
                quantity = request.Quantity;
            }
            
            var unitprice = await _pricingService.GetUnitPrice(request.Product,
                request.Customer,
                request.Store,
                request.Currency,
                ShoppingCartType.ShoppingCart,
                quantity, customAttributes, default,
                rentalStartDate, rentalEndDate,
                true);
                
            double finalPrice = unitprice.unitprice;
            
            // Check if this is a sample selection (just for UI indication, not price)
            if (customAttributes != null && customAttributes.Any())
            {
                var attributeValues = request.Product.ParseProductAttributeValues(customAttributes);
                if (attributeValues != null && attributeValues.Any(av => av.AllowSample))
                {
                    model.SampleEnabled = true;
                }
                
                var combination = request.Product.FindProductAttributeCombination(customAttributes);
                if (combination != null && combination.AllowSample)
                {
                    model.SampleEnabled = true;
                }
            }
            
            var productprice = await _taxService.GetProductPrice(request.Product, finalPrice);
            var finalPriceWithDiscount = productprice.productprice;
            model.Price = _priceFormatter.FormatPrice(finalPriceWithDiscount);
        }

        //stock
        var stock = _stockQuantityService.FormatStockMessage(request.Product, warehouseId, customAttributes);
        model.StockAvailability = string.Format(_translationService.GetResource(stock.resource), stock.arg0);

        //out of stock subscription
        if (request.Product.ManageInventoryMethodId is ManageInventoryMethod.ManageStockByAttributes
                or ManageInventoryMethod.ManageStock &&
            request.Product.BackorderModeId == BackorderMode.NoBackorders &&
            request.Product.AllowOutOfStockSubscriptions)
        {
            var combination = request.Product.FindProductAttributeCombination(customAttributes);
            
            // Check stock quantity - only show subscription if actually out of stock
            bool isOutOfStock = false;
            
            if (combination != null)
                isOutOfStock = _stockQuantityService.GetTotalStockQuantityForCombination(request.Product, combination,
                        warehouseId: warehouseId) <= 0;
                        
            if (request.Product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
                isOutOfStock = request.Product.StockQuantity <= 0;
            
            // Only display subscription option if actually out of stock
            if (isOutOfStock)
            {
                model.DisplayOutOfStockSubscription = true;
                
                var subscription = await _outOfStockSubscriptionService
                    .FindSubscription(request.Customer.Id,
                        request.Product.Id, customAttributes, request.Store.Id, warehouseId);
    
                model.ButtonTextOutOfStockSubscription = _translationService.GetResource(subscription != null
                    ? "OutOfStockSubscriptions.DeleteNotifyWhenAvailable"
                    : "OutOfStockSubscriptions.NotifyMeWhenAvailable");
            }
            else
            {
                model.DisplayOutOfStockSubscription = false;
            }
        }

        if (request.Product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
            model.NotAvailableAttributeMappingids = PrepareNotAvailableAttributeMapping(request, customAttributes);

        //conditional attributes
        if (request.Product.ProductAttributeMappings.Any(x => x.ConditionAttribute.Any()))
        {
            var attributes = request.Product.ProductAttributeMappings;
            foreach (var attribute in attributes)
            {
                var conditionMet = request.Product.IsConditionMet(attribute, customAttributes);
                if (!conditionMet.HasValue) continue;
                if (conditionMet.Value)
                    model.EnabledAttributeMappingIds.Add(attribute.Id);
                else
                    model.DisabledAttributeMappingids.Add(attribute.Id);
            }
        }

        //picture. used when we want to override a default product picture when some attribute is selected
        if (!request.Model.LoadPicture) 
        {
            // Add allowed quantities to the model even if no picture is loaded
            AddAllowedQuantitiesToModel(request.Product, model, customAttributes);
            return model;
        }

        //first, try to get product attribute combination picture
        var pictureId = request.Product.FindProductAttributeCombination(customAttributes)?.PictureId;
        if (string.IsNullOrEmpty(pictureId))
            pictureId = request.Product.ParseProductAttributeValues(customAttributes)
                .FirstOrDefault(attributeValue => !string.IsNullOrEmpty(attributeValue.PictureId))?.PictureId ?? "";

        if (string.IsNullOrEmpty(pictureId)) 
        {
            // Add allowed quantities to the model even if no picture ID is found
            AddAllowedQuantitiesToModel(request.Product, model, customAttributes);
            return model;
        }

        var pictureModel = new PictureModel {
            Id = pictureId,
            FullSizeImageUrl = await _pictureService.GetPictureUrl(pictureId),
            ImageUrl = await _pictureService.GetPictureUrl(pictureId, _mediaSettings.ProductDetailsPictureSize)
        };
        model.PictureFullSizeUrl = pictureModel.FullSizeImageUrl;
        model.PictureDefaultSizeUrl = pictureModel.ImageUrl;
        
        // Add allowed quantities to the model
        AddAllowedQuantitiesToModel(request.Product, model, customAttributes);
        
        return model;
    }

    private void AddAllowedQuantitiesToModel(Product product, ProductDetailsAttributeChangeModel model, IList<CustomAttribute> customAttributes)
    {
        // Clear any existing quantities in the model
        model.AllowedQuantities.Clear();
        
        // Get the product's allowed quantities
        var allowedQuantities = product.ParseAllowedQuantities();
        var allowedQuantitiesList = new List<double>(allowedQuantities);
        
        // Check if the combination allows samples
        var combination = product.FindProductAttributeCombination(customAttributes);
        
        if (combination != null && combination.AllowSample)
        {
            // Set the sample availability flag on the model
            model.SampleEnabled = true;
            
            // Remove 1 if it exists already (to avoid duplicates)
            allowedQuantitiesList.Remove(1);
            // Add sample quantity (1) at the beginning of the list
            allowedQuantitiesList.Insert(0, 1);
        }
        
        // Set case size if combination exists
        if (combination != null)
        {
            model.CaseSize = combination.CaseSize;
        }
        else
        {
            model.SampleEnabled = false;
        }
        
        // Add quantities to the model
        foreach (var qty in allowedQuantitiesList)
        {
            model.AllowedQuantities.Add(new SelectListItem {
                Text = qty.ToString("F2").TrimEnd('0').TrimEnd('.') + (qty == 1 && model.SampleEnabled ? " (Sample)" : ""),
                Value = qty.ToString("F2").TrimEnd('0').TrimEnd('.')
            });
        }
    }
    
    private static List<string> PrepareNotAvailableAttributeMapping(GetProductDetailsAttributeChange request,
        IList<CustomAttribute> customAttributes)
    {
        var model = new List<string>();

        var combination = request.Product.FindProductAttributeCombination(customAttributes);
        foreach (var customAttribute in customAttributes)
        {
            //find all combinations with attributes
            var combinations = request.Product.ProductAttributeCombinations
                .Where(x => x.Attributes.Any(z => z.Key == customAttribute.Key && z.Value == customAttribute.Value))
                .ToList();

            //limit to without existing combination
            combinations = combinations.Where(x => x.Id != combination?.Id).ToList();
            //where the stock is unavailable
            var combinationsStockUnavailable =
                combinations.Where(x => x.StockQuantity - x.ReservedQuantity <= 0).ToList();

            //where the stock is available
            var combinationsStockAvailable = combinations
                .Where(x => x.StockQuantity - x.ReservedQuantity > 0).ToList();

            if (combinationsStockUnavailable.Count > 0)
            {
                var x = combinationsStockUnavailable.SelectMany(x => x.Attributes)
                    .Where(x => x.Value != customAttribute.Value);
                var notAvailable = x.Select(z => z.Value).Distinct().ToList();
                notAvailable.ForEach(z =>
                {
                    if (!model.Contains(z))
                        model.Add(z);
                });
            }

            if (combinationsStockAvailable.Count <= 0) continue;

            combinationsStockAvailable.SelectMany(x => x.Attributes)
                .Where(x => x.Value != customAttribute.Value).Select(x => x.Value).Distinct().ToList().ForEach(z =>
                {
                    if (model.Contains(z))
                        model.Remove(z);
                });

            // another way to list available combinations                 
            /*
            var combinations = request.Product.ProductAttributeCombinations.ToList();

            //limit to without existing combination
            combinations = combinations.Where(x => x.Id != combination?.Id).ToList();
            //where the stock is unavailable
            var combinationsStockUnavailable = combinations.Where(x => x.StockQuantity - x.ReservedQuantity <= 0).ToList();

            //where the stock is available
            var combinationsStockAvailable = combinations
                .Where(x => x.Attributes.Any(z => z.Key == customAttribute.Key && z.Value == customAttribute.Value))
                .Where(x => x.StockQuantity - x.ReservedQuantity > 0).ToList();

            if (combinationsStockUnavailable.Count > 0)
            {
                var x = combinationsStockUnavailable.SelectMany(x => x.Attributes).Where(x => x.Value != customAttribute.Value);
                var notAvailable = x.Select(x => x.Value).Distinct().ToList();
                notAvailable.ForEach((x) =>
                {
                    if (!model.Contains(x))
                        model.Add(x);
                });
            }
            if (combinationsStockAvailable.Count > 0)
            {
                var x = combinationsStockAvailable.SelectMany(x => x.Attributes).Where(x => x.Value != customAttribute.Value);
                var available = x.Select(x => x.Value).Distinct().ToList();
                available.ForEach((x) =>
                {
                    if (model.Contains(x))
                        model.Remove(x);
                });
            }
            */
        }

        if (customAttributes.Any())
        {
            customAttributes.ToList().ForEach(x =>
            {
                if (model.Contains(x.Value))
                    model.Remove(x.Value);
            });
        }
        //?? Should we disable any value if no one attributes selected ?
        else
        {
            var combinationsInStock = request.Product.ProductAttributeCombinations
                .Where(x => x.StockQuantity - x.ReservedQuantity > 0)
                .SelectMany(x => x.Attributes)
                .Select(x => x.Value).Distinct();
            var combinationsOutofStock = request.Product.ProductAttributeCombinations
                .Where(x => x.StockQuantity - x.ReservedQuantity <= 0)
                .SelectMany(x => x.Attributes)
                .Select(x => x.Value).Distinct();
            model = combinationsOutofStock.Except(combinationsInStock).ToList();
        }

        return model;
    }
}