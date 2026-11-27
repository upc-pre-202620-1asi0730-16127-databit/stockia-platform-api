using DataBit.StockIA.Platform.Shared.Application.Errors;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.ACL;

/// <summary>What the plan of a restaurant allows. Null caps mean unlimited.</summary>
public record PlanEntitlementsInfo(
    int PlanId,
    string PlanName,
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
    bool PrioritySupport);

/// <summary>
///     Answer to "may this restaurant do it?". When denied, Error, Message
///     (localized) and Details {feature, limit, current, requiredPlan} go
///     straight into the failed Result of the asking context.
/// </summary>
public record EntitlementCheck(bool Allowed, PlanAccessError? Error = null, string Message = "", object? Details = null)
{
    public static EntitlementCheck Allow { get; } = new(true);
}
