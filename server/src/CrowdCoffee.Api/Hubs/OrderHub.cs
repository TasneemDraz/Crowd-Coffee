using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CrowdCoffee.Api.Hubs;

/// <summary>
/// Story 12.1: pure server-to-client push — staff clients never call a method on this hub, they
/// just connect and listen for "orderStatusChanged" (BroadcastOrderStatusChangedHandler). Auth is
/// enforced at connection time (T12.1.3): either role, since a barista runs the board day to day.
/// </summary>
[Authorize(Policy = "StaffOnly")]
public class OrderHub : Hub
{
}
