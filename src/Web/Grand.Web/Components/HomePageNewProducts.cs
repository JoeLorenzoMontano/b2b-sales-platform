using Grand.Business.Core.Queries.Catalog;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class HomePageNewProductsViewComponent : BaseViewComponent
{
    #region Constructors

    public HomePageNewProductsViewComponent(
        IContextAccessor contextAccessor,
        IMediator mediator,
        CatalogSettings catalogSettings)
    {
        _contextAccessor = contextAccessor;
        _mediator = mediator;
        _catalogSettings = catalogSettings;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync(int? productThumbPictureSize)
    {
        if (!_catalogSettings.NewProductsOnHomePage)
            return Content("");

        var products = (await _mediator.Send(new GetSearchProductsQuery {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            StoreId = _contextAccessor.StoreContext.CurrentStore.Id,
            VisibleIndividuallyOnly = true,
            MarkedAsNewOnly = true,
            OrderBy = ProductSortingEnum.CreatedOn,
            PageSize = _catalogSettings.NewProductsNumberOnHomePage
        })).products;

        if (!products.Any())
            return Content("");
            
        // Prepare dictionary of products with their new attribute combinations
        var nowUtc = DateTime.UtcNow;
        var productAttributeInfoDictionary = new Dictionary<string, List<(string combinationId, string attributeInfo)>>();
        
        foreach (var product in products)
        {
            // Get all new attribute combinations for this product
            var newCombinations = product.ProductAttributeCombinations
                .Where(c => 
                    c.MarkAsNew && 
                    (!c.MarkAsNewStartDateTimeUtc.HasValue || c.MarkAsNewStartDateTimeUtc.Value < nowUtc) && 
                    (!c.MarkAsNewEndDateTimeUtc.HasValue || c.MarkAsNewEndDateTimeUtc.Value > nowUtc))
                .ToList();
                
            // Only process combinations if there are any
            if (newCombinations.Any())
            {
                var combinationInfoList = new List<(string combinationId, string attributeInfo)>();
                
                foreach (var combination in newCombinations)
                {
                    if (combination.Attributes.Any())
                    {
                        // Extract attribute names
                        var attributeNames = new List<string>();
                        
                        foreach (var attr in combination.Attributes)
                        {
                            var attributeMapping = product.ProductAttributeMappings.FirstOrDefault(x => x.Id == attr.Key);
                            if (attributeMapping != null)
                            {
                                var attributeValue = attributeMapping.ProductAttributeValues.FirstOrDefault(x => x.Id == attr.Value);
                                if (attributeValue != null)
                                {
                                    attributeNames.Add(attributeValue.Name);
                                }
                            }
                        }
                        
                        if (attributeNames.Any())
                        {
                            // Store the combination ID and attribute info
                            combinationInfoList.Add((combination.Id, string.Join(", ", attributeNames)));
                        }
                    }
                }
                
                // If we found any valid attributes, add to dictionary
                if (combinationInfoList.Any())
                {
                    productAttributeInfoDictionary[product.Id] = combinationInfoList;
                }
            }
        }
            
        // Pass the attribute info through ViewData
        ViewData["ProductAttributeInfo"] = productAttributeInfoDictionary;

        var model = await _mediator.Send(new GetProductOverview {
            PreparePictureModel = true,
            PreparePriceModel = true,
            PrepareSpecificationAttributes = _catalogSettings.ShowSpecAttributeOnCatalogPages,
            ProductThumbPictureSize = productThumbPictureSize,
            Products = products
        });

        return View(model);
    }

    #endregion

    #region Fields

    private readonly IContextAccessor _contextAccessor;
    private readonly IMediator _mediator;
    private readonly CatalogSettings _catalogSettings;

    #endregion
}