using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using MediatR;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class UpdateOrderItemCommandHandler : IRequestHandler<UpdateOrderItemCommand, bool>
{
    private readonly IInventoryManageService _inventoryManageService;
    private readonly IMediator _mediator;
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;
    private readonly IShipmentService _shipmentService;

    public UpdateOrderItemCommandHandler(
        IMediator mediator,
        IOrderService orderService,
        IShipmentService shipmentService,
        IProductService productService,
        IInventoryManageService inventoryManageService)
    {
        _mediator = mediator;
        _orderService = orderService;
        _shipmentService = shipmentService;
        _productService = productService;
        _inventoryManageService = inventoryManageService;
    }

    public async Task<bool> Handle(UpdateOrderItemCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Order);
        ArgumentNullException.ThrowIfNull(request.OrderItem);

        var originalOrder = await _orderService.GetOrderById(request.Order.Id);
        var originalOrderItem = originalOrder.OrderItems.FirstOrDefault(x => x.Id == request.OrderItem.Id);
        if (originalOrderItem != null)
        {
            // Recalculate order totals from scratch by summing all order items
            // This prevents compounding errors from incremental updates
            request.Order.OrderSubtotalExclTax = request.Order.OrderItems.Sum(item => item.PriceExclTax);
            request.Order.OrderSubtotalInclTax = request.Order.OrderItems.Sum(item => item.PriceInclTax);
            request.Order.OrderTax = request.Order.OrderItems.Sum(item => item.PriceInclTax - item.PriceExclTax);

            // Calculate order total: subtotal + shipping + any additional fees - discounts
            request.Order.OrderTotal = request.Order.OrderSubtotalInclTax
                                     + request.Order.OrderShippingInclTax
                                     + request.Order.PaymentMethodAdditionalFeeInclTax
                                     - request.Order.OrderDiscount;

            //TODO
            //request.Order.OrderTaxes

            //adjust inventory
            if (originalOrderItem.Quantity != request.OrderItem.Quantity)
            {
                var qtyDifference = originalOrderItem.Quantity - request.OrderItem.Quantity;
                var productResult = await _productService.GetProductById(request.OrderItem.ProductId, fromDb: true);
                
                // Log inventory adjustment for quantity changes
                await _orderService.InsertOrderNote(new OrderNote {
                    Note = $"Inventory adjustment: {productResult?.Name ?? "Product"} quantity changed from {originalOrderItem.Quantity} to {request.OrderItem.Quantity} (difference: {qtyDifference:+#;-#;0})",
                    DisplayToCustomer = false,
                    OrderId = request.Order.Id
                });
                
                // Add null check to prevent ArgumentNullException
                if (productResult != null)
                {
                    // Adjust reserved quantities for inventory changes (not actual stock)
                    await _inventoryManageService.AdjustReserved(productResult, qtyDifference, request.OrderItem.Attributes, request.OrderItem.WarehouseId);
                }
                else
                {
                    // Log that product was not found - inventory cannot be adjusted
                    await _orderService.InsertOrderNote(new OrderNote {
                        Note = $"Warning: Could not adjust inventory for order item (Product ID: {request.OrderItem.ProductId}) because product was not found.",
                        DisplayToCustomer = false,
                        OrderId = request.Order.Id
                    });
                }

                if (request.Order.ShippingStatusId == ShippingStatus.PartiallyShipped)
                {
                    var shipments = await _shipmentService.GetShipmentsByOrder(request.Order.Id);

                    if (!request.Order.HasItemsToAddToShipment() && shipments.All(x => x.DeliveryDateUtc != null))
                        request.Order.ShippingStatusId = ShippingStatus.Delivered;
                }
            }
        }

        await _orderService.UpdateOrder(request.Order);
        //check order status
        await _mediator.Send(new CheckOrderStatusCommand { Order = request.Order }, cancellationToken);

        //add a note
        var product = await _productService.GetProductById(request.OrderItem.ProductId, fromDb: true);
        await _orderService.InsertOrderNote(new OrderNote {
            Note = $"Order item has been edited - {product?.Name ?? "Product"}",
            DisplayToCustomer = false,
            OrderId = request.Order.Id
        });

        return true;
    }

}