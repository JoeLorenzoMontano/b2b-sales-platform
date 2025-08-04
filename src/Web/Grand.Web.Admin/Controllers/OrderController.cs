using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Pdf;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.ExportImport;
using Grand.Domain.Permissions;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.Orders)]
public class OrderController(
    IOrderViewModelService orderViewModelService,
    IOrderService orderService,
    IOrderStatusService orderStatusService,
    ITranslationService translationService,
    IContextAccessor contextAccessor,
    IPdfService pdfService,
    IGroupService groupService,
    IExportManager<Order> exportManager,
    IMediator mediator,
    ISalesEmployeeService _salesEmployeeService,
    IPermissionService _permissionService)
    : BaseAdminController
{
    #region Utilities

    protected virtual async Task<bool> CheckSalesManager(Order order)
    {
        return await groupService.IsSalesManager(contextAccessor.WorkContext.CurrentCustomer)
               && contextAccessor.WorkContext.CurrentCustomer.SeId != order.SeId;
    }

    protected virtual string GetCustomerDisplayName(Order order)
    {
        var customerName = $"{order.BillingAddress?.FirstName} {order.BillingAddress?.LastName}".Trim();
        if (string.IsNullOrEmpty(customerName))
            customerName = order.BillingAddress?.Email ?? "Guest";
        return customerName;
    }

    #endregion

    #region Fields

    #endregion

    #region Ctor

    #endregion

    #region Fulfillment helpers
    
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> GetOrderItemsForFulfillment(string orderId, [FromServices] IProductService productService)
    {
        try
        {
            var order = await orderService.GetOrderById(orderId);
            if (order == null)
                return Json(new DataSourceResult { Data = new List<object>(), Total = 0 });
                
            // Restrict access to own orders for sales staff
            if (await CheckSalesManager(order))
                return Json(new { success = false, error = "Access denied" });
                
            // Only include items with open quantities
            var items = new List<object>();
            
            foreach (var item in order.OrderItems.Where(item => item.OpenQty > 0))
            {
                // Get the product name from product service
                var product = await productService.GetProductById(item.ProductId);
                var productName = product != null ? product.Name : "Product #" + item.ProductId;
                
                // Create a simple anonymous object with only the necessary properties
                items.Add(new {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = productName,
                    Sku = item.Sku,
                    Quantity = item.Quantity,
                    OpenQty = item.OpenQty,
                    UnitPriceInclTax = item.UnitPriceInclTax.ToString("C"),
                    AttributeInfo = item.AttributeDescription,
                    PictureThumbnailUrl = ""  // Empty for thumbnail since we removed it from the view
                });
            }
                
            var gridModel = new DataSourceResult
            {
                Data = items,
                Total = items.Count
            };
            
            return Json(gridModel);
        }
        catch (Exception ex)
        {
            return Json(new DataSourceResult { 
                Data = new List<object>(), 
                Total = 0, 
                Errors = $"Error loading order items: {ex.Message}" 
            });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> GetOrderItemsForIncoming(string orderId, [FromServices] IProductService productService)
    {
        try
        {
            var order = await orderService.GetOrderById(orderId);
            if (order == null)
                return Json(new DataSourceResult { Data = new List<object>(), Total = 0 });
                
            // Restrict access to own orders for sales staff
            if (await CheckSalesManager(order))
                return Json(new { success = false, error = "Access denied" });
                
            // Include all order items for informational purposes (no filtering by OpenQty)
            var items = new List<object>();
            
            foreach (var item in order.OrderItems)
            {
                // Get the product name from product service
                var product = await productService.GetProductById(item.ProductId);
                var productName = product != null ? product.Name : "Product #" + item.ProductId;
                
                // Calculate subtotal for this line item
                var subTotal = (item.Quantity * item.UnitPriceInclTax).ToString("C");
                
                // Create a simple anonymous object with only the necessary properties for display
                items.Add(new {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductName = productName,
                    Sku = item.Sku,
                    Quantity = item.Quantity,
                    UnitPriceInclTax = item.UnitPriceInclTax.ToString("C"),
                    SubTotal = subTotal,
                    AttributeInfo = item.AttributeDescription
                });
            }
                
            var gridModel = new DataSourceResult
            {
                Data = items,
                Total = items.Count
            };
            
            return Json(gridModel);
        }
        catch (Exception ex)
        {
            return Json(new DataSourceResult { 
                Data = new List<object>(), 
                Total = 0, 
                Errors = $"Error loading order items: {ex.Message}" 
            });
        }
    }
    
    #endregion

    #region Order list

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public async Task<IActionResult> List(int? orderStatusId = null,
        int? paymentStatusId = null, int? shippingStatusId = null, DateTime? startDate = null, string code = null)
    {
        var model = await orderViewModelService.PrepareOrderListModel(orderStatusId, paymentStatusId, shippingStatusId,
            startDate, contextAccessor.WorkContext.CurrentCustomer.StaffStoreId, code);
        return View(model);
    }

    public async Task<IActionResult> ProductSearchAutoComplete(string term,
        [FromServices] IProductService productService)
    {
        const int searchTermMinimumLength = 3;
        if (string.IsNullOrWhiteSpace(term) || term.Length < searchTermMinimumLength)
            return Content("");

        var storeId = string.Empty;
        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer))
            storeId = contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

        //products
        const int productNumber = 15;
        var products = (await productService.SearchProducts(
            storeId: storeId,
            keywords: term,
            pageSize: productNumber,
            showHidden: true)).products;

        var result = (from p in products
                select new {
                    label = p.Name,
                    productid = p.Id
                })
            .ToList();
        return Json(result);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> OrderList(DataSourceRequest command, OrderListModel model)
    {
        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer))
            model.StoreId = contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

        var (orderModels, totalCount) =
            await orderViewModelService.PrepareOrderModel(model, command.Page, command.PageSize);

        var gridModel = new DataSourceResult {
            Data = orderModels.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }
    
    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> UnpaidOrdersList(DataSourceRequest command, OrderListModel model)
    {
        System.Console.WriteLine("[DEBUG] UnpaidOrdersList action called");
        
        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer))
            model.StoreId = contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

        // Filter specifically for orders with Pending payment status
        // This will be handled by the PrepareUnpaidOrderModel method
        model.PaymentStatusId = 0;

        var (orderModels, totalCount) =
            await orderViewModelService.PrepareUnpaidOrderModel(model, command.Page, command.PageSize);

        var gridModel = new DataSourceResult {
            Data = orderModels.ToList(),
            Total = totalCount
        };
        return Json(gridModel);
    }
    
    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> FulfillmentOrderList(DataSourceRequest command, OrderListModel model,
        [FromServices] ICustomerService customerService)
    {
        try
        {
            if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer))
                model.StoreId = contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
            
            // Direct database access to get the 20 most recent orders 
            // with paginated query that avoids the issue
            var orders = await orderService.SearchOrders(
                storeId: model.StoreId,
                pageIndex: 0,  // Always first page
                pageSize: 20,  // Reasonable number of orders
                createdFromUtc: DateTime.UtcNow.AddDays(-30) // Get orders from the last 30 days
            );
            
            var fulfillmentOrders = new List<OrderModel>();
            
            foreach (var order in orders)
            {
                try
                {
                    // Check if any item has OpenQty > 0 AND order is verified
                    bool hasUnfulfilledItems = order.OrderItems.Any(item => item.OpenQty > 0);
                    
                    if (hasUnfulfilledItems && order.IsVerifiedOrder)
                    {
                        // Get the model for this order
                        var orderModel = new OrderModel
                        {
                            Id = order.Id,
                            OrderNumber = order.OrderNumber,
                            OrderStatusId = order.OrderStatusId,
                            OrderStatus = ((OrderStatusSystem)order.OrderStatusId).ToString(),
                            PaymentStatus = order.PaymentStatusId.ToString(),
                            ShippingStatus = order.ShippingStatusId.ToString(),
                            CustomerEmail = order.BillingAddress?.Email,
                            CustomerFullName = GetCustomerDisplayName(order),
                            CustomerId = order.CustomerId,
                            OrderTotal = order.OrderTotal.ToString("C"),
                            CreatedOn = order.CreatedOnUtc,
                            UpdatedOn = order.UpdatedOnUtc,
                            StoreName = order.StoreId,
                            ShippingAddressString = order.ShippingAddress?.Address1,
                            TargetDeliveryDate = order.TargetDeliveryDate,
                            RequestedShipmentDate = order.RequestedShipmentDate,
                            CustomerCompany = order.BillingAddress?.Company
                        };

                        // Get customer groups that are not system groups
                        if (!string.IsNullOrEmpty(order.CustomerId))
                        {
                            var customer = await customerService.GetCustomerById(order.CustomerId);
                            if (customer != null && customer.Groups.Any())
                            {
                                // Get all customer groups by IDs
                                var customerGroups = await groupService.GetAllByIds(customer.Groups.ToArray());
                                
                                // Filter out system groups
                                var nonSystemGroups = customerGroups.Where(x => !x.IsSystem).ToList();
                                
                                if (nonSystemGroups.Any())
                                {
                                    // Join the group names with commas
                                    orderModel.CustomerGroups = string.Join(", ", nonSystemGroups.Select(x => x.Name));
                                }
                            }
                        }

                        // Add impersonated employee name if available, otherwise blank
                        if (!string.IsNullOrEmpty(order.ImpersonatedByEmployeeId))
                        {
                            // The impersonating user is a Customer entity, not a SalesEmployee
                            var impersonatingCustomer = await customerService.GetCustomerById(order.ImpersonatedByEmployeeId);
                            if (impersonatingCustomer != null)
                            {
                                orderModel.SalesEmployeeId = impersonatingCustomer.Id;
                                orderModel.SalesEmployeeName = impersonatingCustomer.Email; // Using email as it's always available
                            }
                        }
                        
                        fulfillmentOrders.Add(orderModel);
                    }
                }
                catch (Exception ex)
                {
                    // Log but continue to next order
                    System.Diagnostics.Debug.WriteLine($"Error processing order {order.Id}: {ex.Message}");
                    continue;
                }
            }
            
            var gridModel = new DataSourceResult
            {
                Data = fulfillmentOrders,
                Total = fulfillmentOrders.Count
            };
            
            return Json(gridModel);
        }
        catch (Exception ex)
        {
            // Log the error to help with debugging
            System.Diagnostics.Debug.WriteLine($"Error in FulfillmentOrderList: {ex.Message}");
            
            // Return an empty result instead of an error
            return Json(new DataSourceResult
            {
                Data = new List<OrderModel>(),
                Total = 0
            });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> IncomingOrdersList(DataSourceRequest command, OrderListModel model,
        [FromServices] ICustomerService customerService)
    {
        try
        {
            if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer))
                model.StoreId = contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;
            
            // Get orders that are not verified yet
            var orders = await orderService.SearchOrders(
                storeId: model.StoreId,
                pageIndex: 0,
                pageSize: 50, // Show more incoming orders since they need verification
                createdFromUtc: DateTime.UtcNow.AddDays(-7) // Get orders from the last 7 days
            );
            
            var incomingOrders = new List<OrderModel>();
            
            foreach (var order in orders)
            {
                try
                {
                    // Only include orders that are not verified yet
                    if (!order.IsVerifiedOrder)
                    {
                        var orderModel = new OrderModel
                        {
                            Id = order.Id,
                            OrderNumber = order.OrderNumber,
                            OrderStatusId = order.OrderStatusId,
                            OrderStatus = ((OrderStatusSystem)order.OrderStatusId).ToString(),
                            PaymentStatus = order.PaymentStatusId.ToString(),
                            ShippingStatus = order.ShippingStatusId.ToString(),
                            CustomerEmail = order.BillingAddress?.Email,
                            CustomerFullName = GetCustomerDisplayName(order),
                            CustomerId = order.CustomerId,
                            OrderTotal = order.OrderTotal.ToString("C"),
                            CreatedOn = order.CreatedOnUtc,
                            CustomerCompany = order.BillingAddress?.Company
                        };
                        
                        // Add sales employee information if available
                        if (!string.IsNullOrEmpty(order.SeId))
                        {
                            var salesEmployee = await customerService.GetCustomerById(order.SeId);
                            if (salesEmployee != null)
                            {
                                orderModel.SalesEmployeeId = salesEmployee.Id;
                                orderModel.SalesEmployeeName = salesEmployee.Email;
                            }
                        }
                        else if (!string.IsNullOrEmpty(order.ImpersonatedByEmployeeId))
                        {
                            var impersonatingCustomer = await customerService.GetCustomerById(order.ImpersonatedByEmployeeId);
                            if (impersonatingCustomer != null)
                            {
                                orderModel.SalesEmployeeId = impersonatingCustomer.Id;
                                orderModel.SalesEmployeeName = impersonatingCustomer.Email;
                            }
                        }
                        
                        incomingOrders.Add(orderModel);
                    }
                }
                catch (Exception ex)
                {
                    // Log but continue to next order
                    System.Diagnostics.Debug.WriteLine($"Error processing order {order.Id}: {ex.Message}");
                    continue;
                }
            }
            
            var gridModel = new DataSourceResult
            {
                Data = incomingOrders,
                Total = incomingOrders.Count
            };
            
            return Json(gridModel);
        }
        catch (Exception ex)
        {
            // Log the error to help with debugging
            System.Diagnostics.Debug.WriteLine($"Error in IncomingOrdersList: {ex.Message}");
            
            // Return an empty result instead of an error
            return Json(new DataSourceResult
            {
                Data = new List<OrderModel>(),
                Total = 0
            });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpPost]
    public async Task<IActionResult> GoToOrderId(OrderListModel model)
    {
        Order order = null;
        int.TryParse(model.GoDirectlyToNumber, out var orderNumber);
        if (orderNumber > 0) order = await orderService.GetOrderByNumber(orderNumber);
        var orders = await orderService.GetOrdersByCode(model.GoDirectlyToNumber);
        switch (orders.Count)
        {
            case > 1:
                return RedirectToAction("List", new { Code = model.GoDirectlyToNumber });
            case 1:
                order = orders.FirstOrDefault();
                break;
        }

        if (order == null || await CheckSalesManager(order))
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");

        return RedirectToAction("Edit", "Order", new { id = order.Id });
    }

    #endregion

    #region Export

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> ExportExcelAll(OrderListModel model)
    {
        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer))
            model.StoreId = contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

        //load orders
        var orders = await orderViewModelService.PrepareOrders(model);
        try
        {
            var bytes = await exportManager.Export(orders);
            return File(bytes, "text/xls", "orders.xlsx");
        }
        catch (Exception exc)
        {
            Error(exc);
            return RedirectToAction("List");
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> ExportExcelSelected(string selectedIds)
    {
        var orders = new List<Order>();
        if (selectedIds != null)
        {
            var ids = selectedIds
                .Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x)
                .ToArray();
            orders.AddRange(await orderService.GetOrdersByIds(ids));
        }

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer))
            orders = orders.Where(x => x.StoreId == contextAccessor.WorkContext.CurrentCustomer.StaffStoreId).ToList();
        var bytes = await exportManager.Export(orders);
        return File(bytes, "text/xls", "orders.xlsx");
    }

    #endregion

    #region Order details

    #region Payments and other order workflow

    [PermissionAuthorizeAction(PermissionActionName.Cancel)]
    [HttpGet]
    public async Task<IActionResult> CancelOrder(string id)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");
        try
        {
            await mediator.Send(new CancelOrderCommand { Order = order, NotifyCustomer = true });

            Success("Successfully canceled order");
            return RedirectToAction("Edit", "Order", new { id });
        }
        catch (Exception exc)
        {
            //error
            Error(exc);
            return RedirectToAction("Edit", "Order", new { id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SaveOrderTags(OrderModel orderModel)
    {
        var order = await orderService.GetOrderById(orderModel.Id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");

        try
        {
            await orderViewModelService.SaveOrderTags(order, orderModel.OrderTags);

            var model = new OrderModel();
            await orderViewModelService.PrepareOrderDetailsModel(model, order);
            return RedirectToAction("Edit", "Order", new { id = order.Id });
        }
        catch (Exception exception)
        {
            //error
            Error(exception, false);
            return RedirectToAction("Edit", "Order", new { id = order.Id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ChangeOrderStatus(string id, OrderModel model)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");

        try
        {
            var status = await orderStatusService.GetByStatusId(model.OrderStatusId);
            ArgumentNullException.ThrowIfNull(status);

            order.OrderStatusId = model.OrderStatusId;
            await orderService.UpdateOrder(order);

            //add a note
            await orderService.InsertOrderNote(new OrderNote {
                Note = $"Order status has been edited. New status: {status.Name}",
                DisplayToCustomer = false,
                OrderId = order.Id
            });
            model = new OrderModel();
            await orderViewModelService.PrepareOrderDetailsModel(model, order);
            return RedirectToAction("Edit", "Order", new { id });
        }
        catch (Exception exc)
        {
            //error
            Error(exc, false);
            return RedirectToAction("Edit", "Order", new { id });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> VerifyOrder(string orderId)
    {
        try
        {
            var order = await orderService.GetOrderById(orderId);
            if (order == null)
                return Json(new { success = false, message = "Order not found" });

            if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer))
            {
                if (await CheckSalesManager(order))
                    return Json(new { success = false, message = "Access denied" });
            }

            if (order.IsVerifiedOrder)
                return Json(new { success = false, message = "Order is already verified" });

            // Update the order verification status
            order.IsVerifiedOrder = true;
            order.UpdatedOnUtc = DateTime.UtcNow;
            await orderService.UpdateOrder(order);

            // Add order note
            var orderNote = new OrderNote
            {
                Note = "Order verified by employee and moved to fulfillment queue",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id
            };
            await orderService.InsertOrderNote(orderNote);

            return Json(new { success = true, message = "Order verified successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error verifying order: {ex.Message}" });
        }
    }

    #endregion

    #endregion

    #region Edit, delete

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Edit(string id)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || order.Deleted || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");

        var model = new OrderModel();
        await orderViewModelService.PrepareOrderDetailsModel(model, order);

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> Delete(OrderDeleteModel model)
    {
        var order = await orderService.GetOrderById(model.Id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (ModelState.IsValid)
        {
            await mediator.Send(new DeleteOrderCommand { Order = order });

            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Edit", "Order", new { model.Id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> DeleteSelected(
        ICollection<string> selectedIds,
        [FromServices] IShipmentService shipmentService)
    {
        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer))
            return RedirectToAction("List", "Order");

        if (selectedIds != null)
        {
            var orders = new List<Order>();
            orders.AddRange(await orderService.GetOrdersByIds(selectedIds.ToArray()));
            for (var i = 0; i < orders.Count; i++)
            {
                var order = orders[i];
                var shipments = await shipmentService.GetShipmentsByOrder(order.Id);
                if (shipments.Any())
                    Error("Some orders is in associated with shipments. Please delete it first.");

                if (!shipments.Any()) await mediator.Send(new DeleteOrderCommand { Order = order });
            }
        }

        return Json(new { Result = true });
    }

    public async Task<IActionResult> PdfInvoice(string orderId)
    {
        var order = await orderService.GetOrderById(orderId);
        if ((await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
             order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) ||
            await CheckSalesManager(order)) return RedirectToAction("List");

        var orders = new List<Order> {
            order
        };
        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await pdfService.PrintOrdersToPdf(stream, orders, contextAccessor.WorkContext.WorkingLanguage.Id);
            bytes = stream.ToArray();
        }

        // Setting inline disposition to view the PDF in browser
        Response.Headers.Append("Content-Disposition", $"inline; filename=order_{order.Id}.pdf");
        return File(bytes, "application/pdf");
    }

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> PdfInvoiceAll(OrderListModel model)
    {
        //load orders
        var orders = await orderViewModelService.PrepareOrders(model);
        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer))
            orders = orders.Where(x => x.StoreId == contextAccessor.WorkContext.CurrentCustomer.StaffStoreId).ToList();

        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await pdfService.PrintOrdersToPdf(stream, orders, contextAccessor.WorkContext.WorkingLanguage.Id, model.VendorId);
            bytes = stream.ToArray();
        }

        // Setting inline disposition to view the PDF in browser
        Response.Headers.Append("Content-Disposition", "inline; filename=orders.pdf");
        return File(bytes, "application/pdf");
    }

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> PdfInvoiceSelected(string selectedIds)
    {
        var orders = new List<Order>();
        if (selectedIds != null)
        {
            var ids = selectedIds
                .Split([','], StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x)
                .ToArray();
            orders.AddRange(await orderService.GetOrdersByIds(ids));
        }

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer))
            orders = orders.Where(x => x.StoreId == contextAccessor.WorkContext.CurrentCustomer.StaffStoreId).ToList();

        //ensure that we at least one order selected
        if (orders.Count == 0)
        {
            Error(translationService.GetResource("Admin.Orders.PdfInvoice.NoOrders"));
            return RedirectToAction("List");
        }

        byte[] bytes;
        using (var stream = new MemoryStream())
        {
            await pdfService.PrintOrdersToPdf(stream, orders, contextAccessor.WorkContext.WorkingLanguage.Id);
            bytes = stream.ToArray();
        }

        // Setting inline disposition to view the PDF in browser
        Response.Headers.Append("Content-Disposition", "inline; filename=orders.pdf");
        return File(bytes, "application/pdf");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> EditOrderTotals(string id, OrderModel model)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });

        order.OrderSubtotalInclTax = model.OrderSubtotalInclTaxValue;
        order.OrderSubtotalExclTax = model.OrderSubtotalExclTaxValue;
        order.OrderSubTotalDiscountInclTax = model.OrderSubTotalDiscountInclTaxValue;
        order.OrderSubTotalDiscountExclTax = model.OrderSubTotalDiscountExclTaxValue;
        order.OrderShippingInclTax = model.OrderShippingInclTaxValue;
        order.OrderShippingExclTax = model.OrderShippingExclTaxValue;
        order.PaymentMethodAdditionalFeeInclTax = model.PaymentMethodAdditionalFeeInclTaxValue;
        order.PaymentMethodAdditionalFeeExclTax = model.PaymentMethodAdditionalFeeExclTaxValue;
        order.OrderTax = model.TaxValue;
        order.OrderDiscount = model.OrderTotalDiscountValue;
        order.OrderTotal = model.OrderTotalValue;
        order.CurrencyRate = model.CurrencyRate;
        await orderService.UpdateOrder(order);

        //add a note
        await orderService.InsertOrderNote(new OrderNote {
            Note = "Order totals have been edited",
            DisplayToCustomer = false,
            OrderId = order.Id
        });

        await orderViewModelService.PrepareOrderDetailsModel(model, order);
        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> EditShippingMethod(string id, OrderModel model)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });

        order.ShippingMethod = model.ShippingMethod;
        await orderService.UpdateOrder(order);

        //add a note
        await orderService.InsertOrderNote(new OrderNote {
            Note = "Shipping method has been edited",
            DisplayToCustomer = false,
            OrderId = order.Id
        });
        await orderViewModelService.PrepareOrderDetailsModel(model, order);

        //selected tab
        await SaveSelectedTabIndex(persistForTheNextRequest: true);

        return RedirectToAction("Edit", "Order", new { id });
    }

    [HttpPost]
    public async Task<IActionResult> EditUserFields(string id, OrderModel model)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });

        order.UserFields = model.UserFields;

        await orderService.UpdateOrder(order);

        await orderViewModelService.PrepareOrderDetailsModel(model, order);

        //selected tab
        await SaveSelectedTabIndex(persistForTheNextRequest: true);

        return RedirectToAction("Edit", "Order", new { id });
    }
    
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSalesEmployee(string id, string salesEmployeeId)
    {
        // Enhanced debug information
        System.Diagnostics.Debug.WriteLine($"SaveSalesEmployee called - Order ID: {id}, Sales Employee ID: {salesEmployeeId}");
        System.Diagnostics.Debug.WriteLine($"Request form data: {string.Join(", ", Request.Form.Select(x => $"{x.Key}={x.Value}"))}");
        
        // If salesEmployeeId is not provided directly, try to get it from form
        if (string.IsNullOrEmpty(salesEmployeeId) && Request.Form.ContainsKey("salesEmployeeId"))
        {
            salesEmployeeId = Request.Form["salesEmployeeId"].ToString();
            System.Diagnostics.Debug.WriteLine($"Retrieved salesEmployeeId from form: {salesEmployeeId}");
        }
        
        try 
        {
            var order = await orderService.GetOrderById(id);
            if (order == null || await CheckSalesManager(order))
            {
                System.Diagnostics.Debug.WriteLine("Order not found or CheckSalesManager failed");
                //No order found with the specified id
                return Json(new { success = false, message = "Order not found" });
            }

            if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
                order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            {
                System.Diagnostics.Debug.WriteLine("Staff permission check failed");
                return Json(new { success = false, message = "Access denied" });
            }

            // Update sales employee
            System.Diagnostics.Debug.WriteLine($"Current SeId: {order.SeId}, New SeId: {salesEmployeeId}");
            order.SeId = salesEmployeeId;
            await orderService.UpdateOrder(order);
            System.Diagnostics.Debug.WriteLine($"Order updated with new SeId: {salesEmployeeId}");

            // Add a note
            await orderService.InsertOrderNote(new OrderNote {
                Note = string.IsNullOrEmpty(salesEmployeeId) ? 
                    "Sales representative has been removed from this order." :
                    $"Sales representative has been assigned to this order. Sales employee ID: {salesEmployeeId}",
                DisplayToCustomer = false,
                OrderId = order.Id
            });
            System.Diagnostics.Debug.WriteLine("Order note added successfully");
            
            // Return success response for AJAX
            return Json(new { success = true, message = "Sales representative has been updated successfully" });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in SaveSalesEmployee: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // Return error for AJAX
            return Json(new { success = false, message = $"Error saving sales employee: {ex.Message}" });
        }
    }
    
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveImpersonatedEmployee(string id, string impersonatedEmployeeId)
    {
        try 
        {
            var order = await orderService.GetOrderById(id);
            if (order == null || await CheckSalesManager(order))
            {
                // No order found with the specified id
                return Json(new { success = false, message = "Order not found" });
            }

            if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
                order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            {
                return Json(new { success = false, message = "Access denied" });
            }

            // Update impersonated employee
            order.ImpersonatedByEmployeeId = impersonatedEmployeeId;
            await orderService.UpdateOrder(order);

            // Add a note
            await orderService.InsertOrderNote(new OrderNote {
                Note = string.IsNullOrEmpty(impersonatedEmployeeId) ? 
                    "Impersonated employee has been removed from this order." :
                    $"Impersonated employee has been assigned to this order. Employee ID: {impersonatedEmployeeId}",
                DisplayToCustomer = false,
                OrderId = order.Id
            });
            
            // Return success response for AJAX
            return Json(new { success = true, message = "Impersonated employee has been updated successfully" });
        }
        catch (Exception ex)
        {
            // Return error for AJAX
            return Json(new { success = false, message = $"Error saving impersonated employee: {ex.Message}" });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SaveOrderItem(string id, OrderItemsModel model)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });
        if (order.OrderStatusId == (int)OrderStatusSystem.Cancelled)
        {
            Error("You can't edit position when order is canceled");
            return RedirectToAction("Edit", "Order", new { id });
        }

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == model.OrderItemId) ?? throw new ArgumentException("No order item found with the specified id");
        var itemModel = model.Items.FirstOrDefault(x => x.Id == model.OrderItemId) ?? throw new ArgumentException("No order item model found with the specified id");

        if (itemModel.Quantity == 0 || (orderItem.OpenQty != orderItem.Quantity && orderItem.IsShipEnabled))
        {
            Error("You can't change quantity");
            return RedirectToAction("Edit", "Order", new { id });
        }

        if (orderItem.Quantity == itemModel.Quantity && orderItem.UnitPriceExclTax == itemModel.UnitPriceExclTaxValue)
        {
            Error("Nothing has been changed");
            return RedirectToAction("Edit", "Order", new { id });
        }

        orderItem.Quantity = itemModel.Quantity;
        orderItem.OpenQty = itemModel.Quantity;

        if (orderItem.UnitPriceExclTax != itemModel.UnitPriceExclTaxValue)
        {
            orderItem.UnitPriceExclTax = itemModel.UnitPriceExclTaxValue;
            orderItem.UnitPriceInclTax =
                Math.Round(orderItem.UnitPriceExclTax * orderItem.TaxRate / 100 + orderItem.UnitPriceExclTax, 2);
            orderItem.PriceInclTax = Math.Round(orderItem.UnitPriceInclTax * orderItem.Quantity, 2);
            orderItem.PriceExclTax = Math.Round(orderItem.UnitPriceExclTax * orderItem.Quantity, 2);

            orderItem.DiscountAmountInclTax = 0;
            orderItem.DiscountAmountExclTax = 0;
        }
        else
        {
            orderItem.PriceInclTax = Math.Round(orderItem.UnitPriceInclTax * orderItem.Quantity, 2);
            orderItem.PriceExclTax = Math.Round(orderItem.UnitPriceExclTax * orderItem.Quantity, 2);

            orderItem.DiscountAmountInclTax = 0;
            orderItem.DiscountAmountExclTax = 0;
        }

        await mediator.Send(new UpdateOrderItemCommand { Order = order, OrderItem = orderItem });

        //selected tab
        await SaveSelectedTabIndex(persistForTheNextRequest: true);

        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> UpdateOrderItemField(string orderId, string orderItemId, string fieldType, string value, [FromServices] ICurrencyService currencyService)
    {
        try
        {
            var order = await orderService.GetOrderById(orderId);
            if (order == null || await CheckSalesManager(order))
                return Json(new { success = false, message = "Order not found or access denied" });

            if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
                order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
                return Json(new { success = false, message = "Access denied" });

            if (order.OrderStatusId == (int)OrderStatusSystem.Cancelled)
                return Json(new { success = false, message = "Cannot edit cancelled order" });

            var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
            if (orderItem == null)
                return Json(new { success = false, message = "Order item not found" });

            // Validate and parse the new value
            if (fieldType == "quantity")
            {
                if (!int.TryParse(value, out var newQuantity) || newQuantity <= 0)
                    return Json(new { success = false, message = "Quantity must be a positive number" });

                if (orderItem.OpenQty != orderItem.Quantity && orderItem.IsShipEnabled)
                    return Json(new { success = false, message = "Cannot change quantity - item partially shipped" });

                if (orderItem.Quantity == newQuantity)
                    return Json(new { success = false, message = "No change detected" });

                // Update quantity
                orderItem.Quantity = newQuantity;
                orderItem.OpenQty = newQuantity;
                orderItem.PriceInclTax = Math.Round(orderItem.UnitPriceInclTax * orderItem.Quantity, 2);
                orderItem.PriceExclTax = Math.Round(orderItem.UnitPriceExclTax * orderItem.Quantity, 2);
                orderItem.DiscountAmountInclTax = 0;
                orderItem.DiscountAmountExclTax = 0;
            }
            else if (fieldType == "price")
            {
                if (!double.TryParse(value, out var newPrice) || newPrice < 0)
                    return Json(new { success = false, message = "Price must be a valid decimal number" });

                if (Math.Abs(orderItem.UnitPriceExclTax - newPrice) < 0.01)
                    return Json(new { success = false, message = "No change detected" });

                // Update price
                orderItem.UnitPriceExclTax = newPrice;
                orderItem.UnitPriceInclTax = Math.Round(orderItem.UnitPriceExclTax * orderItem.TaxRate / 100 + orderItem.UnitPriceExclTax, 2);
                orderItem.PriceInclTax = Math.Round(orderItem.UnitPriceInclTax * orderItem.Quantity, 2);
                orderItem.PriceExclTax = Math.Round(orderItem.UnitPriceExclTax * orderItem.Quantity, 2);
                orderItem.DiscountAmountInclTax = 0;
                orderItem.DiscountAmountExclTax = 0;
            }
            else
            {
                return Json(new { success = false, message = "Invalid field type" });
            }

            // Save the changes
            await mediator.Send(new UpdateOrderItemCommand { Order = order, OrderItem = orderItem });

            // Prepare response with updated values
            var primaryCurrency = await currencyService.GetPrimaryStoreCurrency();
            
            var response = new
            {
                success = true,
                message = "Saved successfully",
                newSubTotal = order.CustomerTaxDisplayTypeId == (int)TaxDisplayType.IncludingTax ? 
                    orderItem.PriceInclTax.ToString("C", new CultureInfo(primaryCurrency.DisplayLocale ?? "en-US")) :
                    orderItem.PriceExclTax.ToString("C", new CultureInfo(primaryCurrency.DisplayLocale ?? "en-US")),
                displayValue = fieldType == "price" ? 
                    (order.CustomerTaxDisplayTypeId == (int)TaxDisplayType.IncludingTax ? 
                        orderItem.UnitPriceInclTax.ToString("C", new CultureInfo(primaryCurrency.DisplayLocale ?? "en-US")) :
                        orderItem.UnitPriceExclTax.ToString("C", new CultureInfo(primaryCurrency.DisplayLocale ?? "en-US"))) : null
            };

            return Json(response);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error saving changes: {ex.Message}" });
        }
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> DeleteOrderItem(string id, string orderItemId)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
        if (orderItem == null)
            throw new ArgumentException("No order item found with the specified id");

        var result = await mediator.Send(new DeleteOrderItemCommand { Order = order, OrderItem = orderItem });
        if (result.error)
            Error(result.message);

        //selected tab
        await SaveSelectedTabIndex(persistForTheNextRequest: true);

        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CancelOrderItem(string id, string orderItemId)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
        if (orderItem == null)
            throw new ArgumentException("No order item found with the specified id");


        var result = await mediator.Send(new CancelOrderItemCommand { Order = order, OrderItem = orderItem });
        if (result.error)
            Error(result.message);
        else
            Success("The order item was successfully canceled");

        //selected tab
        await SaveSelectedTabIndex(persistForTheNextRequest: true);

        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ResetDownloadCount(string id, string orderItemId)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
        if (orderItem == null)
            throw new ArgumentException("No order item found with the specified id");

        orderItem.DownloadCount = 0;
        await orderService.UpdateOrder(order);
        var model = new OrderModel();
        await orderViewModelService.PrepareOrderDetailsModel(model, order);

        //selected tab
        await SaveSelectedTabIndex(persistForTheNextRequest: true);

        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> ActivateDownloadItem(string id, string orderItemId)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
        if (orderItem == null)
            throw new ArgumentException("No order item found with the specified id");

        orderItem.IsDownloadActivated = !orderItem.IsDownloadActivated;
        await orderService.UpdateOrder(order);
        var model = new OrderModel();
        await orderViewModelService.PrepareOrderDetailsModel(model, order);

        //selected tab
        await SaveSelectedTabIndex(persistForTheNextRequest: true);

        return RedirectToAction("Edit", "Order", new { id });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> UploadLicenseFilePopup(string id, string orderItemId,
        [FromServices] IProductService productService)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id });

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
        if (orderItem == null)
            throw new ArgumentException("No order item found with the specified id");

        var product = await productService.GetProductByIdIncludeArch(orderItem.ProductId);

        if (!product.IsDownload)
            throw new ArgumentException("Product is not downloadable");
        var model = new OrderModel.UploadLicenseModel {
            LicenseDownloadId = !string.IsNullOrEmpty(orderItem.LicenseDownloadId) ? orderItem.LicenseDownloadId : "",
            OrderId = order.Id,
            OrderItemId = orderItem.Id
        };

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> UploadLicenseFilePopup(OrderModel.UploadLicenseModel model)
    {
        var order = await orderService.GetOrderById(model.OrderId);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id = order.Id });

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == model.OrderItemId);
        if (orderItem == null)
            throw new ArgumentException("No order item found with the specified id");

        //attach license
        orderItem.LicenseDownloadId = !string.IsNullOrEmpty(model.LicenseDownloadId) ? model.LicenseDownloadId : null;
        await orderService.UpdateOrder(order);

        //success
        ViewBag.RefreshPage = true;

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> DeleteLicenseFilePopup(OrderModel.UploadLicenseModel model)
    {
        var order = await orderService.GetOrderById(model.OrderId);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            return RedirectToAction("Edit", "Order", new { id = model.OrderId });

        var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == model.OrderItemId);
        if (orderItem == null)
            throw new ArgumentException("No order item found with the specified id");

        //attach license
        orderItem.LicenseDownloadId = null;
        await orderService.UpdateOrder(order);

        //success
        ViewBag.RefreshPage = true;

        return RedirectToAction("Edit", "Order", new { id = model.OrderId });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AddProductToOrder(string orderId)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        var model = await orderViewModelService.PrepareAddOrderProductModel(order);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddProductToOrder(DataSourceRequest command, OrderModel.AddOrderProductModel model,
        [FromServices] IProductService productService)
    {
        var categoryIds = new List<string>();
        if (!string.IsNullOrEmpty(model.SearchCategoryId))
            categoryIds.Add(model.SearchCategoryId);

        var storeId = string.Empty;
        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer)) storeId = contextAccessor.WorkContext.CurrentCustomer.StaffStoreId;

        var gridModel = new DataSourceResult();
        var products = (await productService.SearchProducts(categoryIds: categoryIds,
            storeId: storeId,
            brandId: model.SearchBrandId,
            collectionId: model.SearchCollectionId,
            productType: model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null,
            keywords: model.SearchProductName,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize,
            showHidden: true)).products;
        gridModel.Data = products.Select(x =>
        {
            var productModel = new OrderModel.AddOrderProductModel.ProductModel {
                Id = x.Id,
                Name = x.Name,
                Sku = x.Sku
            };

            return productModel;
        });
        gridModel.Total = products.TotalCount;

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> AddProductToOrderDetails(string orderId, string productId)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");

        var model = await orderViewModelService.PrepareAddProductToOrderModel(order, productId);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddProductToOrderDetails(AddProductToOrderModel model)
    {
        var order = await orderService.GetOrderById(model.OrderId);
        if (order == null || await CheckSalesManager(order))
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");

        var warnings = await orderViewModelService.AddProductToOrderDetails(model);
        if (!warnings.Any())
        {
            //redirect to order details page - stay on Products tab (tab-index 3)
            TempData["SelectedTabIndex"] = 3;
            return RedirectToAction("Edit", "Order", new { id = model.OrderId });
        }

        //errors
        var result = await orderViewModelService.PrepareAddProductToOrderModel(order, model.ProductId);
        result.Warnings.AddRange(warnings);
        return View(result);
    }

    #region Bulk Product Addition

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> BulkAddProductsToOrder(string orderId)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) 
            return RedirectToAction("List");

        var model = await orderViewModelService.PrepareBulkAddProductsToOrderModel(order);
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BulkProductSearch(DataSourceRequest command, BulkAddProductsToOrderModel model,
        [FromServices] IProductService productService)
    {
        var categoryIds = new List<string>();
        if (!string.IsNullOrEmpty(model.SearchCategoryId))
            categoryIds.Add(model.SearchCategoryId);

        var searchResult = await productService.SearchProducts(categoryIds: categoryIds,
            storeId: "",
            brandId: model.SearchBrandId,
            collectionId: model.SearchCollectionId,
            productType: model.SearchProductTypeId > 0 ? (ProductType?)model.SearchProductTypeId : null,
            keywords: model.SearchProductName,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize,
            showHidden: true);

        // Filter out grouped products
        var filteredProducts = searchResult.products.Where(x => x.ProductTypeId != ProductType.GroupedProduct).ToList();

        var gridModel = new DataSourceResult {
            Data = filteredProducts.Select(x => new OrderModel.AddOrderProductModel.ProductModel {
                Id = x.Id,
                Name = x.Name
            }),
            Total = filteredProducts.Count
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> GetProductConfigurationRows(string[] productIds, string orderId)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            return Json(new { success = false });

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            return Json(new { success = false });

        var html = await orderViewModelService.GetProductConfigurationRowsHtml(productIds, orderId);
        return Json(new { success = true, html });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> BulkAddProductsToOrder(BulkAddProductsToOrderModel model)
    {
        var order = await orderService.GetOrderById(model.OrderId);
        if (order == null || await CheckSalesManager(order))
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) 
            return RedirectToAction("List");

        var warnings = await orderViewModelService.ProcessBulkProductAddition(model);
        if (!warnings.Any())
        {
            //redirect to order details page - stay on Products tab (tab-index 3)
            TempData["SelectedTabIndex"] = 3;
            return RedirectToAction("Edit", "Order", new { id = model.OrderId });
        }

        // If there are warnings, reload the page with errors
        var reloadedModel = await orderViewModelService.PrepareBulkAddProductsToOrderModel(order);
        reloadedModel.SearchProductName = model.SearchProductName;
        reloadedModel.SearchCategoryId = model.SearchCategoryId;
        reloadedModel.SearchBrandId = model.SearchBrandId;
        reloadedModel.SearchCollectionId = model.SearchCollectionId;
        reloadedModel.SearchProductTypeId = model.SearchProductTypeId;
        
        foreach (var warning in warnings)
            ModelState.AddModelError("", warning);
            
        return View(reloadedModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> GetCombinationWarehouseInventory(string combinationId, string productId, string orderId)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            return Json(new { success = false, message = "Order not found" });

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            return Json(new { success = false, message = "Access denied" });

        var warehouses = await orderViewModelService.GetCombinationWarehouseInventory(combinationId, productId);
        var combinationDetails = await orderViewModelService.GetCombinationDetails(combinationId, productId);
        
        return Json(new { 
            success = true, 
            warehouses = warehouses,
            overriddenPrice = combinationDetails.OverriddenPrice,
            sku = combinationDetails.Sku
        });
    }

    #endregion

    #region Addresses

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> AddressEdit(string addressId, string orderId, bool billingAddress)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");

        var address = new Address();
        switch (billingAddress)
        {
            case true when order.BillingAddress != null:
            {
                if (order.BillingAddress.Id == addressId)
                    address = order.BillingAddress;
                break;
            }
            case false when order.ShippingAddress != null:
            {
                if (order.ShippingAddress.Id == addressId)
                    address = order.ShippingAddress;
                break;
            }
        }

        if (address == null)
            throw new ArgumentException("No address found with the specified id", nameof(addressId));

        var model = await orderViewModelService.PrepareOrderAddressModel(order, address);
        model.BillingAddress = billingAddress;
        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> AddressEdit(OrderAddressModel model,
        [FromServices] IAddressAttributeService addressAttributeService,
        [FromServices] IAddressAttributeParser addressAttributeParser)
    {
        var order = await orderService.GetOrderById(model.OrderId);
        if (order == null || await CheckSalesManager(order))
            //No order found with the specified id
            return RedirectToAction("List");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) return RedirectToAction("List");

        var address = new Address();
        switch (model.BillingAddress)
        {
            case true when order.BillingAddress != null:
            {
                if (order.BillingAddress.Id == model.Address.Id)
                    address = order.BillingAddress;
                break;
            }
            case false when order.ShippingAddress != null:
            {
                if (order.ShippingAddress.Id == model.Address.Id)
                    address = order.ShippingAddress;
                break;
            }
        }

        if (ModelState.IsValid)
        {
            var customAttributes = new List<CustomAttribute>();
            
            // Get form values for custom attributes
            foreach (var attribute in await addressAttributeService.GetAllAddressAttributes())
            {
                string controlId = $"attributes[{attribute.Id}]";
                var attributeValue = Request.Form[controlId].ToString();
                
                if (!string.IsNullOrEmpty(attributeValue))
                {
                    if (attribute.AttributeControlTypeId == (int)AttributeControlType.Checkboxes)
                    {
                        foreach (var item in attributeValue.Split(','))
                        {
                            if (!string.IsNullOrEmpty(item))
                                customAttributes = addressAttributeParser.AddAddressAttribute(customAttributes, attribute, item).ToList();
                        }
                    }
                    else
                    {
                        customAttributes = addressAttributeParser.AddAddressAttribute(customAttributes, attribute, attributeValue).ToList();
                    }
                }
            }
            
            await orderViewModelService.UpdateOrderAddress(order, address, model, customAttributes);
            return RedirectToAction("AddressEdit",
                new { addressId = model.Address.Id, orderId = model.OrderId, model.BillingAddress });
        }

        //If we got this far, something failed, redisplay form
        model = await orderViewModelService.PrepareOrderAddressModel(order, address);
        return View(model);
    }

    #endregion

    #region Fulfillment
    
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpGet]
    public async Task<IActionResult> GetTargetDeliveryDate(string orderId)
    {
        if (string.IsNullOrEmpty(orderId))
            return Json(new { success = false });
            
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            return Json(new { success = false });
            
        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            return Json(new { success = false });
            
        if (order.TargetDeliveryDate.HasValue)
        {
            // Return the date directly without any adjustments
            string formattedDate = order.TargetDeliveryDate.Value.ToString("yyyy-MM-dd");
            
            // Debug info
            System.Diagnostics.Debug.WriteLine($"Target delivery date from DB: {order.TargetDeliveryDate.Value}");
            System.Diagnostics.Debug.WriteLine($"Formatted date for display: {formattedDate}");
            
            return Json(new { success = true, value = formattedDate });
        }
        
        return Json(new { success = false });
    }
    
    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SaveTargetDeliveryDate(string orderId, string date)
    {
        if (string.IsNullOrEmpty(orderId))
            return Json(new { success = false, error = "Missing required parameters" });
            
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            return Json(new { success = false, error = "Order not found" });
            
        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            return Json(new { success = false, error = "Access denied" });
        
        // Update target delivery date
        if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
        {
            // Store the date as is without adjustments
            order.TargetDeliveryDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            
            // Debug info
            System.Diagnostics.Debug.WriteLine($"Saving date from input: {date}");
            System.Diagnostics.Debug.WriteLine($"Parsed date: {parsedDate}");
            System.Diagnostics.Debug.WriteLine($"Saved date to DB: {order.TargetDeliveryDate}");
        }
        else
        {
            order.TargetDeliveryDate = null;
        }
        
        await orderService.UpdateOrder(order);
        
        return Json(new { success = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpGet]
    public async Task<IActionResult> GetRequestedShipmentDate(string orderId)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            return Json(new { success = false });

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) return Json(new { success = false });

        if (order.RequestedShipmentDate.HasValue)
        {
            string formattedDate = order.RequestedShipmentDate.Value.ToString("yyyy-MM-dd");
            return Json(new { success = true, value = formattedDate });
        }
        return Json(new { success = false });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SaveRequestedShipmentDate(string orderId, string date)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            return Json(new { success = false });

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) return Json(new { success = false });

        if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var parsedDate))
        {
            order.RequestedShipmentDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
        }
        else
        {
            order.RequestedShipmentDate = null;
        }
        
        await orderService.UpdateOrder(order);
        
        return Json(new { success = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> CreateFulfillmentShipment(string orderId, string orderItemIds, string quantities, string targetDeliveryDate,
        [FromServices] IShipmentService shipmentService)
    {
        if (string.IsNullOrEmpty(orderItemIds))
            return Json(new { success = false, error = "No items selected" });

        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            return Json(new { success = false, error = "Order not found" });

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            return Json(new { success = false, error = "Access denied" });
        
        // Store target delivery date in the order
        if (!string.IsNullOrEmpty(targetDeliveryDate) && DateTime.TryParse(targetDeliveryDate, out var parsedDate))
        {
            // Store the date as is without adjustments
            order.TargetDeliveryDate = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
            await orderService.UpdateOrder(order);
            
            // Debug info
            System.Diagnostics.Debug.WriteLine($"Create Shipment - Date from input: {targetDeliveryDate}");
            System.Diagnostics.Debug.WriteLine($"Create Shipment - Parsed date: {parsedDate}");
            System.Diagnostics.Debug.WriteLine($"Create Shipment - Saved to DB: {order.TargetDeliveryDate}");
        }

        var selectedOrderItemIds = orderItemIds.Split(',');
        
        // Parse quantities from JSON
        Dictionary<string, string> quantityMap = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(quantities))
        {
            try
            {
                quantityMap = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(quantities);
            }
            catch
            {
                // If parsing fails, we'll use default quantities
            }
        }
        
        // Create a new shipment
        try
        {
            var shipment = new Shipment
            {
                OrderId = orderId,
                StoreId = order.StoreId,
                VendorId = contextAccessor.WorkContext.CurrentVendor?.Id,
                SeId = contextAccessor.WorkContext.CurrentCustomer.SeId,
                TrackingNumber = "",
                TotalWeight = null,
                ShippedDateUtc = null,
                DeliveryDateUtc = null,
                AdminComment = "Created from Fulfillment Queue",
                CreatedOnUtc = DateTime.UtcNow
            };

        // Add items to shipment
        foreach (var orderItemId in selectedOrderItemIds)
        {
            var orderItem = order.OrderItems.FirstOrDefault(x => x.Id == orderItemId);
            if (orderItem == null || orderItem.OpenQty <= 0)
                continue;

            // Get the quantity to ship from the provided quantities JSON
            double shipQty = orderItem.OpenQty;
            if (quantityMap.TryGetValue(orderItemId, out var qtyStr) &&
                double.TryParse(qtyStr, out var parsedQty) &&
                parsedQty > 0 && parsedQty <= orderItem.OpenQty)
            {
                shipQty = parsedQty;
            }

            var shipmentItem = new ShipmentItem
            {
                OrderItemId = orderItemId,
                ProductId = orderItem.ProductId,
                Quantity = shipQty,
                WarehouseId = orderItem.WarehouseId,
                Attributes = orderItem.Attributes
            };

            shipment.ShipmentItems.Add(shipmentItem);
        }

        if (!shipment.ShipmentItems.Any())
            return Json(new { success = false, error = "No valid items to ship" });

        // Insert shipment
        await shipmentService.InsertShipment(shipment);

        // Add a note
        await orderService.InsertOrderNote(new OrderNote
        {
            Note = !string.IsNullOrEmpty(targetDeliveryDate) && DateTime.TryParse(targetDeliveryDate, out var noteDate) ? 
                $"Shipment #{shipment.ShipmentNumber} has been created from Fulfillment Queue with target delivery date: {noteDate:yyyy-MM-dd}" :
                $"Shipment #{shipment.ShipmentNumber} has been created from Fulfillment Queue",
            DisplayToCustomer = false,
            OrderId = order.Id,
            CreatedOnUtc = DateTime.UtcNow
        });
        
        return Json(new { success = true });
        }
        catch (Exception ex)
        {
            // Log the exception
            System.Diagnostics.Debug.WriteLine($"Error creating fulfillment shipment: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            
            return Json(new { success = false, error = $"Error creating shipment: {ex.Message}" });
        }
    }

    #endregion

    #region User Fields
    
    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    [HttpGet]
    public async Task<IActionResult> UserFieldsTab(string id)
    {
        var order = await orderService.GetOrderById(id);
        if (order == null || await CheckSalesManager(order))
            return Content("Order not found");
            
        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId)
            return Content("Access denied");
            
        var model = new OrderModel();
        await orderViewModelService.PrepareOrderDetailsModel(model, order);
        
        return View("Partials/_UserFieldsTab", model);
    }

    #endregion

    #region Order notes

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> OrderNotesSelect(string orderId, DataSourceRequest command)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            throw new ArgumentException("No order found with the specified id");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) return Content("");
        //order notes
        var orderNoteModels = await orderViewModelService.PrepareOrderNotes(order);
        var gridModel = new DataSourceResult {
            Data = orderNoteModels,
            Total = orderNoteModels.Count
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    public async Task<IActionResult> OrderNoteAdd(string orderId, string downloadId, bool displayToCustomer,
        bool includeOnInvoice, string message)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            return Json(new { Result = false });

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) return Json(new { Result = false });
        await orderViewModelService.InsertOrderNote(order, downloadId, displayToCustomer, includeOnInvoice, message);

        return Json(new { Result = true });
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> OrderNoteDelete(string id, string orderId)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            throw new ArgumentException("No order found with the specified id");

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) return Json(new { Result = false });

        await orderViewModelService.DeleteOrderNote(order, id);

        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> OrderNoteUpdate(string id, string orderId, bool? displayToCustomer, bool? includeOnInvoice)
    {
        var order = await orderService.GetOrderById(orderId);
        if (order == null || await CheckSalesManager(order))
            return Json(new { Result = false });

        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer) &&
            order.StoreId != contextAccessor.WorkContext.CurrentCustomer.StaffStoreId) return Json(new { Result = false });

        await orderViewModelService.UpdateOrderNote(order, id, displayToCustomer, includeOnInvoice);

        return Json(new { Result = true });
    }

    #endregion
    
    #region Impersonated Orders

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> RecentImpersonatedOrdersList(DataSourceRequest command)
    {
        if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
            return Json(new { Data = new List<OrderModel>(), Total = 0 });

        // We display only orders that were impersonated by the current user
        var model = new OrderListModel
        {
            // Filter by the currently logged in user as the impersonator
            ImpersonatedByEmployeeId = contextAccessor.WorkContext.CurrentCustomer.Id,
            StartDate = DateTime.UtcNow.AddDays(-30) // Show orders from the last 30 days
        };

        var (orderModels, totalCount) =
            await orderViewModelService.PrepareOrderModel(model, command.Page, command.PageSize);

        var gridModel = new DataSourceResult
        {
            Data = orderModels.ToList(),
            Total = totalCount
        };

        return Json(gridModel);
    }

    #endregion
    
    #region Export CSV
    
    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> ExportCsv(OrderListModel model)
    {
        // Load orders using existing service
        var orders = await orderViewModelService.PrepareOrders(model);
        
        if (await groupService.IsStaff(contextAccessor.WorkContext.CurrentCustomer))
            orders = orders.Where(x => x.StoreId == contextAccessor.WorkContext.CurrentCustomer.StaffStoreId).ToList();
        
        // Build CSV content
        var csv = new StringBuilder();
        
        // Add CSV header
        csv.AppendLine("OrderNumber,CustomerName,OrderDate,OrderTotal,PaymentStatus");
        
        // Add order data
        foreach (var order in orders)
        {
            var customerName = $"{order.FirstName} {order.LastName}".Trim();
            
            if (string.IsNullOrEmpty(customerName))
                customerName = order.CustomerEmail ?? "Guest";
            
            // Get payment status text
            string paymentStatus = order.PaymentStatusId switch
            {
                PaymentStatus.Paid => "Paid",
                PaymentStatus.Pending => "Pending",
                PaymentStatus.PartiallyPaid => "Partially Paid",
                PaymentStatus.PartiallyRefunded => "Partially Refunded",
                PaymentStatus.Refunded => "Refunded",
                PaymentStatus.Voided => "Voided",
                _ => "Unknown"
            };
            
            var line = new List<string>
            {
                EscapeCsvField(order.OrderNumber.ToString()),
                EscapeCsvField(customerName),
                EscapeCsvField(order.CreatedOnUtc.ToString("yyyy-MM-dd")),
                EscapeCsvField(order.OrderTotal.ToString("0.00", CultureInfo.InvariantCulture)),
                EscapeCsvField(paymentStatus)
            };
            
            csv.AppendLine(string.Join(",", line));
        }
        
        // Return CSV file
        var fileName = $"orders_{DateTime.Now:yyyy-MM-dd}.csv";
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;
            
        // Escape quotes and wrap field in quotes if it contains comma, quotes or newlines
        bool needsQuotes = field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r');
        if (needsQuotes)
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        
        return field;
    }
    
    #endregion
    
    #endregion
}