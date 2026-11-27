using DataBit.StockIA.Platform.Shared.Domain.Model.Entities;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;

/// <summary>
///     An invoice of a paid plan, paid or failed, as reported by Stripe. The invoice id is unique, so the same
///     invoice is never recorded twice.
/// </summary>
public class PaymentTransaction : IAuditableEntity
{
    private PaymentTransaction()
    {
    }

    public PaymentTransaction(int restaurantId, int? planId, string invoiceId, decimal amount, string currency, PaymentStatus status,
        DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(invoiceId)) throw new ArgumentException("The invoice id is required.", nameof(invoiceId));
        RestaurantId = restaurantId;
        PlanId = planId;
        InvoiceId = invoiceId;
        Amount = amount;
        Currency = currency.ToUpperInvariant();
        Status = status;
        OccurredAt = occurredAt;
    }

    public int Id { get; private set; }
    public int RestaurantId { get; private set; }
    public int? PlanId { get; private set; }

    /// <summary>Stripe invoice id (in_...).</summary>
    public string InvoiceId { get; private set; } = string.Empty;

    /// <summary>Amount in the currency units (soles), not in cents.</summary>
    public decimal Amount { get; private set; }

    public string Currency { get; private set; } = "PEN";
    public PaymentStatus Status { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
