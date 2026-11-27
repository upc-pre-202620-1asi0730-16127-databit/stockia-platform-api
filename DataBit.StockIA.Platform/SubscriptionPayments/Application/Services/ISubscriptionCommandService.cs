using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Commands;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.ValueObjects;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Application.Services;

public interface ISubscriptionCommandService
{
    Task<Result<RestaurantSubscription>> Handle(CreateDefaultSubscriptionCommand command, CancellationToken cancellationToken);
    Task<Result<CheckoutLink>> Handle(CreateCheckoutSessionCommand command, CancellationToken cancellationToken);
    Task<Result<CheckoutLink>> Handle(CreatePortalSessionCommand command, CancellationToken cancellationToken);
}
