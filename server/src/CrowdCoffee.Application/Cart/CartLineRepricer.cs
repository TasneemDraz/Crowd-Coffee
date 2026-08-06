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

        // Whether milk/shot/syrups apply at all is driven entirely by which groups are actually
        // attached to this product's category (ProductModifierGroup), not a hardcoded drink-category
        // list — a non-drink product simply has zero groups here, so every selection must be absent.
        var milkGroup = productGroups.FirstOrDefault(g => g.Slug == "milk");
        ModifierOptionCatalogRow? milkOption = null;
        if (milkGroup is not null)
        {
            if (request.MilkOptionId is not { } milkId)
            {
                return Result.Failure<RepricedLine>(
                    Error.Validation("Cart.MilkRequired", $"Product '{product.Slug}' requires a milk selection."));
            }

            milkOption = milkGroup.Options.FirstOrDefault(o => o.Id == milkId);
            if (milkOption is null)
            {
                return Result.Failure<RepricedLine>(
                    Error.Validation("Cart.InvalidMilkOption", $"Milk option '{milkId}' does not belong to product '{product.Slug}'."));
            }
        }
        else if (request.MilkOptionId is not null)
        {
            return Result.Failure<RepricedLine>(
                Error.Validation("Cart.UnexpectedMilkOption", $"Product '{product.Slug}' does not accept a milk selection."));
        }

        decimal shotDelta = 0;
        string? shotName = null;
        var shotGroup = productGroups.FirstOrDefault(g => g.Slug == "shot");
        if (request.ShotOptionId is { } shotId)
        {
            if (shotGroup is null)
            {
                return Result.Failure<RepricedLine>(
                    Error.Validation("Cart.UnexpectedShotOption", $"Product '{product.Slug}' does not accept a shot selection."));
            }

            var shotOption = shotGroup.Options.FirstOrDefault(o => o.Id == shotId);
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
        if (syrupGroup is null && request.SyrupOptionIds.Count > 0)
        {
            return Result.Failure<RepricedLine>(
                Error.Validation("Cart.UnexpectedSyrupOptions", $"Product '{product.Slug}' does not accept syrup selections."));
        }

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

        var unitPrice = product.Price + (milkOption?.PriceDelta ?? 0) + shotDelta + syrupDelta;

        var nameParts = new List<string>();
        if (milkOption is not null)
        {
            nameParts.Add(milkOption.Name);
        }

        if (shotName is not null)
        {
            nameParts.Add($"+ {shotName}");
        }

        nameParts.AddRange(syrupNames.Select(n => $"+ {n}"));
        var composedName = nameParts.Count > 0 ? $"{product.Name} ({string.Join(", ", nameParts)})" : product.Name;

        // Non-drink lines use the plain product id (matching the frontend cart store's
        // useCartStore.add(), which has no modifiers to compose against); drink lines keep the
        // Epic 8.1 composed scheme exactly.
        string id;
        if (milkGroup is not null)
        {
            var shotPart = request.ShotOptionId.HasValue ? "shot" : "noshot";
            var sortedSyrupPart = string.Join("-", request.SyrupOptionIds.Select(sid => sid.ToString()).OrderBy(s => s, StringComparer.Ordinal));
            id = $"{product.Id}::{request.MilkOptionId}::{shotPart}::{sortedSyrupPart}";
        }
        else
        {
            id = product.Id.ToString();
        }

        return Result.Success(new RepricedLine(id, composedName, unitPrice, request.Quantity, unitPrice * request.Quantity));
    }
}

public record RepricedLine(string Id, string Name, decimal UnitPrice, int Quantity, decimal LineTotal);
