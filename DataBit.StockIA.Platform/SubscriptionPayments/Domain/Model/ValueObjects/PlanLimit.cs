namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

/// <summary>A quantity a plan caps (a missing cap means unlimited).</summary>
public enum PlanLimit
{
    Users,
    InventoryItems,
    Recipes,
    SalesPerMonth,
    ForecastsPerMonth
}
