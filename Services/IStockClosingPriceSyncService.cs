namespace Services;

public interface IStockClosingPriceSyncService
{
    Task SyncAsync(CancellationToken cancellationToken = default);
}
