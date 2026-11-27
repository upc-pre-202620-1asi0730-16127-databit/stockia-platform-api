namespace DataBit.StockIA.Platform.Shared.Application.Errors;

/// <summary>
///     Why the plan of a restaurant denied an action. Every bounded context
///     that enforces the plan returns these codes (they are the problem titles)
///     and maps them to HTTP 402.
/// </summary>
public enum PlanAccessError
{
    /// <summary>A quantity cap of the plan was reached (users, supplies, recipes, sales, forecasts).</summary>
    PlanLimitReached,

    /// <summary>The plan does not include the capability.</summary>
    FeatureNotInPlan
}
