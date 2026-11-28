using DataBit.StockIA.Platform.Shared.Domain.Repositories;
using DataBit.StockIA.Platform.SubscriptionPayments.Domain.Model.Aggregates;

namespace DataBit.StockIA.Platform.SubscriptionPayments.Domain.Repositories;

public interface IPaymentTransactionRepository : IBaseRepository<PaymentTransaction>
{
    Task<bool> ExistsByInvoiceIdAsync(string invoiceId, CancellationToken cancellationToken = default);
}
