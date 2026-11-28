using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Services;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Commands;
using DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST.Transform;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST;

/// <summary>
///     Where Stripe reports what happened (card saved, plan bought, invoice paid or failed). It has no bearer token: the
///     request is trusted only if its Stripe-Signature header verifies against the webhook secret.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/v1/webhooks/stripe")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Stripe webhooks (called by Stripe only)")]
public class StripeWebhooksController(IPaymentEventCommandService paymentEventCommandService, ProblemDetailsFactory problemDetailsFactory)
    : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Receive a Stripe event")]
    [EndpointName("ReceiveStripeWebhook")]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        // The signature covers the exact bytes Stripe sent, so the body is read raw, never through model binding.
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();

        var result = await paymentEventCommandService.Handle(new ReceivePaymentWebhookCommand(payload, signature), cancellationToken);
        return SubscriptionActionResultAssembler.ToActionResult(result, problemDetailsFactory, () => Ok());
    }
}
