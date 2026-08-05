using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Domain.Common;

namespace CrowdCoffee.Application.Cart;

/// <summary>
/// §4a rules 2/3: the client's prices are display-only — every submitted line is recomputed
/// here from the current DB product price + modifier deltas, and any line whose option ids
/// don't genuinely belong to that product's attached groups rejects the whole request rather
/// than silently dropping/ignoring the bad line.
/// </summary>
public class RepriceCartHandler(ICatalogRepository catalogRepository)
{
    public async Task<Result<CartRepriceResponseDto>> HandleAsync(CartRepriceRequest request, CancellationToken cancellationToken)
    {
        var repriced = new List<RepricedLineDto>();

        foreach (var line in request.Lines)
        {
            var product = await catalogRepository.GetActiveProductByIdAsync(line.ProductId, cancellationToken);
            if (product is null)
            {
                return Result.Failure<CartRepriceResponseDto>(
                    Error.NotFound("Cart.ProductNotFound", $"No active product with id '{line.ProductId}'."));
            }

            var groups = await catalogRepository.GetModifierGroupsForCategoryAsync(product.CategorySlug, cancellationToken);

            var result = CartLineRepricer.Reprice(product, groups, line);
            if (result.IsFailure)
            {
                return Result.Failure<CartRepriceResponseDto>(result.Error!);
            }

            repriced.Add(new RepricedLineDto(result.Value.Id, result.Value.Name, result.Value.UnitPrice, result.Value.Quantity, result.Value.LineTotal));
        }

        var subtotal = repriced.Sum(l => l.LineTotal);
        return Result.Success(new CartRepriceResponseDto(repriced, subtotal));
    }
}
