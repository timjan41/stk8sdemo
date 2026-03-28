namespace Models;

public sealed class StockClosingPriceUpsertItem
{
    public string StockNo { get; init; } = string.Empty;
    public DateOnly ClosedDate { get; init; }
    public decimal Price { get; init; }
    public string CreateBy { get; init; } = string.Empty;
    public string CreateIp { get; init; } = string.Empty;
    public string UpdateBy { get; init; } = string.Empty;
    public string UpdateIp { get; init; } = string.Empty;
}
