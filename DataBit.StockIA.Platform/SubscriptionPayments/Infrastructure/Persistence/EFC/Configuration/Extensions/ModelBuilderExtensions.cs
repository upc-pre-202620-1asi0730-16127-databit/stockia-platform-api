using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration.Extensions;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Entities;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Infrastructure.Persistence.EFC.Configuration.Extensions;

public static class ModelBuilderExtensions
{
    public static void ApplySubscriptionPaymentsConfiguration(this ModelBuilder builder)
    {
        builder.Entity<Plan>(entity =>
        {
            entity.HasKey(plan => plan.Id);
            entity.Property(plan => plan.Id).ValueGeneratedOnAdd();
            entity.Property(plan => plan.Name).IsRequired().HasMaxLength(60);
            entity.Property(plan => plan.MonthlyPrice).HasPrecision(10, 2);
            entity.Property(plan => plan.Features).HasStringListConversion();
            // Complex type: the entitlements are columns of plans (entitlements_max_users, ...), without a key of their own.
            entity.ComplexProperty(plan => plan.Entitlements, complex =>
            {
                complex.Property(entitlements => entitlements.MaxUsers).HasColumnName("entitlements_max_users");
                complex.Property(entitlements => entitlements.MaxInventoryItems).HasColumnName("entitlements_max_inventory_items");
                complex.Property(entitlements => entitlements.MaxRecipes).HasColumnName("entitlements_max_recipes");
                complex.Property(entitlements => entitlements.MaxSalesPerMonth).HasColumnName("entitlements_max_sales_per_month");
                complex.Property(entitlements => entitlements.MaxForecastsPerMonth).HasColumnName("entitlements_max_forecasts_per_month");
                complex.Property(entitlements => entitlements.PurchaseRecommendations).HasColumnName("entitlements_purchase_recommendations");
                complex.Property(entitlements => entitlements.MenuRecommendations).HasColumnName("entitlements_menu_recommendations");
                complex.Property(entitlements => entitlements.SustainabilityReports).HasColumnName("entitlements_sustainability_reports");
                complex.Property(entitlements => entitlements.AlertChannels).HasConversion<int>().HasColumnName("entitlements_alert_channels");
                complex.Property(entitlements => entitlements.RealtimeCriticalAlerts).HasColumnName("entitlements_realtime_critical_alerts");
                complex.Property(entitlements => entitlements.Automation).HasColumnName("entitlements_automation");
                complex.Property(entitlements => entitlements.ExternalIntegrations).HasColumnName("entitlements_external_integrations");
                complex.Property(entitlements => entitlements.PrioritySupport).HasColumnName("entitlements_priority_support");
            });
            entity.Ignore(plan => plan.IsFree);
            entity.Ignore(plan => plan.AnnualPrice);
        });

        builder.Entity<RestaurantSubscription>(entity =>
        {
            entity.ToTable("Subscription");
            entity.HasKey(subscription => subscription.Id);
            entity.Property(subscription => subscription.Id).ValueGeneratedOnAdd();
            entity.HasIndex(subscription => subscription.RestaurantId).IsUnique();
            entity.Property(subscription => subscription.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(subscription => subscription.PaymentMethod).HasConversion<string>().HasMaxLength(20);
            entity.Property(subscription => subscription.BillingInterval).HasConversion<string>().HasMaxLength(20);
            entity.Property(subscription => subscription.StripeCustomerId).HasMaxLength(64);
            entity.Property(subscription => subscription.StripeSubscriptionId).HasMaxLength(64);
            entity.HasIndex(subscription => subscription.StripeCustomerId);
            entity.HasIndex(subscription => subscription.StripeSubscriptionId);
            entity.Property(subscription => subscription.RenewalDate).HasDateOnlyConversion();
            entity.HasOne<Plan>().WithMany().HasForeignKey(subscription => subscription.PlanId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(transaction => transaction.Id);
            entity.Property(transaction => transaction.Id).ValueGeneratedOnAdd();
            entity.Property(transaction => transaction.InvoiceId).IsRequired().HasMaxLength(64);
            entity.HasIndex(transaction => transaction.InvoiceId).IsUnique();
            entity.HasIndex(transaction => transaction.RestaurantId);
            entity.Property(transaction => transaction.Amount).HasPrecision(10, 2);
            entity.Property(transaction => transaction.Currency).IsRequired().HasMaxLength(3);
            entity.Property(transaction => transaction.Status).HasConversion<string>().HasMaxLength(20);
        });

        builder.Entity<ProcessedPaymentEvent>(entity =>
        {
            entity.HasKey(processedEvent => processedEvent.EventId);
            entity.Property(processedEvent => processedEvent.EventId).HasMaxLength(64);
            entity.Property(processedEvent => processedEvent.Type).IsRequired().HasMaxLength(80);
        });
    }
}
