using Grand.Web.Admin.Models.Orders;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Components.OrderProductsTable;

public class OrderProductsTableViewComponent : BaseAdminViewComponent
{
    public IViewComponentResult Invoke(OrderModel model, bool showAddProducts = true, bool showSummary = true, bool collapsible = true)
    {
        var viewModel = new OrderProductsTableModel
        {
            OrderId = model.Id,
            Items = model.Items,
            TaxDisplayType = model.TaxDisplayType,
            OrderTotal = model.OrderTotal,
            OrderSubtotalInclTax = model.OrderSubtotalInclTax,
            OrderSubtotalExclTax = model.OrderSubtotalExclTax,
            OrderShippingInclTax = model.OrderShippingInclTax,
            OrderShippingExclTax = model.OrderShippingExclTax,
            OrderTax = model.Tax,
            ShowAddProducts = showAddProducts,
            ShowSummary = showSummary,
            Collapsible = collapsible
        };

        return View(viewModel);
    }
}

public class OrderProductsTableModel
{
    public string OrderId { get; set; }
    public IList<OrderModel.OrderItemModel> Items { get; set; }
    public Grand.Domain.Tax.TaxDisplayType TaxDisplayType { get; set; }
    public string OrderTotal { get; set; }
    public string OrderSubtotalInclTax { get; set; }
    public string OrderSubtotalExclTax { get; set; }
    public string OrderShippingInclTax { get; set; }
    public string OrderShippingExclTax { get; set; }
    public string OrderTax { get; set; }
    public bool ShowAddProducts { get; set; }
    public bool ShowSummary { get; set; }
    public bool Collapsible { get; set; }
}