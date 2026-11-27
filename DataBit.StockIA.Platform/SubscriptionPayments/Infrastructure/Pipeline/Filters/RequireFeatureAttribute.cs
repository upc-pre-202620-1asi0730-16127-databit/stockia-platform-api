using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.ACL;
using ProblemDetailsFactory = DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails.ProblemDetailsFactory;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Infrastructure.Pipeline.Filters;

/// <summary>
///     The endpoint is only for plans that include the feature, e.g.
///     [RequireFeature(PlanFeature.SustainabilityReports)]. It runs after the IAM
///     middleware (so the restaurant is known) and answers 402 with the
///     FeatureNotInPlan problem and the plan that unlocks it. Roles are still
///     checked by [RequireRole].
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class RequireFeatureAttribute(PlanFeature feature) : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var services = context.HttpContext.RequestServices;
        var restaurantId = services.GetRequiredService<ICurrentUserAccessor>().CurrentRestaurantId;
        if (restaurantId == null)
        {
            await next();
            return;
        }

        var check = await services.GetRequiredService<ISubscriptionContextFacade>()
            .CheckFeature(restaurantId.Value, feature, context.HttpContext.RequestAborted);
        if (check.Allowed)
        {
            await next();
            return;
        }

        context.Result = services.GetRequiredService<ProblemDetailsFactory>().ToActionResult(StatusCodes.Status402PaymentRequired,
            check.Error!.Value.ToString(), check.Message, check.Details);
    }
}
