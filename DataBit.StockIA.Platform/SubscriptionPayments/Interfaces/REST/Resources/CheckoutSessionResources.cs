using System.ComponentModel.DataAnnotations;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST.Resources;

/// <summary>BillingInterval: MONTHLY (default) or ANNUAL; ignored for the free plan.</summary>
public record CreateCheckoutSessionResource([Range(1, int.MaxValue)] int PlanId, string? BillingInterval = null);

/// <summary>Stripe page to redirect the user to; null when the change was applied without leaving the app.</summary>
public record CheckoutSessionResource(string? Url);
