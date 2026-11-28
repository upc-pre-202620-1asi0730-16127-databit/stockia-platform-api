using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Application.Internal.OutboundServices;

/// <summary>
///     Port to the payment provider (Stripe). Cards are entered on the provider's hosted pages, so the platform never
///     sees them: it only creates sessions, redirects the user and reacts to the verified webhook events.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Creates the customer of a restaurant at the provider and returns its id.</summary>
    Task<string> CreateCustomerAsync(int restaurantId, CancellationToken cancellationToken);

    /// <summary>Hosted page that saves and validates a card without charging it (free plan). Returns its URL.</summary>
    Task<string> CreateCardSetupSessionAsync(string customerId, int restaurantId, CancellationToken cancellationToken);

    /// <summary>Hosted checkout of a paid plan (it charges the card). Returns its URL.</summary>
    Task<string> CreateSubscriptionSessionAsync(string customerId, int restaurantId, Plan plan, BillingInterval billingInterval,
        CancellationToken cancellationToken);

    /// <summary>Moves an existing paid subscription to another paid plan or interval, prorating. The webhook confirms it.</summary>
    Task ChangeSubscriptionPlanAsync(string subscriptionId, int restaurantId, Plan plan, BillingInterval billingInterval,
        CancellationToken cancellationToken);

    /// <summary>Ends a paid subscription right away.</summary>
    Task CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken);

    /// <summary>Hosted page where the customer changes the card or cancels. Returns its URL.</summary>
    Task<string> CreatePortalSessionAsync(string customerId, CancellationToken cancellationToken);

    /// <summary>
    ///     Verifies the signature of a webhook and translates the event. Throws <see cref="InvalidWebhookSignatureException" />
    ///     when the signature is wrong, so a forged "payment completed" is never applied.
    /// </summary>
    PaymentEvent ReadWebhook(string payload, string signatureHeader);
}

/// <summary>The provider is not configured (missing keys or price ids).</summary>
public class PaymentGatewayNotConfiguredException(string message) : Exception(message);

/// <summary>The provider rejected or failed the request.</summary>
public class PaymentGatewayException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>The webhook did not come from the provider (or is too old).</summary>
public class InvalidWebhookSignatureException(string message, Exception? inner = null) : Exception(message, inner);
