namespace CrowdCoffee.Domain.Ordering;

public class OrderStatusHistory
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public OrderStatus FromStatus { get; private set; }
    public OrderStatus ToStatus { get; private set; }
    public string ChangedBy { get; private set; } = string.Empty;
    public DateTime ChangedAt { get; private set; }
    public string? Reason { get; private set; }

    private OrderStatusHistory()
    {
    }

    public OrderStatusHistory(Guid id, Guid orderId, OrderStatus fromStatus, OrderStatus toStatus, string changedBy, DateTime changedAt, string? reason)
    {
        Id = id;
        OrderId = orderId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedBy = changedBy;
        ChangedAt = changedAt;
        Reason = reason;
    }
}
