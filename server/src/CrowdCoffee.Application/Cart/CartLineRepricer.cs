using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Domain.Common;

namespace CrowdCoffee.Application.Cart;

/// <summary>
/// Pure recompute-and-validate logic for a single cart line (§4a rules 2/3/6) — no I/O, so the
/// handler owns fetching the product/groups and this just decides whether the selections are
/// legit and what they cost. Mirrors the frontend's <c>composeDrinkLineId</c>
/// (frontend/src/lib/cartLineId.ts) exactly, including the syrup-id sort, so a server-recomputed
/// line can be matched back against the client's own composed id.
/// </summary>
public static class CartLineRepricer
{
    public static Result<RepricedLine> Reprice(
        ProductCatalogRow product,
        IReadOnlyList<ModifierGroupCatalogRow> productGroups,
        CartLineRepriceRequest request)
    {
        if (request.Quantity <= 0)
        {
            return Result.Failure<RepricedLine>(Error.Validation("Cart.InvalidQuantity", "Quantity must be positive."));
        }

        var milkGroup = productGroups.FirstOrDefault(g => g.Slug == "milk");
        var milkOption = milkGroup?.Options.FirstOrDefault(o => o.Id == request.MilkOptionId);
        if (milkOption is null)
        {
            return Result.Failure<RepricedLine>(
                Error.Validation("Cart.InvalidMilkOption", $"Milk option '{request.MilkOptionId}' does not belong to product '{product.Slug}'."));
        }

        decimal shotDelta = 0;
        string? shotName = null;
        if (request.ShotOptionId is { } shotId)
        {
            var shotGroup = productGroups.FirstOrDefault(g => g.Slug == "shot");
            var shotOption = shotGroup?.Options.FirstOrDefault(o => o.Id == shotId);
            if (shotOption is null)
            {
                return Result.Failure<RepricedLine>(
                    Error.Validation("Cart.InvalidShotOption", $"Shot option '{shotId}' does not belong to product '{product.Slug}'."));
            }

            shotDelta = shotOption.PriceDelta;
            shotName = shotOption.Name;
        }

        if (request.SyrupOptionIds.Count > 2)
        {
            return Result.Failure<RepricedLine>(Error.Validation("Cart.TooManySyrups", "At most 2 syrups are allowed."));
        }

        if (request.SyrupOptionIds.Distinct().Count() != request.SyrupOptionIds.Count)
        {
            return Result.Failure<RepricedLine>(Error.Validation("Cart.DuplicateSyrups", "Duplicate syrup selections are not allowed."));
        }

        var syrupGroup = productGroups.FirstOrDefault(g => g.Slug == "syrups");
        decimal syrupDelta = 0;
        var syrupNames = new List<string>();
        foreach (var syrupId in request.SyrupOptionIds)
        {
            var syrupOption = syrupGroup?.Options.FirstOrDefault(o => o.Id == syrupId);
            if (syrupOption is null)
            {
                return Result.Failure<RepricedLine>(
                    Error.Validation("Cart.InvalidSyrupOption", $"Syrup option '{syrupId}' does not belong to product '{product.Slug}'."));
            }

            syrupDelta += syrupOption.PriceDelta;
            syrupNames.Add(syrupOption.Name);
        }

        var unitPrice = product.Price + milkOption.PriceDelta + shotDelta + syrupDelta;

        var nameParts = new List<string> { milkOption.Name };
        if (shotName is not null)
        {
            nameParts.Add($"+ {shotName}");
        }

        nameParts.AddRange(syrupNames.Select(n => $"+ {n}"));
        var composedName = $"{product.Name} ({string.Join(", ", nameParts)})";

        var shotPart = request.ShotOptionId.HasValue ? "shot" : "noshot";
        var sortedSyrupPart = string.Join("-", request.SyrupOptionIds.Select(id => id.ToString()).OrderBy(s => s, StringComparer.Ordinal));
        var id = $"{product.Id}::{request.MilkOptionId}::{shotPart}::{sortedSyrupPart}";

        return Result.Success(new RepricedLine(id, composedName, unitPrice, request.Quantity, unitPrice * request.Quantity));
    }
}

public record RepricedLine(string Id, string Name, decimal UnitPrice, int Quantity, decimal LineTotal);
