using Stripe;
using Stripe.Checkout;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Infrastructure.Payments;

/// <summary>
///     Translates verified Stripe events into <see cref="PaymentEvent" />, the language of this context. Anything the
///     platform does not act on becomes <see cref="PaymentEventType.Ignored" />.
/// </summary>
public static class StripeEventTranslator
{
    public const string RestaurantKey = "restaurantId";
    public const string PlanKey = "planId";
    public const string IntervalKey = "billingInterval";

    public static PaymentEvent Translate(Event stripeEvent) => stripeEvent.Type switch
    {
        "checkout.session.completed" when stripeEvent.Data.Object is Session session => FromSession(stripeEvent.Id, session),
        "customer.subscription.created" or "customer.subscription.updated" when stripeEvent.Data.Object is Subscription subscription =>
            FromSubscription(stripeEvent.Id, PaymentEventType.SubscriptionUpdated, subscription),
        "customer.subscription.deleted" when stripeEvent.Data.Object is Subscription subscription =>
            FromSubscription(stripeEvent.Id, PaymentEventType.SubscriptionEnded, subscription),
        "invoice.paid" when stripeEvent.Data.Object is Invoice invoice => FromInvoice(stripeEvent.Id, PaymentEventType.InvoicePaid, invoice),
        "invoice.payment_failed" when stripeEvent.Data.Object is Invoice invoice =>
            FromInvoice(stripeEvent.Id, PaymentEventType.InvoicePaymentFailed, invoice),
        _ => new PaymentEvent(stripeEvent.Id, PaymentEventType.Ignored)
    };

    private static PaymentEvent FromSession(string eventId, Session session)
    {
        var metadata = session.Metadata;
        var type = session.Mode == "setup" ? PaymentEventType.CardSaved
            : session.Mode == "subscription" ? PaymentEventType.SubscriptionStarted
            : PaymentEventType.Ignored;
        return new PaymentEvent(eventId, type, IntOf(metadata, RestaurantKey), IntOf(metadata, PlanKey), IntervalOf(metadata),
            session.CustomerId, session.SubscriptionId);
    }

    private static PaymentEvent FromSubscription(string eventId, PaymentEventType type, Subscription subscription)
    {
        // "incomplete" is a checkout still being paid: nothing to apply until Stripe reports the outcome.
        if (subscription.Status == "incomplete") return new PaymentEvent(eventId, PaymentEventType.Ignored);
        var state = subscription.Status switch
        {
            "active" or "trialing" => ProviderSubscriptionState.Active,
            "past_due" or "unpaid" or "paused" => ProviderSubscriptionState.PastDue,
            _ => ProviderSubscriptionState.Canceled
        };
        var periodEnd = subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd;
        return new PaymentEvent(eventId, type, IntOf(subscription.Metadata, RestaurantKey), IntOf(subscription.Metadata, PlanKey),
            IntervalOf(subscription.Metadata), subscription.CustomerId, subscription.Id, SubscriptionState: state,
            CancelAtPeriodEnd: subscription.CancelAtPeriodEnd, PeriodEnd: periodEnd == null ? null : DateOnly.FromDateTime(periodEnd.Value));
    }

    private static PaymentEvent FromInvoice(string eventId, PaymentEventType type, Invoice invoice)
    {
        // Stripe amounts are in the smallest currency unit (cents of a sol).
        var cents = type == PaymentEventType.InvoicePaid ? invoice.AmountPaid : invoice.AmountDue;
        return new PaymentEvent(eventId, type, CustomerId: invoice.CustomerId,
            SubscriptionId: invoice.Parent?.SubscriptionDetails?.SubscriptionId, InvoiceId: invoice.Id, Amount: cents / 100m,
            Currency: invoice.Currency);
    }

    private static int? IntOf(IDictionary<string, string>? metadata, string key) =>
        metadata != null && metadata.TryGetValue(key, out var value) && int.TryParse(value, out var number) ? number : null;

    private static BillingInterval? IntervalOf(IDictionary<string, string>? metadata) =>
        metadata != null && metadata.TryGetValue(IntervalKey, out var value) && EnumCode.TryParse<BillingInterval>(value, out var interval)
            ? interval
            : null;
}
