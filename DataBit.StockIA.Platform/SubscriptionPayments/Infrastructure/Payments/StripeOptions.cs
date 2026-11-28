using DataBit.StockIA.Platform.Shared.Domain.Model.ValueObjects;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Infrastructure.Payments;

/// <summary>
///     Stripe settings, read from environment variables so no secret is ever in the repository:
///     STOCKIA_STRIPE_SECRET_KEY (sk_...), STOCKIA_STRIPE_WEBHOOK_SECRET (whsec_...), STOCKIA_WEBAPP_URL (where Stripe
///     sends the user back) and one price id per paid plan and interval, STOCKIA_STRIPE_PRICE_{PLAN}_{INTERVAL}
///     (e.g. STOCKIA_STRIPE_PRICE_STARTER_MONTHLY, STOCKIA_STRIPE_PRICE_PROFESSIONAL_ANNUAL). The API starts without them;
///     only the payment endpoints answer "not configured".
/// </summary>
public class StripeOptions
{
    public const string SecretKeyVariable = "STOCKIA_STRIPE_SECRET_KEY";
    public const string WebhookSecretVariable = "STOCKIA_STRIPE_WEBHOOK_SECRET";
    public const string WebAppUrlVariable = "STOCKIA_WEBAPP_URL";

    public string? SecretKey { get; init; }
    public string? WebhookSecret { get; init; }
    public string WebAppBaseUrl { get; init; } = "http://localhost:5173";

    public static StripeOptions FromEnvironment() => new()
    {
        SecretKey = Environment.GetEnvironmentVariable(SecretKeyVariable),
        WebhookSecret = Environment.GetEnvironmentVariable(WebhookSecretVariable),
        WebAppBaseUrl = Environment.GetEnvironmentVariable(WebAppUrlVariable) is { Length: > 0 } url ? url.TrimEnd('/') : "http://localhost:5173"
    };

    /// <summary>Name of the variable that holds the Stripe price id of a plan and interval.</summary>
    public static string PriceVariableOf(Plan plan, BillingInterval interval) =>
        $"STOCKIA_STRIPE_PRICE_{plan.Name.ToUpperInvariant().Replace(' ', '_')}_{interval.ToCode()}";

    public string? PriceIdOf(Plan plan, BillingInterval interval) =>
        Environment.GetEnvironmentVariable(PriceVariableOf(plan, interval)) is { Length: > 0 } id ? id : null;

    public string SuccessUrl => $"{WebAppBaseUrl}/settings/plans?checkout=success";
    public string CancelUrl => $"{WebAppBaseUrl}/settings/plans?checkout=cancel";
}
