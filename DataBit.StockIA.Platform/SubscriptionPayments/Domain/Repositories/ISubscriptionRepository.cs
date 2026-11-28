using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Repositories;

public interface ISubscriptionRepository : IBaseRepository<RestaurantSubscription>
{
    Task<RestaurantSubscription?> FindByRestaurantIdAsync(int restaurantId, CancellationToken cancellationToken = default);
    Task<RestaurantSubscription?> FindByStripeCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);
    Task<RestaurantSubscription?> FindByStripeSubscriptionIdAsync(string stripeSubscriptionId, CancellationToken cancellationToken = default);
}
