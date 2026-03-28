using System.Globalization;
using System.Text.Json.Serialization;

namespace Models;

public sealed class TwseStockDayAllItem
{
    [JsonPropertyName("Date")]
    public string Date { get; init; } = string.Empty;

    [JsonPropertyName("Code")]
    public string Code { get; init; } = string.Empty;

    [JsonPropertyName("ClosingPrice")]
    public string ClosingPrice { get; init; } = string.Empty;

    public DateOnly GetClosedDate()
    {
        // 民國日期格式: YYYMMDD，YYY 可能為 1~3 碼。
        if (string.IsNullOrWhiteSpace(Date) || Date.Length < 6)
        {
            throw new FormatException($"無法解析民國日期: {Date}");
        }

        var rocYearText = Date[..^4];
        var monthText = Date[^4..^2];
        var dayText = Date[^2..];

        var rocYear = int.Parse(rocYearText, CultureInfo.InvariantCulture);
        var month = int.Parse(monthText, CultureInfo.InvariantCulture);
        var day = int.Parse(dayText, CultureInfo.InvariantCulture);

        return new DateOnly(rocYear + 1911, month, day);
    }

    public decimal GetClosingPrice()
    {
        // 來源可能包含逗號，先去除再轉 decimal。
        var normalized = ClosingPrice.Replace(",", string.Empty, StringComparison.Ordinal);
        return decimal.Parse(normalized, CultureInfo.InvariantCulture);
    }
}
