namespace CrowdCoffee.Domain.Ordering;

/// <summary>
/// Full graph implemented by the state machine in Epic 3 (§7). Declared here because the
/// Order entity's schema needs the type; transition rules are Epic 3's responsibility.
/// </summary>
public enum OrderStatus
{
    Pending,
    PendingPayment,
    Confirmed,
    Preparing,
    Ready,
    OutForDelivery,
    PickedUp,
    Delivered,
    Cancelled
}

public enum OrderMode
{
    Delivery,
    Pickup
}

public enum PaymentMethod
{
    Cash,
    Card,
    Wallet
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Refunded
}
