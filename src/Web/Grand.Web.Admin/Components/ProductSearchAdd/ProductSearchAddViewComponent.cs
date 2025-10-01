using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Web.Admin.Models.Components;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Components.ProductSearchAdd;

public class ProductSearchAddViewComponent : BaseAdminViewComponent
{
    private readonly IBrandService _brandService;
    private readonly ICategoryService _categoryService;

    public ProductSearchAddViewComponent(IBrandService brandService, ICategoryService categoryService)
    {
        _brandService = brandService;
        _categoryService = categoryService;
    }

    public async Task<IViewComponentResult> InvokeAsync(string contextId = "",
        string orderId = null,
        string searchEndpoint = "SearchProductsInline",
        string addEndpoint = "AddProductToOrderInline",
        string contextType = "order")
    {
        // Extract actual order ID from contextId if orderId not provided
        var actualOrderId = orderId;
        if (string.IsNullOrEmpty(actualOrderId) && !string.IsNullOrEmpty(contextId))
        {
            // Remove prefixes like 'incoming-' or 'reverification-' to get actual order ID
            if (contextId.StartsWith("incoming-"))
                actualOrderId = contextId.Substring("incoming-".Length);
            else if (contextId.StartsWith("reverification-"))
                actualOrderId = contextId.Substring("reverification-".Length);
            else
                actualOrderId = contextId; // Use as-is if no known prefix
        }

        var model = new ProductSearchAddModel
        {
            ContextId = contextId,
            OrderId = actualOrderId ?? contextId, // Fallback to contextId if extraction failed
            SearchEndpoint = searchEndpoint,
            AddEndpoint = addEndpoint,
            ContextType = contextType
        };

        // Populate brands and categories dropdowns
        await PopulateBrandsDropdown(model);
        await PopulateCategoriesDropdown(model);

        return View(model);
    }

    private async Task PopulateBrandsDropdown(ProductSearchAddModel model)
    {
        // Populate brands
        model.AvailableBrands.Add(new SelectListItem { Text = "All", Value = "" });
        var brands = await _brandService.GetAllBrands(showHidden: true);
        foreach (var brand in brands)
        {
            model.AvailableBrands.Add(new SelectListItem
            {
                Text = brand.Name,
                Value = brand.Id
            });
        }
    }

    private async Task PopulateCategoriesDropdown(ProductSearchAddModel model)
    {
        // Populate categories
        model.AvailableCategories.Add(new SelectListItem { Text = "All", Value = "" });
        var categories = await _categoryService.GetAllCategories(showHidden: true);
        foreach (var category in categories)
        {
            model.AvailableCategories.Add(new SelectListItem
            {
                Text = category.Name,
                Value = category.Id
            });
        }
    }
}