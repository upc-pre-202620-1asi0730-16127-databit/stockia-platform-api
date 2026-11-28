namespace DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST.Resources;

/// <summary>
///     RenewalDate as yyyy-MM-dd; status (ACTIVE, PAST_DUE, CANCELED, INCOMPLETE), payment method and billing interval as
///     UPPER_SNAKE codes. INCOMPLETE means the card or payment still has to be confirmed at Stripe.
/// </summary>
public record SubscriptionResource(int Id, int PlanId, string Status, string RenewalDate, string PaymentMethod, string BillingInterval,
    bool HasCardOnFile, bool CancelAtPeriodEnd);
