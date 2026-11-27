using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Commands;

public record CreateCheckoutSessionCommand(int RestaurantId, int PlanId, BillingInterval BillingInterval = BillingInterval.Monthly);
