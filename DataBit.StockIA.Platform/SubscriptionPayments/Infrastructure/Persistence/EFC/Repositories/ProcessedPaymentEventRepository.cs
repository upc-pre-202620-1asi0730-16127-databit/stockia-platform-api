using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Entities;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Repositories;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Infrastructure.Persistence.EFC.Repositories;

public class ProcessedPaymentEventRepository(AppDbContext context) : IProcessedPaymentEventRepository
{
    public Task<bool> ExistsAsync(string eventId, CancellationToken cancellationToken = default) =>
        context.Set<ProcessedPaymentEvent>().AnyAsync(processedEvent => processedEvent.EventId == eventId, cancellationToken);

    public async Task AddAsync(ProcessedPaymentEvent processedEvent, CancellationToken cancellationToken = default) =>
        await context.Set<ProcessedPaymentEvent>().AddAsync(processedEvent, cancellationToken);
}
