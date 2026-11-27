namespace DataBit.StockIA.Platform.SubscriptionPayments.Interfaces.REST.Resources;

/// <summary>Null caps mean unlimited. AlertChannels are codes: APP, EMAIL, WHATSAPP.</summary>
public record PlanEntitlementsResource(
    int? MaxUsers,
    int? MaxInventoryItems,
    int? MaxRecipes,
    int? MaxSalesPerMonth,
    int? MaxForecastsPerMonth,
    bool PurchaseRecommendations,
    bool MenuRecommendations,
    bool SustainabilityReports,
    IReadOnlyList<string> AlertChannels,
    bool RealtimeCriticalAlerts,
    bool Automation,
    bool ExternalIntegrations,
    bool PrioritySupport);

public record PlanResource(int Id, string Name, decimal MonthlyPrice, decimal AnnualPrice, IReadOnlyList<string> Features,
    PlanEntitlementsResource Entitlements, bool Highlighted);
