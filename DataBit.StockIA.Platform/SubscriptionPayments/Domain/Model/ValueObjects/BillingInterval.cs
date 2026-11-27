namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

/// <summary>How often a paid plan is billed; yearly billing has a discount (see Plan.AnnualDiscount).</summary>
public enum BillingInterval
{
    Monthly,
    Annual
}
