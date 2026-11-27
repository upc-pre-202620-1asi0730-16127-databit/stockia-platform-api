namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

public enum SubscriptionStatus
{
    Active,
    PastDue,
    Canceled,

    /// <summary>Waiting for the card (free plan) or the payment (paid plan) to be confirmed by Stripe; no access yet.</summary>
    Incomplete
}
