namespace CrowdCoffee.Application.Cart;

/// MilkOptionId is nullable: drink products (with a "milk" modifier group attached) require it;
/// non-drink products (no modifier groups at all, e.g. pastries) must leave it null. See
/// CartLineRepricer for the group-driven validation, not a hardcoded drink/non-drink list.
public record CartLineRepriceRequest(Guid ProductId, Guid? MilkOptionId, Guid? ShotOptionId, List<Guid> SyrupOptionIds, int Quantity);

public record CartRepriceRequest(List<CartLineRepriceRequest> Lines);

public record RepricedLineDto(string Id, string Name, decimal UnitPrice, int Quantity, decimal LineTotal);

public record CartRepriceResponseDto(List<RepricedLineDto> Lines, decimal Subtotal);
