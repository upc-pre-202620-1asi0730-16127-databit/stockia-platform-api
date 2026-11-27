namespace DataBit.StockIA.Platform.SubscriptionPayments.Application.Errors;

public enum SubscriptionPaymentsError
{
    PlanNotFound,
    SubscriptionNotFound,
    AlreadyOnPlan,
    CardNotLinked,
    PaymentGatewayNotConfigured,
    PaymentGatewayError,
    InvalidWebhookSignature
}
