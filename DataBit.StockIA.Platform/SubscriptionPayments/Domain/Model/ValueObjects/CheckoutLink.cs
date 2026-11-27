namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

/// <summary>Where to send the user at the payment provider; null when the change was applied without leaving the app.</summary>
public record CheckoutLink(string? Url);
