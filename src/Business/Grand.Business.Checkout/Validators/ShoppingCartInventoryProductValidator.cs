using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;

namespace Grand.Business.Checkout.Validators;

public record ShoppingCartInventoryProductValidatorRecord(
    Customer Customer,
    Product Product,
    ShoppingCartItem ShoppingCartItem);

public class ShoppingCartInventoryProductValidator : AbstractValidator<ShoppingCartInventoryProductValidatorRecord>
{
  public ShoppingCartInventoryProductValidator(ITranslationService translationService, IProductService productService,
      IStockQuantityService stockQuantityService, ShoppingCartSettings shoppingCartSettings)
  {
    RuleFor(x => x).CustomAsync(async (value, context, ct) =>
    {
      //quantity validation
      var hasQtyWarnings = false;
      if (value.ShoppingCartItem.Quantity < value.Product.OrderMinimumQuantity)
      {
        context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.MinimumQuantity"),
                value.Product.OrderMinimumQuantity));
        hasQtyWarnings = true;
      }

      if (value.ShoppingCartItem.Quantity > value.Product.OrderMaximumQuantity)
      {
        context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.MaximumQuantity"),
                value.Product.OrderMaximumQuantity));
        hasQtyWarnings = true;
      }

      var allowedQuantities = value.Product.ParseAllowedQuantities();

      // Get combination to check if sample is allowed
      var attributeCombination = value.Product.FindProductAttributeCombination(value.ShoppingCartItem.Attributes);
      bool isSampleAllowed = attributeCombination != null && attributeCombination.AllowSample && value.ShoppingCartItem.Quantity == 1;

      // Only validate allowed quantities if it's not an allowed sample
      if (allowedQuantities.Length > 0 && !isSampleAllowed && !allowedQuantities.Contains(value.ShoppingCartItem.Quantity))
        context.AddFailure(string.Format(translationService.GetResource("ShoppingCart.AllowedQuantities"),
                string.Join(", ", allowedQuantities)));

      // Check if warehouse selection is required
      var warehouseSelectionRequired = shoppingCartSettings.AllowToSelectWarehouse;

      // If the product uses attributes, check if it manages inventory by attributes and uses multiple warehouses
      if (warehouseSelectionRequired && value.Product.ManageInventoryMethodId == ManageInventoryMethod.ManageStockByAttributes)
      {
        // Only require warehouse selection if the product uses multiple warehouses
        warehouseSelectionRequired = value.Product.UseMultipleWarehouses &&
                                       attributeCombination != null &&
                                       attributeCombination.WarehouseInventory.Any();
      }
      else if (warehouseSelectionRequired && value.Product.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
      {
        // Only require warehouse selection if the product uses multiple warehouses and has warehouse inventory
        warehouseSelectionRequired = value.Product.UseMultipleWarehouses &&
                                       value.Product.ProductWarehouseInventory.Any();
      }

      if (warehouseSelectionRequired && string.IsNullOrEmpty(value.ShoppingCartItem.WarehouseId))
        context.AddFailure(translationService.GetResource("ShoppingCart.RequiredWarehouse"));

      var warehouseId = !string.IsNullOrEmpty(value.ShoppingCartItem.WarehouseId)
            ? value.ShoppingCartItem.WarehouseId
            : "";

      if (!hasQtyWarnings)
        switch (value.Product.ManageInventoryMethodId)
        {
          case ManageInventoryMethod.DontManageStock:
            {
              //do nothing
            }
            break;
          case ManageInventoryMethod.ManageStock:
            {
              if (value.Product.BackorderModeId == BackorderMode.NoBackorders)
              {
                var qty = value.ShoppingCartItem.Quantity;

                qty += value.Customer.ShoppingCartItems
                        .Where(x => x.ShoppingCartTypeId == value.ShoppingCartItem.ShoppingCartTypeId &&
                                    x.WarehouseId == warehouseId &&
                                    x.ProductId == value.ShoppingCartItem.ProductId &&
                                    x.StoreId == value.ShoppingCartItem.StoreId &&
                                    x.Id != value.ShoppingCartItem.Id)
                        .Sum(x => x.Quantity);

                var maximumQuantityCanBeAdded =
                        stockQuantityService.GetTotalStockQuantity(value.Product, warehouseId: warehouseId);
                if (maximumQuantityCanBeAdded < qty)
                  context.AddFailure(maximumQuantityCanBeAdded <= 0
                          ? translationService.GetResource("ShoppingCart.OutOfStock")
                          : string.Format(
                              translationService.GetResource("ShoppingCart.QuantityExceedsStock"),
                              maximumQuantityCanBeAdded));
              }
            }
            break;
          case ManageInventoryMethod.ManageStockByBundleProducts:
            {
              foreach (var item in value.Product.BundleProducts)
              {
                var qty = value.ShoppingCartItem.Quantity * item.Quantity;
                var p1 = await productService.GetProductById(item.ProductId);
                if (p1 is not { BackorderModeId: BackorderMode.NoBackorders }) continue;
                if (p1.ManageInventoryMethodId == ManageInventoryMethod.ManageStock)
                {
                  var maximumQuantityCanBeAdded =
                          stockQuantityService.GetTotalStockQuantity(p1, warehouseId: warehouseId);
                  if (maximumQuantityCanBeAdded < qty)
                    context.AddFailure(string.Format(
                            translationService.GetResource("ShoppingCart.OutOfStock.BundleProduct"),
                            p1.Name));
                }

                if (p1.ManageInventoryMethodId != ManageInventoryMethod.ManageStockByAttributes)
                  continue;
                if (attributeCombination != null)
                {
                  //combination exists - check stock level
                  var stockQuantity =
                          stockQuantityService.GetTotalStockQuantityForCombination(p1, attributeCombination,
                              warehouseId: warehouseId);
                  if (!attributeCombination.AllowOutOfStockOrders && stockQuantity < qty)
                    context.AddFailure(stockQuantity <= 0
                            ? string.Format(
                                translationService.GetResource(
                                    "ShoppingCart.OutOfStock.BundleProduct"), p1.Name)
                            : string.Format(
                                translationService.GetResource(
                                    "ShoppingCart.QuantityExceedsStock.BundleProduct"), p1.Name,
                                stockQuantity));
                }
                else
                {
                  context.AddFailure(translationService.GetResource("ShoppingCart.Combination.NotExist"));
                }
              }
            }
            break;
          case ManageInventoryMethod.ManageStockByAttributes:
            {
              var productCombination =
                      value.Product.FindProductAttributeCombination(value.ShoppingCartItem.Attributes);
              if (productCombination != null)
              {
                //combination exists - check stock level
                var stockQuantity =
                        stockQuantityService.GetTotalStockQuantityForCombination(value.Product, productCombination,
                            warehouseId: warehouseId);
                if (!productCombination.AllowOutOfStockOrders && stockQuantity < value.ShoppingCartItem.Quantity)
                  context.AddFailure(stockQuantity <= 0
                          ? translationService.GetResource("ShoppingCart.OutOfStock")
                          : string.Format(
                              translationService.GetResource("ShoppingCart.QuantityExceedsStock"),
                              stockQuantity));
              }
              else
              {
                context.AddFailure(translationService.GetResource("ShoppingCart.Combination.NotExist"));
              }
            }
            break;
        }
    });
  }
}