using Microsoft.Extensions.Localization;
using DataBit.StockIA.Platform.Shared.Application.Errors;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Services;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Commands;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Repositories;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Services;
using DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.ACL;
using DataBit.StockIA.Platform.SubscriptionPayments.Resources;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Application.ACL;

public class SubscriptionContextFacade(
    ISubscriptionCommandService subscriptionCommandService,
    ISubscriptionRepository subscriptionRepository,
    IPlanRepository planRepository,
    IStringLocalizer<SubscriptionPaymentsMessages> localizer) : ISubscriptionContextFacade
{
    private const string FallbackPlanName = "Essential";

    public async Task<int> CreateDefaultSubscription(int restaurantId, CancellationToken cancellationToken)
    {
        var result = await subscriptionCommandService.Handle(new CreateDefaultSubscriptionCommand(restaurantId), cancellationToken);
        if (result.IsFailure) throw new InvalidOperationException(result.Message);
        return result.Value!.Id;
    }

    public async Task<PlanEntitlementsInfo> GetEntitlements(int restaurantId, CancellationToken cancellationToken)
    {
        var plan = await PlanOfAsync(restaurantId, cancellationToken);
        var entitlements = plan?.Entitlements ?? PlanEntitlements.Essential;
        return new PlanEntitlementsInfo(plan?.Id ?? 0, plan?.Name ?? FallbackPlanName, entitlements.MaxUsers, entitlements.MaxInventoryItems,
            entitlements.MaxRecipes, entitlements.MaxSalesPerMonth, entitlements.MaxForecastsPerMonth, entitlements.PurchaseRecommendations,
            entitlements.MenuRecommendations, entitlements.SustainabilityReports, entitlements.AlertChannels, entitlements.RealtimeCriticalAlerts,
            entitlements.Automation, entitlements.ExternalIntegrations, entitlements.PrioritySupport);
    }

    public async Task<EntitlementCheck> CheckLimit(int restaurantId, PlanLimit limit, int currentUsage, CancellationToken cancellationToken)
    {
        var plan = await PlanOfAsync(restaurantId, cancellationToken);
        var entitlements = plan?.Entitlements ?? PlanEntitlements.Essential;
        if (entitlements.AllowsOneMore(limit, currentUsage)) return EntitlementCheck.Allow;

        var required = EntitlementPolicy.CheapestPlanAllowingOneMore(await planRepository.ListAsync(cancellationToken), limit, currentUsage);
        return new EntitlementCheck(false, PlanAccessError.PlanLimitReached,
            localizer[nameof(PlanAccessError.PlanLimitReached), plan?.Name ?? FallbackPlanName, entitlements.LimitOf(limit)!.Value, limit.ToString()],
            new { feature = limit.ToString(), limit = entitlements.LimitOf(limit), current = currentUsage, requiredPlan = required?.Name });
    }

    public async Task<EntitlementCheck> CheckAccess(int restaurantId, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.FindByRestaurantIdAsync(restaurantId, cancellationToken);
        if (subscription == null || subscription.GrantsAccess) return EntitlementCheck.Allow;

        return new EntitlementCheck(false, PlanAccessError.SubscriptionRequired, localizer[nameof(PlanAccessError.SubscriptionRequired)],
            new { requiredAction = "LINK_CARD_OR_PAY", planId = subscription.PlanId, hasCardOnFile = subscription.HasCardOnFile });
    }

    public async Task<EntitlementCheck> CheckFeature(int restaurantId, PlanFeature feature, CancellationToken cancellationToken)
    {
        var plan = await PlanOfAsync(restaurantId, cancellationToken);
        var entitlements = plan?.Entitlements ?? PlanEntitlements.Essential;
        if (entitlements.Includes(feature)) return EntitlementCheck.Allow;

        var required = EntitlementPolicy.CheapestPlanIncluding(await planRepository.ListAsync(cancellationToken), feature);
        return new EntitlementCheck(false, PlanAccessError.FeatureNotInPlan,
            localizer[nameof(PlanAccessError.FeatureNotInPlan), plan?.Name ?? FallbackPlanName, feature.ToString()],
            new { feature = feature.ToString(), limit = (int?)null, current = (int?)null, requiredPlan = required?.Name });
    }

    /// <summary>The plan of the subscription, or the free plan when the restaurant has none.</summary>
    private async Task<Plan?> PlanOfAsync(int restaurantId, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.FindByRestaurantIdAsync(restaurantId, cancellationToken);
        var plan = subscription == null ? null : await planRepository.FindByIdAsync(subscription.PlanId, cancellationToken);
        return plan ?? await planRepository.FindFreePlanAsync(cancellationToken);
    }
}
