using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.ACL;
using ProblemDetailsFactory = DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails.ProblemDetailsFactory;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Infrastructure.Pipeline.Filters;

/// <summary>
///     Global filter: a restaurant whose subscription is still Incomplete (the card or payment is not confirmed at Stripe
///     yet) cannot use the platform. It answers 402 SubscriptionRequired with details.requiredAction, so the Web
///     Application can send the user to the payment step. Controllers that must stay reachable (sign-in, profile,
///     the subscription itself) opt out with [AllowIncompleteSubscription]. Anonymous endpoints have no restaurant and pass.
/// </summary>
public sealed class RequireActiveSubscriptionFilter(
    ICurrentUserAccessor currentUser,
    ISubscriptionContextFacade subscriptionContextFacade,
    ProblemDetailsFactory problemDetailsFactory) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (currentUser.CurrentRestaurantId is not { } restaurantId ||
            context.ActionDescriptor.EndpointMetadata.OfType<AllowIncompleteSubscriptionAttribute>().Any())
        {
            await next();
            return;
        }

        var check = await subscriptionContextFacade.CheckAccess(restaurantId, context.HttpContext.RequestAborted);
        if (check.Allowed)
        {
            await next();
            return;
        }

        context.Result = problemDetailsFactory.ToActionResult(StatusCodes.Status402PaymentRequired, check.Error!.Value.ToString(), check.Message,
            check.Details);
    }
}

/// <summary>The controller stays reachable while the subscription is Incomplete (identity, profile, payment endpoints).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowIncompleteSubscriptionAttribute : Attribute
{
}
