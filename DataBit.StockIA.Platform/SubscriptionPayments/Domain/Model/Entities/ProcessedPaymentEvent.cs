namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Entities;

/// <summary>
///     A Stripe webhook event already applied. Stripe delivers events at least once, so an event id seen before is
///     ignored: a repeated webhook never duplicates a payment or a plan change.
/// </summary>
public class ProcessedPaymentEvent
{
    private ProcessedPaymentEvent()
    {
    }

    public ProcessedPaymentEvent(string eventId, string type, DateTimeOffset processedAt)
    {
        EventId = eventId;
        Type = type;
        ProcessedAt = processedAt;
    }

    /// <summary>Stripe event id (evt_...), the primary key.</summary>
    public string EventId { get; private set; } = string.Empty;

    public string Type { get; private set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; private set; }
}
