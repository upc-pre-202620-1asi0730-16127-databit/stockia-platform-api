using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST.Resources;

/// <summary>RenewalDate as yyyy-MM-dd; status, payment method and billing interval as UPPER_SNAKE codes.</summary>
public record SubscriptionResource(int Id, int PlanId, string Status, string RenewalDate, string PaymentMethod, string BillingInterval);
