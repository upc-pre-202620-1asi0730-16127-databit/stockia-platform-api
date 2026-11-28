using Microsoft.Extensions.Localization;
using DataBit.StockIA.Platform.Shared.Application;
using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Errors;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Internal.OutboundServices;
using DataBit.StockIA.Platform.SubscriptionPayments.Application.Services;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Commands;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Repositories;
using DataBit.StockIA.Platform.SubscriptionPayments.Resources;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Application.Internal.CommandServices;

/// <summary>
///     Starts the card setup or the payment of a plan at Stripe. Nothing here activates a paid plan: the plan only changes
///     when the verified Stripe webhook arrives (<see cref="PaymentEventCommandService" />).
/// </summary>
public class SubscriptionCommandService(
    IPlanRepository planRepository,
    ISubscriptionRepository subscriptionRepository,
    IPaymentGateway paymentGateway,
    IRestaurantClock clock,
    IUnitOfWork unitOfWork,
    ILogger<SubscriptionCommandService> logger,
    IStringLocalizer<SubscriptionPaymentsMessages> localizer) : ISubscriptionCommandService
{
    /// <summary>
    ///     Every new restaurant starts on the free plan, Incomplete until it links a card with Stripe (called by the
    ///     sign-up of Restaurant Registration).
    /// </summary>
    public async Task<Result<RestaurantSubscription>> Handle(CreateDefaultSubscriptionCommand command, CancellationToken cancellationToken)
    {
        var existing = await subscriptionRepository.FindByRestaurantIdAsync(command.RestaurantId, cancellationToken);
        if (existing != null) return Result<RestaurantSubscription>.Success(existing);

        var plan = await planRepository.FindFreePlanAsync(cancellationToken);
        if (plan == null) return Failure<RestaurantSubscription>(SubscriptionPaymentsError.PlanNotFound);

        var subscription = RestaurantSubscription.StartFree(command.RestaurantId, plan.Id, clock.Today);
        await subscriptionRepository.AddAsync(subscription, cancellationToken);
        await unitOfWork.CompleteAsync(cancellationToken);
        return Result<RestaurantSubscription>.Success(subscription);
    }

    /// <summary>
    ///     Free plan: saves the card without charging it (Stripe validates it), or, with a card already saved, goes back to
    ///     the free plan right away. Paid plan: opens Stripe Checkout, or, with a paid subscription already running, changes
    ///     it in place (the webhook confirms). The result has a URL to redirect the user to, or none when nothing is left to do.
    /// </summary>
    public async Task<Result<CheckoutLink>> Handle(CreateCheckoutSessionCommand command, CancellationToken cancellationToken)
    {
        var plan = await planRepository.FindByIdAsync(command.PlanId, cancellationToken);
        if (plan == null) return Failure<CheckoutLink>(SubscriptionPaymentsError.PlanNotFound);
        var subscription = await subscriptionRepository.FindByRestaurantIdAsync(command.RestaurantId, cancellationToken);
        if (subscription == null) return Failure<CheckoutLink>(SubscriptionPaymentsError.SubscriptionNotFound);

        var interval = plan.IsFree ? BillingInterval.Monthly : command.BillingInterval;
        if (subscription.PlanId == plan.Id && subscription.Status != SubscriptionStatus.Incomplete && subscription.BillingInterval == interval)
            return Failure<CheckoutLink>(SubscriptionPaymentsError.AlreadyOnPlan);

        try
        {
            return plan.IsFree
                ? await ChooseFreePlan(subscription, plan, cancellationToken)
                : await ChoosePaidPlan(subscription, plan, interval, cancellationToken);
        }
        catch (PaymentGatewayNotConfiguredException exception)
        {
            logger.LogError(exception, "Payments are not configured");
            return Failure<CheckoutLink>(SubscriptionPaymentsError.PaymentGatewayNotConfigured);
        }
        catch (PaymentGatewayException)
        {
            return Failure<CheckoutLink>(SubscriptionPaymentsError.PaymentGatewayError);
        }
    }

    public async Task<Result<CheckoutLink>> Handle(CreatePortalSessionCommand command, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.FindByRestaurantIdAsync(command.RestaurantId, cancellationToken);
        if (subscription == null) return Failure<CheckoutLink>(SubscriptionPaymentsError.SubscriptionNotFound);
        if (subscription.StripeCustomerId == null) return Failure<CheckoutLink>(SubscriptionPaymentsError.CardNotLinked);

        try
        {
            return Result<CheckoutLink>.Success(new CheckoutLink(await paymentGateway.CreatePortalSessionAsync(subscription.StripeCustomerId,
                cancellationToken)));
        }
        catch (PaymentGatewayNotConfiguredException exception)
        {
            logger.LogError(exception, "Payments are not configured");
            return Failure<CheckoutLink>(SubscriptionPaymentsError.PaymentGatewayNotConfigured);
        }
        catch (PaymentGatewayException)
        {
            return Failure<CheckoutLink>(SubscriptionPaymentsError.PaymentGatewayError);
        }
    }

    private async Task<Result<CheckoutLink>> ChooseFreePlan(RestaurantSubscription subscription, Plan plan, CancellationToken cancellationToken)
    {
        if (subscription.HasCardOnFile)
        {
            // Back to the free plan: end the paid subscription first, then apply the lower plan from the next request.
            if (subscription.StripeSubscriptionId != null)
                await paymentGateway.CancelSubscriptionAsync(subscription.StripeSubscriptionId, cancellationToken);
            subscription.DowngradeToFree(plan.Id, clock.Today);
            subscriptionRepository.Update(subscription);
            await unitOfWork.CompleteAsync(cancellationToken);
            return Result<CheckoutLink>.Success(new CheckoutLink(null));
        }

        var customerId = await EnsureCustomerAsync(subscription, cancellationToken);
        return Result<CheckoutLink>.Success(new CheckoutLink(
            await paymentGateway.CreateCardSetupSessionAsync(customerId, subscription.RestaurantId, cancellationToken)));
    }

    private async Task<Result<CheckoutLink>> ChoosePaidPlan(RestaurantSubscription subscription, Plan plan, BillingInterval interval,
        CancellationToken cancellationToken)
    {
        if (subscription.StripeSubscriptionId != null && subscription.Status is SubscriptionStatus.Active or SubscriptionStatus.PastDue)
        {
            await paymentGateway.ChangeSubscriptionPlanAsync(subscription.StripeSubscriptionId, subscription.RestaurantId, plan, interval,
                cancellationToken);
            return Result<CheckoutLink>.Success(new CheckoutLink(null));
        }

        var customerId = await EnsureCustomerAsync(subscription, cancellationToken);
        return Result<CheckoutLink>.Success(new CheckoutLink(
            await paymentGateway.CreateSubscriptionSessionAsync(customerId, subscription.RestaurantId, plan, interval, cancellationToken)));
    }

    /// <summary>The Stripe customer is saved at once, so a retry never creates a second one.</summary>
    private async Task<string> EnsureCustomerAsync(RestaurantSubscription subscription, CancellationToken cancellationToken)
    {
        if (subscription.StripeCustomerId != null) return subscription.StripeCustomerId;
        var customerId = await paymentGateway.CreateCustomerAsync(subscription.RestaurantId, cancellationToken);
        subscription.UseStripeCustomer(customerId);
        subscriptionRepository.Update(subscription);
        await unitOfWork.CompleteAsync(cancellationToken);
        return customerId;
    }

    private Result<T> Failure<T>(SubscriptionPaymentsError error) => Result<T>.Failure(error, localizer[error.ToString()]);
}
