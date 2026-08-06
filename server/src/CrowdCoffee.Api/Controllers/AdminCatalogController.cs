using CrowdCoffee.Api.Common;
using CrowdCoffee.Application.Catalog;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdCoffee.Api.Controllers;

[Authorize(Policy = "ManagerOnly")]
[ApiController]
[Route("api/admin/catalog")]
public class AdminCatalogController(
    AdminListCategoriesHandler listCategoriesHandler,
    CreateCategoryHandler createCategoryHandler,
    IValidator<CreateCategoryRequest> createCategoryValidator,
    UpdateCategoryHandler updateCategoryHandler,
    IValidator<UpdateCategoryRequest> updateCategoryValidator,
    AdminListProductsHandler listProductsHandler,
    CreateProductHandler createProductHandler,
    IValidator<CreateProductRequest> createProductValidator,
    UpdateProductHandler updateProductHandler,
    IValidator<UpdateProductRequest> updateProductValidator,
    AdminListModifierGroupsHandler listModifierGroupsHandler,
    UpdateModifierOptionHandler updateModifierOptionHandler,
    IValidator<UpdateModifierOptionRequest> updateModifierOptionValidator,
    UploadProductImageHandler uploadImageHandler) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken) =>
        Ok(await listCategoriesHandler.HandleAsync(cancellationToken));

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var validation = await createCategoryValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var failure in validation.Errors)
            {
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var result = await createCategoryHandler.HandleAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("categories/{categoryId:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var validation = await updateCategoryValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var failure in validation.Errors)
            {
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var result = await updateCategoryHandler.HandleAsync(categoryId, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("products")]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken) =>
        Ok(await listProductsHandler.HandleAsync(cancellationToken));

    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var validation = await createProductValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var failure in validation.Errors)
            {
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var result = await createProductHandler.HandleAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("products/{productId:guid}")]
    public async Task<IActionResult> UpdateProduct(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var validation = await updateProductValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var failure in validation.Errors)
            {
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var result = await updateProductHandler.HandleAsync(productId, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpGet("modifier-groups")]
    public async Task<IActionResult> GetModifierGroups(CancellationToken cancellationToken) =>
        Ok(await listModifierGroupsHandler.HandleAsync(cancellationToken));

    [HttpPut("modifier-options/{optionId:guid}")]
    public async Task<IActionResult> UpdateModifierOption(Guid optionId, UpdateModifierOptionRequest request, CancellationToken cancellationToken)
    {
        var validation = await updateModifierOptionValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (var failure in validation.Errors)
            {
                ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var result = await updateModifierOptionHandler.HandleAsync(optionId, request, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("images")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var result = await uploadImageHandler.HandleAsync(stream, file.FileName, file.ContentType, file.Length, cancellationToken);
        if (result.IsFailure)
        {
            return result.ToActionResult(this);
        }

        return Ok(new { url = result.Value });
    }
}
