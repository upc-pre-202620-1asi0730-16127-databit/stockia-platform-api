using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Commands;

/// <summary>A webhook as received: the raw body and the Stripe-Signature header, still to be verified.</summary>
public record ReceivePaymentWebhookCommand(string Payload, string Signature);

/// <summary>An already verified and translated provider event to apply.</summary>
public record ProcessPaymentEventCommand(PaymentEvent Event);
