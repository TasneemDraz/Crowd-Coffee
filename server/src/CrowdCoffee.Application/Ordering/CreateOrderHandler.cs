using CrowdCoffee.Application.Cart;
using CrowdCoffee.Application.Catalog;
using CrowdCoffee.Application.Common;
using CrowdCoffee.Application.Inventory;
using CrowdCoffee.Domain.Common;
using CrowdCoffee.Domain.Ordering;
using Microsoft.Extensions.Logging;

namespace CrowdCoffee.Application.Ordering;

/// <summary>
/// §12 Reliability: "Order creation is a single transaction: validate → reserve stock → persist
/// order. Partial failure leaves no orphan reservation." §7/§8 add the entry-point split this
/// reconciles against: cash orders start at Pending and card/wallet orders start at
/// PendingPayment, and §8 explicitly forbids reserving stock for a PendingPayment order
/// ("otherwise abandoned card checkouts eat your inventory") — reservation only ever happens at
/// the Confirmed transition. The resolution: cash orders have nothing left to wait on once
/// placed, so this handler creates them at Pending and immediately transitions them to Confirmed
/// in the same transaction (full OrderStatusHistory trail, per §7's "no exceptions"), which is
/// what actually triggers StockReservationService — satisfying both the entry-point rule and the
/// "reserve at Confirmed" rule. Card/wallet orders stop at PendingPayment with no reservation;
/// Epic 10's webhook is what later transitions them to Confirmed and reserves stock.
/// </summary>
public class CreateOrderHandler(
    ICatalogRepository catalogRepository,
    IOrderRepository orderRepository,
    IOrderNumberGenerator orderNumberGenerator,
    IUnitOfWork unitOfWork,
    StockReservationService stockReservationService,
    ILogger<CreateOrderHandler> logger)
{
    // Matches the frontend cart store's DELIVERY_FEE exactly (frontend/src/stores/cart.ts) —
    // flat everywhere / free on pickup (§3). §14.1 (flat vs. distance-capped) resolved the same
    // way that store already committed to: nothing in the design or seed data encodes zones.
    private const decimal DeliveryFee = 30m;

    public async Task<Result<OrderDto>> HandleAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var existing = await orderRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return Result.Success(OrderDtoMapper.ToDto(existing));
        }

        var repriceResult = await RepriceLinesAsync(request.Lines, cancellationToken);
        if (repriceResult.IsFailure)
        {
            return Result.Failure<OrderDto>(repriceResult.Error!);
        }

        var lines = repriceResult.Value;
        var subtotal = lines.Sum(l => l.LineTotal);
        var deliveryFee = request.Mode == OrderMode.Delivery ? DeliveryFee : 0m;
        var now = DateTime.UtcNow;

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        string orderNumber;
        try
        {
            orderNumber = await orderNumberGenerator.NextAsync(now, cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Failed to generate order number.");
            throw;
        }

        var order = request.PaymentMethod == PaymentMethod.Cash
            ? Order.CreateCash(
                Guid.NewGuid(), orderNumber, request.CustomerName, request.Phone, request.Mode,
                request.Address, request.Latitude, request.Longitude, subtotal, deliveryFee,
                request.IdempotencyKey, now, request.Notes)
            : Order.CreatePendingPayment(
                Guid.NewGuid(), orderNumber, request.CustomerName, request.Phone, request.Mode,
                request.Address, request.Latitude, request.Longitude, subtotal, deliveryFee,
                request.PaymentMethod, request.IdempotencyKey, now, request.Notes);

        foreach (var line in lines)
        {
            var item = new OrderItem(Guid.NewGuid(), order.Id, line.ProductId, line.ProductName, line.UnitPrice, line.Quantity);
            foreach (var modifier in line.Modifiers)
            {
                item.AddModifier(new OrderItemModifier(Guid.NewGuid(), item.Id, modifier.ModifierOptionId, modifier.Name, modifier.PriceDelta));
            }

            order.AddItem(item);
        }

        orderRepository.Add(order);

        try
        {
            await orderRepository.SaveChangesAsync(cancellationToken);
        }
        catch (OrderIdempotencyConflictException)
        {
            // Concurrent double-submit racing on the same IdempotencyKey (Story 9.3) — the other
            // request won, so return its order instead of erroring or creating a duplicate.
            await transaction.RollbackAsync(cancellationToken);
            var raced = await orderRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
            return raced is not null
                ? Result.Success(OrderDtoMapper.ToDto(raced))
                : Result.Failure<OrderDto>(Error.Conflict("Order.IdempotencyRace", "A concurrent request with the same idempotency key failed."));
        }

        if (request.PaymentMethod == PaymentMethod.Cash)
        {
            var confirmResult = order.TransitionTo(OrderStatus.Confirmed, "system", DateTime.UtcNow, "Cash order auto-confirmed at checkout.");
            if (confirmResult.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<OrderDto>(confirmResult.Error!);
            }

            await orderRepository.SaveChangesAsync(cancellationToken);

            var reserveResult = await stockReservationService.ReserveForOrderAsync(order.Id, cancellationToken);
            if (reserveResult.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result.Failure<OrderDto>(reserveResult.Error!);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return Result.Success(OrderDtoMapper.ToDto(order));
    }

    private async Task<Result<List<RepricedOrderLine>>> RepriceLinesAsync(List<CreateOrderLineRequest> requestLines, CancellationToken cancellationToken)
    {
        var repriced = new List<RepricedOrderLine>();

        foreach (var line in requestLines)
        {
            var product = await catalogRepository.GetActiveProductByIdAsync(line.ProductId, cancellationToken);
            if (product is null)
            {
                return Result.Failure<List<RepricedOrderLine>>(
                    Error.NotFound("Order.ProductNotFound", $"No active product with id '{line.ProductId}'."));
            }

            var groups = await catalogRepository.GetModifierGroupsForCategoryAsync(product.CategorySlug, cancellationToken);
            var repriceRequest = new CartLineRepriceRequest(line.ProductId, line.MilkOptionId, line.ShotOptionId, line.SyrupOptionIds, line.Quantity);
            var result = CartLineRepricer.Reprice(product, groups, repriceRequest);
            if (result.IsFailure)
            {
                return Result.Failure<List<RepricedOrderLine>>(result.Error!);
            }

            var modifiers = CollectModifiers(groups, line);
            repriced.Add(new RepricedOrderLine(line.ProductId, product.Name, result.Value.UnitPrice, line.Quantity, result.Value.LineTotal, modifiers));
        }

        return Result.Success(repriced);
    }

    /// CartLineRepricer already validated every id genuinely belongs to this product's groups —
    /// re-walk the same groups here purely to snapshot each selected option's name/delta onto
    /// OrderItemModifier (§4a rule 1), which CartLineRepricer's RepricedLine doesn't expose
    /// per-selection (only the composed display name and total price).
    private static List<RepricedModifier> CollectModifiers(IReadOnlyList<ModifierGroupCatalogRow> groups, CreateOrderLineRequest line)
    {
        var modifiers = new List<RepricedModifier>();

        if (line.MilkOptionId is { } milkId)
        {
            var option = groups.FirstOrDefault(g => g.Slug == "milk")?.Options.First(o => o.Id == milkId);
            if (option is not null)
            {
                modifiers.Add(new RepricedModifier(option.Id, option.Name, option.PriceDelta));
            }
        }

        if (line.ShotOptionId is { } shotId)
        {
            var option = groups.FirstOrDefault(g => g.Slug == "shot")?.Options.First(o => o.Id == shotId);
            if (option is not null)
            {
                modifiers.Add(new RepricedModifier(option.Id, option.Name, option.PriceDelta));
            }
        }

        var syrupGroup = groups.FirstOrDefault(g => g.Slug == "syrups");
        foreach (var syrupId in line.SyrupOptionIds)
        {
            var option = syrupGroup?.Options.First(o => o.Id == syrupId);
            if (option is not null)
            {
                modifiers.Add(new RepricedModifier(option.Id, option.Name, option.PriceDelta));
            }
        }

        return modifiers;
    }

    private record RepricedModifier(Guid ModifierOptionId, string Name, decimal PriceDelta);

    private record RepricedOrderLine(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal, List<RepricedModifier> Modifiers);
}
