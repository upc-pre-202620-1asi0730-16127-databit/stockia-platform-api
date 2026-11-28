using Microsoft.EntityFrameworkCore;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Configuration;
using DataBit.StockIA.Platform.Shared.Infrastructure.Persistence.EFC.Repositories;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Repositories;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Infrastructure.Persistence.EFC.Repositories;

public class PaymentTransactionRepository(AppDbContext context) : BaseRepository<PaymentTransaction>(context), IPaymentTransactionRepository
{
    public Task<bool> ExistsByInvoiceIdAsync(string invoiceId, CancellationToken cancellationToken = default) =>
        Context.Set<PaymentTransaction>().AnyAsync(transaction => transaction.InvoiceId == invoiceId, cancellationToken);
}
