namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

/// <summary>Delivery channels a plan allows for alerts (the app is always included).</summary>
[Flags]
public enum AlertChannels
{
    None = 0,
    App = 1,
    Email = 2,
    Whatsapp = 4
}
