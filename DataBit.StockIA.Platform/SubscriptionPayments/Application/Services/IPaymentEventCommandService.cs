using DataBit.StockIA.Platform.Shared.Application.Patterns;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Commands;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Application.Services;

public interface IPaymentEventCommandService
{
    /// <summary>Verifies the signature, then applies the event once. Fails with InvalidWebhookSignature when it is forged.</summary>
    Task<Result> Handle(ReceivePaymentWebhookCommand command, CancellationToken cancellationToken);

    Task<Result> Handle(ProcessPaymentEventCommand command, CancellationToken cancellationToken);
}
