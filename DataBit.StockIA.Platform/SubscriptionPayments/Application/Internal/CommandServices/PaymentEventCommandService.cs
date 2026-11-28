using Microsoft.Extensions.Localization;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Errors;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Services;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Commands;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Entities;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Repositories;
using DataBit.StockIA.Platform.SubscriptionPayments.Resources;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Application.Internal.CommandServices;

/// <summary>
///     The only place where a paid plan starts, changes or ends: the verified webhooks of the payment provider. Each event
///     is applied once (its id is remembered), and events that do not match the subscription of the restaurant are ignored,
///     so a repeated, late or out-of-order delivery never corrupts the plan.
/// </summary>
public class PaymentEventCommandService(
    IPaymentGateway paymentGateway,
    IPlanRepository planRepository,
    ISubscriptionRepository subscriptionRepository,
    IPaymentTransactionRepository transactionRepository,
    IProcessedPaymentEventRepository processedEventRepository,
    IRestaurantClock clock,
    IUnitOfWork unitOfWork,
    ILogger<PaymentEventCommandService> logger,
    IStringLocalizer<SubscriptionPaymentsMessages> localizer) : IPaymentEventCommandService
{
    public async Task<Result> Handle(ReceivePaymentWebhookCommand command, CancellationToken cancellationToken)
    {
        PaymentEvent paymentEvent;
        try
        {
            paymentEvent = paymentGateway.ReadWebhook(command.Payload, command.Signature);
        }
        catch (InvalidWebhookSignatureException exception)
        {
            logger.LogWarning(exception, "A webhook with an invalid signature was rejected");
            return Result.Failure(SubscriptionPaymentsError.InvalidWebhookSignature, localizer[nameof(SubscriptionPaymentsError.InvalidWebhookSignature)]);
        }
        catch (PaymentGatewayNotConfiguredException exception)
        {
            logger.LogError(exception, "Payments are not configured");
            return Result.Failure(SubscriptionPaymentsError.PaymentGatewayNotConfigured,
                localizer[nameof(SubscriptionPaymentsError.PaymentGatewayNotConfigured)]);
        }

        return await Handle(new ProcessPaymentEventCommand(paymentEvent), cancellationToken);
    }

    public async Task<Result> Handle(ProcessPaymentEventCommand command, CancellationToken cancellationToken)
    {
        var paymentEvent = command.Event;
        if (paymentEvent.Type == PaymentEventType.Ignored) return Result.Success();
        if (await processedEventRepository.ExistsAsync(paymentEvent.Id, cancellationToken)) return Result.Success();

        switch (paymentEvent.Type)
        {
            case PaymentEventType.CardSaved:
                await OnCardSaved(paymentEvent, cancellationToken);
                break;
            case PaymentEventType.SubscriptionStarted:
                await OnSubscriptionStarted(paymentEvent, cancellationToken);
                break;
            case PaymentEventType.SubscriptionUpdated:
                await OnSubscriptionUpdated(paymentEvent, cancellationToken);
                break;
            case PaymentEventType.SubscriptionEnded:
                await OnSubscriptionEnded(paymentEvent, cancellationToken);
                break;
            case PaymentEventType.InvoicePaid:
            case PaymentEventType.InvoicePaymentFailed:
                await OnInvoice(paymentEvent, cancellationToken);
                break;
        }

        await processedEventRepository.AddAsync(new ProcessedPaymentEvent(paymentEvent.Id, paymentEvent.Type.ToString(), clock.Now.ToUniversalTime()),
            cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result.Success();
    }

    /// <summary>The card was saved and validated (no charge). On the free plan this is what confirms the subscription.</summary>
    private async Task OnCardSaved(PaymentEvent paymentEvent, CancellationToken cancellationToken)
    {
        var subscription = await FindByRestaurantOrCustomer(paymentEvent, cancellationToken);
        var customerId = paymentEvent.CustomerId ?? subscription?.StripeCustomerId;
        if (subscription == null || customerId == null)
        {
            logger.LogWarning("Card saved event {EventId} does not match any subscription", paymentEvent.Id);
            return;
        }

        subscription.LinkCard(customerId);
        var plan = await planRepository.FindByIdAsync(subscription.PlanId, cancellationToken);
        if (plan is { IsFree: true } && subscription.Status == SubscriptionStatus.Incomplete)
            subscription.Activate(plan.Id, PaymentMethod.Stripe, BillingInterval.Monthly, clock.Today);
        subscriptionRepository.Update(subscription);
    }

    /// <summary>The checkout of a paid plan was completed: the plan starts now.</summary>
    private async Task OnSubscriptionStarted(PaymentEvent paymentEvent, CancellationToken cancellationToken)
    {
        var subscription = await FindByRestaurantOrCustomer(paymentEvent, cancellationToken);
        var plan = paymentEvent.PlanId is { } planId ? await planRepository.FindByIdAsync(planId, cancellationToken) : null;
        if (subscription == null || plan == null || plan.IsFree || paymentEvent.SubscriptionId == null)
        {
            logger.LogWarning("Subscription started event {EventId} cannot be applied", paymentEvent.Id);
            return;
        }

        // A subscription already tied to another Stripe subscription is not replaced by a stale event.
        if (subscription.StripeSubscriptionId != null && subscription.StripeSubscriptionId != paymentEvent.SubscriptionId) return;

        var interval = paymentEvent.BillingInterval ?? BillingInterval.Monthly;
        var renewal = interval == BillingInterval.Annual
            ? clock.Today.AddMonths(RestaurantSubscription.AnnualBillingPeriodMonths)
            : clock.Today.AddDays(RestaurantSubscription.BillingPeriodDays);
        if (paymentEvent.CustomerId != null) subscription.UseStripeCustomer(paymentEvent.CustomerId);
        subscription.StartPaidPlan(plan.Id, interval, paymentEvent.SubscriptionId, renewal);
        subscriptionRepository.Update(subscription);
    }

    /// <summary>Plan change, renewed period, cancellation requested or payment trouble of a known paid subscription.</summary>
    private async Task OnSubscriptionUpdated(PaymentEvent paymentEvent, CancellationToken cancellationToken)
    {
        var subscription = paymentEvent.SubscriptionId == null
            ? null
            : await subscriptionRepository.FindByStripeSubscriptionIdAsync(paymentEvent.SubscriptionId, cancellationToken);
        if (subscription == null) return; // Not started yet (its checkout event will start it) or already ended.

        if (paymentEvent.SubscriptionState == ProviderSubscriptionState.Canceled)
        {
            await DowngradeToFree(subscription, cancellationToken);
            return;
        }

        var plan = paymentEvent.PlanId is { } planId ? await planRepository.FindByIdAsync(planId, cancellationToken) : null;
        var planId2 = plan is { IsFree: false } ? plan.Id : subscription.PlanId;
        subscription.StartPaidPlan(planId2, paymentEvent.BillingInterval ?? subscription.BillingInterval, paymentEvent.SubscriptionId!,
                paymentEvent.PeriodEnd ?? subscription.RenewalDate)
            .UpdatePeriod(paymentEvent.PeriodEnd ?? subscription.RenewalDate, paymentEvent.CancelAtPeriodEnd);
        if (paymentEvent.SubscriptionState == ProviderSubscriptionState.PastDue) subscription.MarkPastDue();
        subscriptionRepository.Update(subscription);
    }

    private async Task OnSubscriptionEnded(PaymentEvent paymentEvent, CancellationToken cancellationToken)
    {
        var subscription = paymentEvent.SubscriptionId == null
            ? null
            : await subscriptionRepository.FindByStripeSubscriptionIdAsync(paymentEvent.SubscriptionId, cancellationToken);
        if (subscription == null) return; // Already back on the free plan (for example the restaurant chose it).
        await DowngradeToFree(subscription, cancellationToken);
    }

    /// <summary>An invoice of a paid plan: recorded once, and it settles or flags the subscription.</summary>
    private async Task OnInvoice(PaymentEvent paymentEvent, CancellationToken cancellationToken)
    {
        var subscription = paymentEvent.SubscriptionId == null
            ? null
            : await subscriptionRepository.FindByStripeSubscriptionIdAsync(paymentEvent.SubscriptionId, cancellationToken);
        if (subscription == null && paymentEvent.CustomerId != null)
            subscription = await subscriptionRepository.FindByStripeCustomerIdAsync(paymentEvent.CustomerId, cancellationToken);
        if (subscription == null || paymentEvent.InvoiceId == null)
        {
            logger.LogWarning("Invoice event {EventId} does not match any subscription", paymentEvent.Id);
            return;
        }

        var paid = paymentEvent.Type == PaymentEventType.InvoicePaid;
        if (!await transactionRepository.ExistsByInvoiceIdAsync(paymentEvent.InvoiceId, cancellationToken))
            await transactionRepository.AddAsync(new PaymentTransaction(subscription.RestaurantId, subscription.PlanId, paymentEvent.InvoiceId,
                paymentEvent.Amount ?? 0m, paymentEvent.Currency ?? "pen", paid ? PaymentStatus.Paid : PaymentStatus.Failed,
                clock.Now.ToUniversalTime()), cancellationToken);

        if (paid) subscription.MarkPaid();
        else subscription.MarkPastDue();
        subscriptionRepository.Update(subscription);
    }

    private async Task DowngradeToFree(RestaurantSubscription subscription, CancellationToken cancellationToken)
    {
        var freePlan = await planRepository.FindFreePlanAsync(cancellationToken);
        if (freePlan == null)
        {
            logger.LogError("There is no free plan to downgrade restaurant {RestaurantId} to", subscription.RestaurantId);
            return;
        }

        subscription.DowngradeToFree(freePlan.Id, clock.Today);
        subscriptionRepository.Update(subscription);
    }

    /// <summary>The restaurant stamped on the Checkout session wins; the Stripe customer is the fallback.</summary>
    private async Task<RestaurantSubscription?> FindByRestaurantOrCustomer(PaymentEvent paymentEvent, CancellationToken cancellationToken)
    {
        var subscription = paymentEvent.RestaurantId is { } restaurantId
            ? await subscriptionRepository.FindByRestaurantIdAsync(restaurantId, cancellationToken)
            : null;
        if (subscription == null && paymentEvent.CustomerId != null)
            subscription = await subscriptionRepository.FindByStripeCustomerIdAsync(paymentEvent.CustomerId, cancellationToken);
        return subscription;
    }
}
