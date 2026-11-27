using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.ACL;

/// <summary>What other bounded contexts may ask Subscription &amp; Billing.</summary>
public interface ISubscriptionContextFacade
{
    /// <summary>Gives a new restaurant the free plan (IAM sign-up). Throws when there is no free plan.</summary>
    Task<int> CreateDefaultSubscription(int restaurantId, CancellationToken cancellationToken);

    /// <summary>
    ///     Entitlements of the plan of the restaurant, read on every call (the plan can change
    ///     without a new sign-in). A restaurant without subscription gets the free plan.
    /// </summary>
    Task<PlanEntitlementsInfo> GetEntitlements(int restaurantId, CancellationToken cancellationToken);

    /// <summary>May the restaurant add one more unit of the limit, given its current usage?</summary>
    Task<EntitlementCheck> CheckLimit(int restaurantId, PlanLimit limit, int currentUsage, CancellationToken cancellationToken);

    /// <summary>Does the plan of the restaurant include the feature?</summary>
    Task<EntitlementCheck> CheckFeature(int restaurantId, PlanFeature feature, CancellationToken cancellationToken);
}
