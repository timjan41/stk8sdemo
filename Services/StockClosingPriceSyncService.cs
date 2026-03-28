using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Models;
using Repositories;
using System.Net.Http.Json;

namespace Services;

public sealed class StockClosingPriceSyncService : IStockClosingPriceSyncService
{
    private readonly IStockRepository _stockRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StockClosingPriceSyncService> _logger;
    private readonly string _endpoint;
    private readonly string _createBy;
    private readonly string _createIp;
    private readonly string _updateBy;
    private readonly string _updateIp;

    public StockClosingPriceSyncService(
        IStockRepository stockRepository,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<StockClosingPriceSyncService> logger)
    {
        _stockRepository = stockRepository;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        _endpoint = configuration["Twse:StockDayAllEndpoint"]
            ?? throw new InvalidOperationException("找不到設定值: Twse:StockDayAllEndpoint");

        _createBy = configuration["SyncOptions:CreateBy"] ?? "system";
        _createIp = configuration["SyncOptions:CreateIp"] ?? "127.0.0.1";
        _updateBy = configuration["SyncOptions:UpdateBy"] ?? "system";
        _updateIp = configuration["SyncOptions:UpdateIp"] ?? "127.0.0.1";
    }

    public async Task SyncAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("開始同步股票每日收盤市價。");

        var stockNos = await _stockRepository.GetStockNosAsync(cancellationToken);
        if (stockNos.Count == 0)
        {
            _logger.LogWarning("stock_info 無任何股票代碼，略過同步。");
            return;
        }

        var stockNoSet = stockNos.ToHashSet(StringComparer.Ordinal);
        var client = _httpClientFactory.CreateClient();

        var apiRows = await client.GetFromJsonAsync<List<TwseStockDayAllItem>>(_endpoint, cancellationToken)
                      ?? new List<TwseStockDayAllItem>();

        if (apiRows.Count == 0)
        {
            _logger.LogWarning("TWSE API 回傳空資料，略過同步。");
            return;
        }

        var upsertItems = new List<StockClosingPriceUpsertItem>();

        foreach (var row in apiRows)
        {
            if (!stockNoSet.Contains(row.Code))
            {
                continue;
            }

            try
            {
                var closedDate = row.GetClosedDate();
                var price = row.GetClosingPrice();

                upsertItems.Add(new StockClosingPriceUpsertItem
                {
                    StockNo = row.Code,
                    ClosedDate = closedDate,
                    Price = price,
                    CreateBy = _createBy,
                    CreateIp = _createIp,
                    UpdateBy = _updateBy,
                    UpdateIp = _updateIp
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "資料解析失敗，已略過。Code={Code}, Date={Date}, ClosingPrice={ClosingPrice}", row.Code, row.Date, row.ClosingPrice);
            }
        }

        if (upsertItems.Count == 0)
        {
            _logger.LogWarning("API 與 stock_info 無交集，或資料解析皆失敗，未寫入任何資料。");
            return;
        }

        var affected = await _stockRepository.UpsertStockClosingPricesAsync(upsertItems, cancellationToken);
        _logger.LogInformation("同步完成，處理筆數: {Count}，影響筆數: {Affected}。", upsertItems.Count, affected);
    }
}
