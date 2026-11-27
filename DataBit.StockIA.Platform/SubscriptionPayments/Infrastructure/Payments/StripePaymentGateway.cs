using Stripe;
using Stripe.BillingPortal;
using Stripe.Checkout;
using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;
using SessionCreateOptions = Stripe.Checkout.SessionCreateOptions;
using Plan = DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates.Plan;
using SessionService = Stripe.Checkout.SessionService;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Infrastructure.Payments;

/// <summary>
///     Stripe adapter: hosted Checkout (card setup for the free plan, subscription for paid plans), Customer Portal and
///     webhook verification. The platform stamps restaurantId / planId / interval as metadata on everything it creates,
///     so webhook events can be tied back to the restaurant without trusting the browser.
/// </summary>
public class StripePaymentGateway(StripeOptions options, ILogger<StripePaymentGateway> logger) : IPaymentGateway
{
    private const string CardOnly = "card";

    public async Task<string> CreateCustomerAsync(int restaurantId, CancellationToken cancellationToken) =>
        await Call(async client =>
        {
            var customer = await new CustomerService(client).CreateAsync(new CustomerCreateOptions
            {
                Description = $"StockIA restaurant {restaurantId}",
                Metadata = new Dictionary<string, string> { [StripeEventTranslator.RestaurantKey] = restaurantId.ToString() }
            }, cancellationToken: cancellationToken);
            return customer.Id;
        });

    public async Task<string> CreateCardSetupSessionAsync(string customerId, int restaurantId, CancellationToken cancellationToken) =>
        await Call(async client =>
        {
            var session = await new SessionService(client).CreateAsync(new SessionCreateOptions
            {
                Mode = "setup",
                Customer = customerId,
                Currency = "pen",
                AllowedPaymentMethodTypes = [CardOnly],
                SuccessUrl = options.SuccessUrl,
                CancelUrl = options.CancelUrl,
                Metadata = new Dictionary<string, string> { [StripeEventTranslator.RestaurantKey] = restaurantId.ToString() }
            }, cancellationToken: cancellationToken);
            return session.Url;
        });

    public async Task<string> CreateSubscriptionSessionAsync(string customerId, int restaurantId, Plan plan, BillingInterval billingInterval,
        CancellationToken cancellationToken)
    {
        var priceId = RequirePriceId(plan, billingInterval);
        var metadata = MetadataOf(restaurantId, plan, billingInterval);
        return await Call(async client =>
        {
            var session = await new SessionService(client).CreateAsync(new SessionCreateOptions
            {
                Mode = "subscription",
                Customer = customerId,
                AllowedPaymentMethodTypes = [CardOnly],
                LineItems = [new SessionLineItemOptions { Price = priceId, Quantity = 1 }],
                SuccessUrl = options.SuccessUrl,
                CancelUrl = options.CancelUrl,
                Metadata = metadata,
                SubscriptionData = new SessionSubscriptionDataOptions { Metadata = metadata }
            }, cancellationToken: cancellationToken);
            return session.Url;
        });
    }

    public async Task ChangeSubscriptionPlanAsync(string subscriptionId, int restaurantId, Plan plan, BillingInterval billingInterval,
        CancellationToken cancellationToken)
    {
        var priceId = RequirePriceId(plan, billingInterval);
        await Call(async client =>
        {
            var service = new SubscriptionService(client);
            var current = await service.GetAsync(subscriptionId, cancellationToken: cancellationToken);
            var item = current.Items.Data.FirstOrDefault() ?? throw new PaymentGatewayException("The subscription has no items.");
            await service.UpdateAsync(subscriptionId, new SubscriptionUpdateOptions
            {
                Items = [new SubscriptionItemOptions { Id = item.Id, Price = priceId }],
                ProrationBehavior = "create_prorations",
                CancelAtPeriodEnd = false,
                Metadata = MetadataOf(restaurantId, plan, billingInterval)
            }, cancellationToken: cancellationToken);
            return string.Empty;
        });
    }

    public async Task CancelSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken) =>
        await Call(async client =>
        {
            await new SubscriptionService(client).CancelAsync(subscriptionId, cancellationToken: cancellationToken);
            return string.Empty;
        });

    public async Task<string> CreatePortalSessionAsync(string customerId, CancellationToken cancellationToken) =>
        await Call(async client =>
        {
            var session = await new Stripe.BillingPortal.SessionService(client).CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = $"{options.WebAppBaseUrl}/settings/plans"
            }, cancellationToken: cancellationToken);
            return session.Url;
        });

    public PaymentEvent ReadWebhook(string payload, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(options.WebhookSecret))
            throw new PaymentGatewayNotConfiguredException($"{StripeOptions.WebhookSecretVariable} is not set.");
        Event stripeEvent;
        try
        {
            // The API version of the endpoint may be newer than the SDK's: the signature is what must be trusted.
            stripeEvent = EventUtility.ConstructEvent(payload, signatureHeader, options.WebhookSecret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException exception)
        {
            throw new InvalidWebhookSignatureException("The Stripe signature is not valid.", exception);
        }

        return StripeEventTranslator.Translate(stripeEvent);
    }

    private static Dictionary<string, string> MetadataOf(int restaurantId, Plan plan, BillingInterval interval) => new()
    {
        [StripeEventTranslator.RestaurantKey] = restaurantId.ToString(),
        [StripeEventTranslator.PlanKey] = plan.Id.ToString(),
        [StripeEventTranslator.IntervalKey] = interval.ToCode()
    };

    private string RequirePriceId(Plan plan, BillingInterval interval) =>
        options.PriceIdOf(plan, interval) ??
        throw new PaymentGatewayNotConfiguredException($"{StripeOptions.PriceVariableOf(plan, interval)} is not set.");

    private async Task<T> Call<T>(Func<StripeClient, Task<T>> action)
    {
        if (string.IsNullOrWhiteSpace(options.SecretKey))
            throw new PaymentGatewayNotConfiguredException($"{StripeOptions.SecretKeyVariable} is not set.");
        try
        {
            return await action(new StripeClient(options.SecretKey));
        }
        catch (StripeException exception)
        {
            logger.LogError(exception, "Stripe rejected the request: {Code} {Message}", exception.StripeError?.Code, exception.Message);
            throw new PaymentGatewayException("Stripe could not complete the request.", exception);
        }
    }
}
