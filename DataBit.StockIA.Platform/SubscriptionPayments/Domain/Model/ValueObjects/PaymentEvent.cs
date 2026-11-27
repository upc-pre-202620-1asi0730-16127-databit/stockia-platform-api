namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

/// <summary>What happened at the payment provider, in the language of this context (not Stripe's).</summary>
public enum PaymentEventType
{
    /// <summary>An event the platform does not act on.</summary>
    Ignored,

    /// <summary>The customer saved a card (Checkout in setup mode): validation, no charge.</summary>
    CardSaved,

    /// <summary>A paid plan was bought (Checkout in subscription mode completed).</summary>
    SubscriptionStarted,

    /// <summary>The paid plan changed: new plan, renewed period, cancellation requested, or payment problems.</summary>
    SubscriptionUpdated,

    /// <summary>The paid plan ended.</summary>
    SubscriptionEnded,

    InvoicePaid,
    InvoicePaymentFailed
}

/// <summary>State of a paid plan at the provider.</summary>
public enum ProviderSubscriptionState
{
    Active,
    PastDue,
    Canceled
}

/// <summary>
///     A verified webhook event, already translated from the provider. Only the fields that apply to its type are set.
///     RestaurantId, PlanId and BillingInterval come from the metadata the platform attached when it created the
///     Checkout session or the subscription.
/// </summary>
public record PaymentEvent(
    string Id,
    PaymentEventType Type,
    int? RestaurantId = null,
    int? PlanId = null,
    BillingInterval? BillingInterval = null,
    string? CustomerId = null,
    string? SubscriptionId = null,
    string? InvoiceId = null,
    decimal? Amount = null,
    string? Currency = null,
    ProviderSubscriptionState? SubscriptionState = null,
    bool CancelAtPeriodEnd = false,
    DateOnly? PeriodEnd = null);
