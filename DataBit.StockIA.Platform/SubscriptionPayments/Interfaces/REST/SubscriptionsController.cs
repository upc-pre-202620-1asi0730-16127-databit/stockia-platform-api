using DataBit.StockIA.Platform.SubscriptionPayments.Infrastructure.Pipeline.Filters;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Iam.Infrastructure.Pipeline.Middleware.Attributes;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Services;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Commands;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Queries;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST.Resources;
using DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST.Transform;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST;

/// <summary>Subscription type and Upgrade plan: Administrator only.</summary>
[Authorize]
[AllowIncompleteSubscription]
[RequireRole("ADMIN")]
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
[Tags("Subscription of the restaurant (Administrator)")]
public class SubscriptionsController(
    ISubscriptionCommandService subscriptionCommandService,
    ISubscriptionQueryService subscriptionQueryService,
    ICurrentUserAccessor currentUser,
    ProblemDetailsFactory problemDetailsFactory) : ControllerBase
{
    [HttpGet("current")]
    [EndpointSummary("Get the current subscription")]
    [EndpointName("GetCurrentSubscription")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var subscription = await subscriptionQueryService.Handle(
            new GetSubscriptionByRestaurantIdQuery(currentUser.CurrentRestaurantId!.Value), cancellationToken);
        return subscription == null ? NotFound() : Ok(SubscriptionResourceFromEntityAssembler.ToResourceFromEntity(subscription));
    }

    /// <summary>
    ///     Starts a plan at Stripe. Returns the Stripe URL to redirect the user to (card setup for the free plan, payment for a
    ///     paid plan). A null URL means the change was applied in place. The plan never changes from this call alone: a paid
    ///     plan starts when the Stripe webhook confirms the payment.
    /// </summary>
    [HttpPost("checkout-sessions")]
    [EndpointSummary("Start the card setup or the payment of a plan at Stripe")]
    [EndpointName("CreateCheckoutSession")]
    [ProducesResponseType(typeof(CheckoutSessionResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionResource resource, CancellationToken cancellationToken)
    {
        var billingInterval = BillingInterval.Monthly;
        if (resource.BillingInterval != null && !EnumCode.TryParse(resource.BillingInterval, out billingInterval))
            return BadRequest(problemDetailsFactory.CreateProblemDetails(StatusCodes.Status400BadRequest, "InvalidBillingInterval",
                "The billing interval must be MONTHLY or ANNUAL."));

        var result = await subscriptionCommandService.Handle(
            new CreateCheckoutSessionCommand(currentUser.CurrentRestaurantId!.Value, resource.PlanId, billingInterval), cancellationToken);
        return SubscriptionActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            link => Ok(new CheckoutSessionResource(link.Url)));
    }

    /// <summary>Stripe Customer Portal: change the card or cancel the paid plan.</summary>
    [HttpPost("portal-session")]
    [EndpointSummary("Open the Stripe billing portal")]
    [EndpointName("CreatePortalSession")]
    [ProducesResponseType(typeof(CheckoutSessionResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreatePortalSession(CancellationToken cancellationToken)
    {
        var result = await subscriptionCommandService.Handle(new CreatePortalSessionCommand(currentUser.CurrentRestaurantId!.Value),
            cancellationToken);
        return SubscriptionActionResultAssembler.ToActionResult(result, problemDetailsFactory,
            link => Ok(new CheckoutSessionResource(link.Url)));
    }
}
