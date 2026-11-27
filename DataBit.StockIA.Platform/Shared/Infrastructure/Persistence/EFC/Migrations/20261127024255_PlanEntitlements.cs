using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Migrations
{
    /// <inheritdoc />
    public partial class PlanEntitlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "billing_interval",
                table: "subscriptions",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Monthly");

            migrationBuilder.AddColumn<int>(
                name: "entitlements_alert_channels",
                table: "plans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "entitlements_automation",
                table: "plans",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "entitlements_external_integrations",
                table: "plans",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "entitlements_max_forecasts_per_month",
                table: "plans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "entitlements_max_inventory_items",
                table: "plans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "entitlements_max_recipes",
                table: "plans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "entitlements_max_sales_per_month",
                table: "plans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "entitlements_max_users",
                table: "plans",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "entitlements_menu_recommendations",
                table: "plans",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "entitlements_priority_support",
                table: "plans",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "entitlements_purchase_recommendations",
                table: "plans",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "entitlements_realtime_critical_alerts",
                table: "plans",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "entitlements_sustainability_reports",
                table: "plans",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            // Data migration for databases that already have the old catalog. Ids 1, 2 and 3 are kept so existing
            // subscriptions still point to the right plan: Esencial -> Essential, Profesional (S/39) -> Starter,
            // IoT Completo (S/79) -> Professional (without IoT). A fresh database has no plans yet: the seeder inserts them.
            // Alert channels: App = 1, Email = 2, Whatsapp = 4.
            UpdatePlan(migrationBuilder, 1, "Essential", 0m, false,
                ["Hasta 50 insumos", "Registro de ventas básico", "1 usuario", "Alertas en la app"],
                maxUsers: 1, maxInventoryItems: 50, maxRecipes: 10, maxSalesPerMonth: 100, maxForecastsPerMonth: 0,
                purchaseRecommendations: false, menuRecommendations: false, sustainabilityReports: false, alertChannels: 1,
                realtimeCriticalAlerts: false, automation: false, externalIntegrations: false, prioritySupport: false);
            UpdatePlan(migrationBuilder, 2, "Starter", 39m, false,
                ["Insumos ilimitados", "Hasta 5 usuarios", "Predicción de demanda con IA", "Recomendaciones automáticas de compra",
                    "Reportes de sostenibilidad", "Alertas en la app y por correo"],
                maxUsers: 5, maxInventoryItems: null, maxRecipes: 50, maxSalesPerMonth: null, maxForecastsPerMonth: 30,
                purchaseRecommendations: true, menuRecommendations: false, sustainabilityReports: true, alertChannels: 3,
                realtimeCriticalAlerts: false, automation: false, externalIntegrations: false, prioritySupport: false);
            UpdatePlan(migrationBuilder, 3, "Professional", 79m, true,
                ["Todo lo de Starter", "Usuarios ilimitados", "Recomendaciones con IA de compra y de menú", "Alertas por WhatsApp",
                    "Alertas críticas en tiempo real (próximamente)", "Automatización de procesos (próximamente)",
                    "Integración con servicios externos (próximamente)", "Soporte prioritario"],
                maxUsers: null, maxInventoryItems: null, maxRecipes: null, maxSalesPerMonth: null, maxForecastsPerMonth: null,
                purchaseRecommendations: true, menuRecommendations: true, sustainabilityReports: true, alertChannels: 7,
                realtimeCriticalAlerts: true, automation: true, externalIntegrations: true, prioritySupport: true);

            // PayPal is no longer a payment method: Stripe is the only provider.
            migrationBuilder.Sql("UPDATE subscriptions SET payment_method = 'Stripe' WHERE payment_method = 'Paypal';");
        }

        private static void UpdatePlan(MigrationBuilder migrationBuilder, int id, string name, decimal monthlyPrice, bool highlighted,
            string[] features, int? maxUsers, int? maxInventoryItems, int? maxRecipes, int? maxSalesPerMonth, int? maxForecastsPerMonth,
            bool purchaseRecommendations, bool menuRecommendations, bool sustainabilityReports, int alertChannels,
            bool realtimeCriticalAlerts, bool automation, bool externalIntegrations, bool prioritySupport)
        {
            migrationBuilder.UpdateData(
                table: "plans",
                keyColumn: "id",
                keyValue: id,
                columns:
                [
                    "name", "monthly_price", "features", "highlighted", "entitlements_max_users", "entitlements_max_inventory_items",
                    "entitlements_max_recipes", "entitlements_max_sales_per_month", "entitlements_max_forecasts_per_month",
                    "entitlements_purchase_recommendations", "entitlements_menu_recommendations", "entitlements_sustainability_reports",
                    "entitlements_alert_channels", "entitlements_realtime_critical_alerts", "entitlements_automation",
                    "entitlements_external_integrations", "entitlements_priority_support"
                ],
                values: new object?[]
                {
                    name, monthlyPrice, JsonSerializer.Serialize(features), highlighted, maxUsers, maxInventoryItems, maxRecipes,
                    maxSalesPerMonth, maxForecastsPerMonth, purchaseRecommendations, menuRecommendations, sustainabilityReports,
                    alertChannels, realtimeCriticalAlerts, automation, externalIntegrations, prioritySupport
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "billing_interval",
                table: "subscriptions");

            migrationBuilder.DropColumn(
                name: "entitlements_alert_channels",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "entitlements_automation",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "entitlements_external_integrations",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "entitlements_max_forecasts_per_month",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "entitlements_max_inventory_items",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "entitlements_max_recipes",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "entitlements_max_sales_per_month",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "entitlements_max_users",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "entitlements_menu_recommendations",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "entitlements_priority_support",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "entitlements_purchase_recommendations",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "entitlements_realtime_critical_alerts",
                table: "plans");

            migrationBuilder.DropColumn(
                name: "entitlements_sustainability_reports",
                table: "plans");
        }
    }
}
