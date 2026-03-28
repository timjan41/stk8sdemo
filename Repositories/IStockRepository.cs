using Models;

namespace Repositories;

public interface IStockRepository
{
    Task<IReadOnlyCollection<string>> GetStockNosAsync(CancellationToken cancellationToken = default);
    Task<int> UpsertStockClosingPricesAsync(
        IReadOnlyCollection<StockClosingPriceUpsertItem> items,
        CancellationToken cancellationToken = default);
}
