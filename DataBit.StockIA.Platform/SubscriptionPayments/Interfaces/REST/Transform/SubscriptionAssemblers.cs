using Microsoft.AspNetCore.Mvc;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.Shared.Interfaces.REST;
using DataBit.StockIA.Platform.Shared.Interfaces.REST.ProblemDetails;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Errors;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST.Resources;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST.Transform;

public static class PlanResourceFromEntityAssembler
{
    public static PlanResource ToResourceFromEntity(Plan plan) =>
        new(plan.Id, plan.Name, plan.MonthlyPrice, plan.AnnualPrice, plan.Features, ToResource(plan.Entitlements), plan.Highlighted);

    public static PlanEntitlementsResource ToResource(PlanEntitlements entitlements) => new(
        entitlements.MaxUsers, entitlements.MaxInventoryItems, entitlements.MaxRecipes, entitlements.MaxSalesPerMonth,
        entitlements.MaxForecastsPerMonth, entitlements.PurchaseRecommendations, entitlements.MenuRecommendations,
        entitlements.SustainabilityReports, ChannelCodesOf(entitlements.AlertChannels), entitlements.RealtimeCriticalAlerts,
        entitlements.Automation, entitlements.ExternalIntegrations, entitlements.PrioritySupport);

    public static IReadOnlyList<string> ChannelCodesOf(AlertChannels channels) =>
        Enum.GetValues<AlertChannels>().Where(channel => channel != AlertChannels.None && channels.HasFlag(channel))
            .Select(channel => channel.ToCode()).ToList();
}

public static class SubscriptionResourceFromEntityAssembler
{
    public static SubscriptionResource ToResourceFromEntity(RestaurantSubscription subscription) => new(
        subscription.Id, subscription.PlanId, subscription.Status.ToCode(), subscription.RenewalDate.ToString("yyyy-MM-dd"),
        subscription.PaymentMethod.ToCode(), subscription.BillingInterval.ToCode(), subscription.HasCardOnFile, subscription.CancelAtPeriodEnd);
}

public static class SubscriptionActionResultAssembler
{
    public static IActionResult ToActionResult<T>(Result<T> result, ProblemDetailsFactory factory, Func<T, IActionResult> onSuccess) =>
        ResultActionAssembler.ToActionResult(result, factory, error => error switch
        {
            SubscriptionPaymentsError.PlanNotFound => StatusCodes.Status404NotFound,
            SubscriptionPaymentsError.SubscriptionNotFound => StatusCodes.Status404NotFound,
            SubscriptionPaymentsError.AlreadyOnPlan => StatusCodes.Status409Conflict,
            SubscriptionPaymentsError.CardNotLinked => StatusCodes.Status409Conflict,
            SubscriptionPaymentsError.PaymentGatewayNotConfigured => StatusCodes.Status503ServiceUnavailable,
            SubscriptionPaymentsError.PaymentGatewayError => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status500InternalServerError
        }, onSuccess);
}
