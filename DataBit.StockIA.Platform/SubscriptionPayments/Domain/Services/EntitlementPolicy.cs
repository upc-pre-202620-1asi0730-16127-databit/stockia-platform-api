using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Services;

/// <summary>Pure rules to suggest which plan unlocks a denied limit or feature.</summary>
public static class EntitlementPolicy
{
    /// <summary>Cheapest plan that allows one more unit than the current usage, or null when none does.</summary>
    public static Plan? CheapestPlanAllowingOneMore(IEnumerable<Plan> plans, PlanLimit limit, int currentUsage) =>
        plans.OrderBy(plan => plan.MonthlyPrice).FirstOrDefault(plan => plan.Entitlements.AllowsOneMore(limit, currentUsage));

    public static Plan? CheapestPlanIncluding(IEnumerable<Plan> plans, PlanFeature feature) =>
        plans.OrderBy(plan => plan.MonthlyPrice).FirstOrDefault(plan => plan.Entitlements.Includes(feature));
}
