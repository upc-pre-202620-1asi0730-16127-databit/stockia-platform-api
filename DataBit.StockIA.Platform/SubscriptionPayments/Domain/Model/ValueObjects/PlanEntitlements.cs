namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

/// <summary>
///     What a plan allows. A null cap means unlimited. Realtime alerts,
///     automation and external integrations are sold with the plan but not built
///     yet; their flags only record the promise.
/// </summary>
public sealed record PlanEntitlements(
    int? MaxUsers,
    int? MaxInventoryItems,
    int? MaxRecipes,
    int? MaxSalesPerMonth,
    int? MaxForecastsPerMonth,
    bool PurchaseRecommendations,
    bool MenuRecommendations,
    bool SustainabilityReports,
    AlertChannels AlertChannels,
    bool RealtimeCriticalAlerts,
    bool Automation,
    bool ExternalIntegrations,
    bool PrioritySupport)
{
    /// <summary>Free demo plan: the full cycle (supply → recipe → sale → alert) without AI.</summary>
    public static PlanEntitlements Essential { get; } = new(1, 50, 10, 100, 0, false, false, false, AlertChannels.App, false, false, false, false);

    public static PlanEntitlements Starter { get; } = new(5, null, 50, null, 30, true, false, true,
        AlertChannels.App | AlertChannels.Email, false, false, false, false);

    public static PlanEntitlements Professional { get; } = new(null, null, null, null, null, true, true, true,
        AlertChannels.App | AlertChannels.Email | AlertChannels.Whatsapp, true, true, true, true);

    public int? LimitOf(PlanLimit limit) => limit switch
    {
        PlanLimit.Users => MaxUsers,
        PlanLimit.InventoryItems => MaxInventoryItems,
        PlanLimit.Recipes => MaxRecipes,
        PlanLimit.SalesPerMonth => MaxSalesPerMonth,
        PlanLimit.ForecastsPerMonth => MaxForecastsPerMonth,
        _ => throw new ArgumentOutOfRangeException(nameof(limit), limit, null)
    };

    public bool Includes(PlanFeature feature) => feature switch
    {
        PlanFeature.PurchaseRecommendations => PurchaseRecommendations,
        PlanFeature.MenuRecommendations => MenuRecommendations,
        PlanFeature.SustainabilityReports => SustainabilityReports,
        PlanFeature.RealtimeCriticalAlerts => RealtimeCriticalAlerts,
        PlanFeature.Automation => Automation,
        PlanFeature.ExternalIntegrations => ExternalIntegrations,
        PlanFeature.PrioritySupport => PrioritySupport,
        _ => throw new ArgumentOutOfRangeException(nameof(feature), feature, null)
    };

    /// <summary>One more unit is allowed while the current usage is below the cap.</summary>
    public bool AllowsOneMore(PlanLimit limit, int currentUsage) => LimitOf(limit) is not { } cap || currentUsage < cap;
}
