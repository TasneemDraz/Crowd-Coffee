using CrowdCoffee.Api.Common;
using CrowdCoffee.Application.Catalog;
using Microsoft.AspNetCore.Mvc;

namespace CrowdCoffee.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public class CatalogController(
    GetCategoriesHandler categoriesHandler,
    GetProductsHandler productsHandler,
    GetProductModifiersHandler productModifiersHandler) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await categoriesHandler.HandleAsync(cancellationToken);
        return Ok(categories);
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        var products = await productsHandler.HandleAsync(cancellationToken);
        return Ok(products);
    }

    [HttpGet("products/{slug}/modifiers")]
    public async Task<IActionResult> GetProductModifiers(string slug, CancellationToken cancellationToken)
    {
        var result = await productModifiersHandler.HandleAsync(slug, cancellationToken);
        return result.ToActionResult(this);
    }
}
