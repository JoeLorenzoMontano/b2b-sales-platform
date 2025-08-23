using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Web.Admin.Models.Components;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Components.ProductSearchAdd;

public class ProductSearchAddViewComponent : BaseAdminViewComponent
{
    private readonly IBrandService _brandService;

    public ProductSearchAddViewComponent(IBrandService brandService)
    {
        _brandService = brandService;
    }

    public async Task<IViewComponentResult> InvokeAsync(string contextId = "", 
        string searchEndpoint = "SearchProductsInline", 
        string addEndpoint = "AddProductToOrderInline",
        string contextType = "order")
    {
        var model = new ProductSearchAddModel
        {
            ContextId = contextId,
            SearchEndpoint = searchEndpoint,
            AddEndpoint = addEndpoint,
            ContextType = contextType
        };

        // Populate brands dropdown
        await PopulateBrandsDropdown(model);

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
}