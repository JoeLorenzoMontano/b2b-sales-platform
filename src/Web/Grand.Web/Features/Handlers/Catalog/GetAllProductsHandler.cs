using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Queries.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Web.Features.Models.Catalog;
using Grand.Web.Features.Models.Products;
using Grand.Web.Models.Catalog;
using MediatR;

namespace Grand.Web.Features.Handlers.Catalog
{
    public class GetAllProductsHandler : IRequestHandler<GetAllProducts, CatalogProductsModel>
    {
        private readonly IProductService _productService;
        private readonly IMediator _mediator;

        public GetAllProductsHandler(
            IProductService productService,
            IMediator mediator)
        {
            _productService = productService;
            _mediator = mediator;
        }

        public async Task<CatalogProductsModel> Handle(GetAllProducts request, CancellationToken cancellationToken)
        {
            var model = new CatalogProductsModel();

            var pageSize = 12;
            
            if (request.Command.PageSize > 0)
            {
                pageSize = request.Command.PageSize;
            }

            var orderBy = ProductSortingEnum.Position;
            if (request.Command.OrderBy.HasValue)
            {
                orderBy = (ProductSortingEnum)request.Command.OrderBy.Value;
            }

            //view/sorting/page size
            var options = await _mediator.Send(new GetViewSortSizeOptions {
                Command = request.Command,
                PagingFilteringModel = request.Command,
                Language = request.Language,
                AllowCustomersToSelectPageSize = true,
                PageSizeOptions = "12,24,36,72",
                PageSize = pageSize
            }, cancellationToken);
            model.PagingFilteringContext = options.command;

            //products
            var searchProductsResult = await _productService.SearchProducts(
                storeId: request.Store.Id,
                visibleIndividuallyOnly: true,
                orderBy: orderBy,
                pageIndex: request.Command.PageNumber - 1,
                pageSize: pageSize);

            model.PagingFilteringContext.LoadPagedList(searchProductsResult.products);

            //prepare product list
            var productOverview = await _mediator.Send(new GetProductOverview {
                ProductThumbPictureSize = 0,
                Products = searchProductsResult.products
            });
            model.Products = productOverview.ToList();

            model.PagingFilteringContext.ViewMode = request.Command.ViewMode;

            return model;
        }
    }
}