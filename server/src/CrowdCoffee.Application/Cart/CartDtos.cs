namespace CrowdCoffee.Application.Cart;

public record CartLineRepriceRequest(Guid ProductId, Guid MilkOptionId, Guid? ShotOptionId, List<Guid> SyrupOptionIds, int Quantity);

public record CartRepriceRequest(List<CartLineRepriceRequest> Lines);

public record RepricedLineDto(string Id, string Name, decimal UnitPrice, int Quantity, decimal LineTotal);

public record CartRepriceResponseDto(List<RepricedLineDto> Lines, decimal Subtotal);
