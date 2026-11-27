using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;

/// <summary>
///     A plan of the catalog (Essential, Starter, Professional). The
///     entitlements are what the platform enforces; the features are the
///     marketing texts shown in the plan catalog.
/// </summary>
public class Plan(string name, decimal monthlyPrice, List<string> features, PlanEntitlements entitlements, bool highlighted)
{
    /// <summary>Discount of yearly billing over twelve monthly payments.</summary>
    public const decimal AnnualDiscount = 0.30m;

    public Plan() : this(string.Empty, 0, [], PlanEntitlements.Essential, false)
    {
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = name;

    /// <summary>Price in soles (PEN).</summary>
    public decimal MonthlyPrice { get; private set; } = monthlyPrice;

    public List<string> Features { get; private set; } = features;
    public PlanEntitlements Entitlements { get; private set; } = entitlements;
    public bool Highlighted { get; private set; } = highlighted;

    public bool IsFree => MonthlyPrice == 0;

    /// <summary>Price of twelve months paid at once, with the annual discount.</summary>
    public decimal AnnualPrice => Math.Round(MonthlyPrice * 12 * (1 - AnnualDiscount), 2, MidpointRounding.AwayFromZero);

    public decimal PriceFor(BillingInterval interval) => interval == BillingInterval.Annual ? AnnualPrice : MonthlyPrice;
}
