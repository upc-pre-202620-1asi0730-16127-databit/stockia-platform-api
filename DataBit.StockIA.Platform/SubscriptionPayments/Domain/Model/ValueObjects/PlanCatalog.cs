namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

/// <summary>
///     Names and marketing texts of the three plans of a fresh database. The
///     texts only describe the plan; what is enforced is PlanEntitlements. What
///     is sold but not built yet says "próximamente" until it exists.
/// </summary>
public static class PlanCatalog
{
    public const string EssentialName = "Essential";
    public const string StarterName = "Starter";
    public const string ProfessionalName = "Professional";

    public static List<string> EssentialFeatures =>
        ["Hasta 50 insumos", "Registro de ventas básico", "1 usuario", "Alertas en la app"];

    public static List<string> StarterFeatures =>
    [
        "Insumos ilimitados", "Hasta 5 usuarios", "Predicción de demanda con IA", "Recomendaciones automáticas de compra",
        "Reportes de sostenibilidad", "Alertas en la app y por correo"
    ];

    public static List<string> ProfessionalFeatures =>
    [
        "Todo lo de Starter", "Usuarios ilimitados", "Recomendaciones con IA de compra y de menú", "Alertas por WhatsApp",
        "Alertas críticas en tiempo real (próximamente)", "Automatización de procesos (próximamente)",
        "Integración con servicios externos (próximamente)", "Soporte prioritario"
    ];
}
