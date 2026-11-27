namespace DataBit.StockIA.Platform.Shared.Application;

public static class RestaurantClockExtensions
{
    /// <summary>First instant of the current month in the restaurant's time zone, as UTC (for monthly plan quotas).</summary>
    public static DateTimeOffset StartOfMonth(this IRestaurantClock clock)
    {
        var today = clock.Today;
        return new DateTimeOffset(new DateTime(today.Year, today.Month, 1), clock.Now.Offset).ToUniversalTime();
    }
}
