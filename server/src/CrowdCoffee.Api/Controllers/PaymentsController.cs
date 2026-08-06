using CrowdCoffee.Api.Common;
using CrowdCoffee.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrowdCoffee.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(
    InitiatePaymentHandler initiatePaymentHandler,
    HandlePaymentWebhookHandler webhookHandler,
    GetReconciliationHandler reconciliationHandler) : ControllerBase
{
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate(InitiatePaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await initiatePaymentHandler.HandleAsync(request, cancellationToken);
        return result.ToActionResult(this);
    }

    /// Paymob calls this server-to-server on transaction completion, with the HMAC in the `hmac`
    /// query string parameter (not a body field) — read the raw body ourselves rather than
    /// model-binding, since VerifyAndParseWebhook needs the exact bytes the signature was
    /// computed over, not a re-serialized copy.
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        var hmac = Request.Query["hmac"].FirstOrDefault();

        var result = await webhookHandler.HandleAsync(rawBody, hmac, cancellationToken);
        return result.ToActionResult(this);
    }

    [Authorize(Policy = "ManagerOnly")]
    [HttpGet("reconciliation")]
    public async Task<IActionResult> Reconciliation(CancellationToken cancellationToken)
    {
        var rows = await reconciliationHandler.HandleAsync(cancellationToken);
        return Ok(rows);
    }
}
