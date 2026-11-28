using DataBit.StockIA.Platform.Shared.Domain.Model.Entities;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;

/// <summary>
///     The plan a restaurant is subscribed to ("Subscription" in the Web
///     Application). Named RestaurantSubscription so it does not clash with the
///     bounded context namespace.
///     A new restaurant starts on the free plan as <see cref="SubscriptionStatus.Incomplete" /> until it links a card with
///     Stripe (no charge). A paid plan only starts when Stripe confirms the payment, never from a client request.
/// </summary>
public class RestaurantSubscription(int restaurantId, int planId) : IAuditableEntity
{
    /// <summary>Days covered by one monthly payment.</summary>
    public const int BillingPeriodDays = 30;

    /// <summary>Months covered by one annual payment.</summary>
    public const int AnnualBillingPeriodMonths = 12;

    public RestaurantSubscription() : this(0, 0)
    {
    }

    public int Id { get; private set; }
    public int RestaurantId { get; private set; } = restaurantId;
    public int PlanId { get; private set; } = planId;
    public SubscriptionStatus Status { get; private set; } = SubscriptionStatus.Active;
    public DateOnly RenewalDate { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; } = PaymentMethod.None;
    public BillingInterval BillingInterval { get; private set; } = BillingInterval.Monthly;

    /// <summary>Stripe Customer of the restaurant (created the first time it needs Stripe).</summary>
    public string? StripeCustomerId { get; private set; }

    /// <summary>Stripe Subscription of a paid plan; null on the free plan.</summary>
    public string? StripeSubscriptionId { get; private set; }

    /// <summary>The restaurant asked to cancel: the paid plan lasts until <see cref="RenewalDate" />.</summary>
    public bool CancelAtPeriodEnd { get; private set; }

    /// <summary>A card is saved in Stripe (validated, not charged on the free plan).</summary>
    public bool HasCardOnFile { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>Everything but an unconfirmed subscription may use the platform.</summary>
    public bool GrantsAccess => Status != SubscriptionStatus.Incomplete;

    /// <summary>Free plan waiting for the card of the restaurant (Stripe setup, no charge).</summary>
    public static RestaurantSubscription StartFree(int restaurantId, int freePlanId, DateOnly today)
    {
        var subscription = new RestaurantSubscription(restaurantId, freePlanId)
        {
            Status = SubscriptionStatus.Incomplete,
            RenewalDate = today.AddDays(BillingPeriodDays)
        };
        return subscription;
    }

    /// <summary>Activates a plan paid or confirmed today; it renews after one billing period (a month or a year).</summary>
    public RestaurantSubscription Activate(int planId, PaymentMethod paymentMethod, BillingInterval billingInterval, DateOnly today)
    {
        PlanId = planId;
        PaymentMethod = paymentMethod;
        BillingInterval = billingInterval;
        Status = SubscriptionStatus.Active;
        CancelAtPeriodEnd = false;
        RenewalDate = billingInterval == BillingInterval.Annual ? today.AddMonths(AnnualBillingPeriodMonths) : today.AddDays(BillingPeriodDays);
        return this;
    }

    public RestaurantSubscription UseStripeCustomer(string customerId)
    {
        StripeCustomerId = customerId;
        return this;
    }

    /// <summary>A card was saved in Stripe for the customer.</summary>
    public RestaurantSubscription LinkCard(string customerId)
    {
        StripeCustomerId = customerId;
        HasCardOnFile = true;
        PaymentMethod = PaymentMethod.Stripe;
        return this;
    }

    /// <summary>Stripe confirmed a paid plan. The renewal date is the end of the paid period.</summary>
    public RestaurantSubscription StartPaidPlan(int planId, BillingInterval billingInterval, string stripeSubscriptionId, DateOnly renewalDate)
    {
        PlanId = planId;
        BillingInterval = billingInterval;
        StripeSubscriptionId = stripeSubscriptionId;
        RenewalDate = renewalDate;
        Status = SubscriptionStatus.Active;
        CancelAtPeriodEnd = false;
        HasCardOnFile = true;
        PaymentMethod = PaymentMethod.Stripe;
        return this;
    }

    public RestaurantSubscription UpdatePeriod(DateOnly renewalDate, bool cancelAtPeriodEnd)
    {
        RenewalDate = renewalDate;
        CancelAtPeriodEnd = cancelAtPeriodEnd;
        return this;
    }

    /// <summary>The last invoice failed: the plan stays until the grace period of the lifecycle ends (phase 4).</summary>
    public RestaurantSubscription MarkPastDue()
    {
        if (Status == SubscriptionStatus.Active) Status = SubscriptionStatus.PastDue;
        return this;
    }

    public RestaurantSubscription MarkPaid()
    {
        if (Status == SubscriptionStatus.PastDue) Status = SubscriptionStatus.Active;
        return this;
    }

    /// <summary>
    ///     Back to the free plan. Nothing is deleted: the entitlements of the free plan apply from the next request.
    ///     Without a card the restaurant must link one again before using the platform.
    /// </summary>
    public RestaurantSubscription DowngradeToFree(int freePlanId, DateOnly today)
    {
        PlanId = freePlanId;
        BillingInterval = BillingInterval.Monthly;
        StripeSubscriptionId = null;
        CancelAtPeriodEnd = false;
        Status = HasCardOnFile ? SubscriptionStatus.Active : SubscriptionStatus.Incomplete;
        RenewalDate = today.AddDays(BillingPeriodDays);
        return this;
    }
}
