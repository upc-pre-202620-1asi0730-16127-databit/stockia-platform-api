using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Entities;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Repositories;

/// <summary>Remembers which webhook events were applied (idempotency).</summary>
public interface IProcessedPaymentEventRepository
{
    Task<bool> ExistsAsync(string eventId, CancellationToken cancellationToken = default);
    Task AddAsync(ProcessedPaymentEvent processedEvent, CancellationToken cancellationToken = default);
}
