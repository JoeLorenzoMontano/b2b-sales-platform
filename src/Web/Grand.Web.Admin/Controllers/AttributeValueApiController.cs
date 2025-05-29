using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[Route("admin-api/attribute-value")]
[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
public class AttributeValueApiController : BaseController
{
    private readonly IProductService _productService;
    private readonly IProductAttributeService _productAttributeService;

    public AttributeValueApiController(
        IProductService productService,
        IProductAttributeService productAttributeService)
    {
        _productService = productService;
        _productAttributeService = productAttributeService;
    }

    [HttpPost("update-price")]
    public async Task<IActionResult> UpdateOverriddenPrice([FromBody] UpdatePriceRequest request)
    {
        // Add debug logging
        Console.WriteLine($"API Call: ProductId={request?.ProductId}, AttributeMappingId={request?.AttributeMappingId}, AttributeValueId={request?.AttributeValueId}, OverriddenPrice={request?.OverriddenPrice}");
        
        if (request == null)
        {
            return BadRequest("Request body is empty");
        }
        
        if (string.IsNullOrEmpty(request.ProductId) || 
            string.IsNullOrEmpty(request.AttributeMappingId) || 
            string.IsNullOrEmpty(request.AttributeValueId))
        {
            return BadRequest("Required parameters missing");
        }

        try
        {
            // Get the product
            var product = await _productService.GetProductById(request.ProductId);
            if (product == null)
            {
                return NotFound("Product not found");
            }

            // Find the attribute mapping
            var attributeMapping = product.ProductAttributeMappings
                .FirstOrDefault(x => x.Id == request.AttributeMappingId);
            if (attributeMapping == null)
            {
                return NotFound("Attribute mapping not found");
            }

            // Find the attribute value
            var attributeValue = attributeMapping.ProductAttributeValues
                .FirstOrDefault(x => x.Id == request.AttributeValueId);
            if (attributeValue == null)
            {
                return NotFound("Attribute value not found");
            }

            // Set the overridden price
            attributeValue.OverriddenPrice = request.OverriddenPrice;

            // Save the changes
            await _productAttributeService.UpdateProductAttributeValue(
                attributeValue, request.ProductId, request.AttributeMappingId);

            // Get the updated value to confirm
            var updatedProduct = await _productService.GetProductById(request.ProductId);
            var updatedMapping = updatedProduct.ProductAttributeMappings
                .FirstOrDefault(x => x.Id == request.AttributeMappingId);
            var updatedValue = updatedMapping?.ProductAttributeValues
                .FirstOrDefault(x => x.Id == request.AttributeValueId);

            return Ok(new
            {
                success = true,
                oldPrice = request.OverriddenPrice,
                newPrice = updatedValue?.OverriddenPrice,
                message = "Price updated successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error updating price: {ex.Message}");
        }
    }

    public class UpdatePriceRequest
    {
        public string ProductId { get; set; }
        public string AttributeMappingId { get; set; }
        public string AttributeValueId { get; set; }
        public double? OverriddenPrice { get; set; }
    }
}