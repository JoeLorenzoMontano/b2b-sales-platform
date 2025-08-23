using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Categories;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Web.Admin.Models.Components;
using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Components.ProductSearchAdd;

public class ProductSearchAddViewComponent : BaseAdminViewComponent
{
    private readonly IBrandService _brandService;
    private readonly ICategoryService _categoryService;
    private readonly ICollectionService _collectionService;
    private readonly IProductService _productService;

    public ProductSearchAddViewComponent(
        IBrandService brandService,
        ICategoryService categoryService,
        ICollectionService collectionService,
        IProductService productService)
    {
        _brandService = brandService;
        _categoryService = categoryService;
        _collectionService = collectionService;
        _productService = productService;
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

        // Populate filter dropdowns
        await PopulateDropdowns(model);

        return View(model);
    }

    private async Task PopulateDropdowns(ProductSearchAddModel model)
    {
        // Populate categories
        model.AvailableCategories.Add(new SelectListItem { Text = "All", Value = "" });
        var categories = await _categoryService.GetAllCategories(showHidden: true);
        foreach (var category in categories)
        {
            model.AvailableCategories.Add(new SelectListItem
            {
                Text = await _categoryService.GetFormattedBreadCrumb(category),
                Value = category.Id
            });
        }

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

        // Populate collections
        model.AvailableCollections.Add(new SelectListItem { Text = "All", Value = "" });
        var collections = await _collectionService.GetAllCollections(showHidden: true);
        foreach (var collection in collections)
        {
            model.AvailableCollections.Add(new SelectListItem
            {
                Text = collection.Name,
                Value = collection.Id
            });
        }

        // Populate product types
        model.AvailableProductTypes.Add(new SelectListItem { Text = "All", Value = "0" });
        model.AvailableProductTypes.Add(new SelectListItem { Text = "Simple product", Value = "5" });
        model.AvailableProductTypes.Add(new SelectListItem { Text = "Grouped product", Value = "10" });
        model.AvailableProductTypes.Add(new SelectListItem { Text = "Reservation", Value = "15" });
        model.AvailableProductTypes.Add(new SelectListItem { Text = "Bundle product", Value = "20" });
        model.AvailableProductTypes.Add(new SelectListItem { Text = "Auction", Value = "25" });
    }
}