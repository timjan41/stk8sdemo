using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Models;

namespace Repositories;

public sealed class StockRepository : IStockRepository
{
    private readonly string _connectionString;

    public StockRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("SafeSchooling")
            ?? throw new InvalidOperationException("找不到連線字串: ConnectionStrings:SafeSchooling");
    }

    public async Task<IReadOnlyCollection<string>> GetStockNosAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT [stockNo] FROM [dbo].[stock_info];";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(sql, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<string>(command);

        return rows.Where(x => !string.IsNullOrWhiteSpace(x))
                   .Select(x => x.Trim())
                   .Distinct(StringComparer.Ordinal)
                   .ToArray();
    }

    public async Task<int> UpsertStockClosingPricesAsync(
        IReadOnlyCollection<StockClosingPriceUpsertItem> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return 0;
        }

        const string sql = @"
MERGE [dbo].[stock_closing_price] AS target
USING (
    SELECT
        @StockNo AS StockNo,
        @ClosedDate AS ClosedDate,
        @Price AS Price,
        @CreateBy AS CreateBy,
        @CreateIp AS CreateIp,
        @UpdateBy AS UpdateBy,
        @UpdateIp AS UpdateIp
) AS src
ON target.[stockNo] = src.StockNo
AND target.[closedDate] = src.ClosedDate
WHEN MATCHED THEN
    UPDATE SET
        target.[price] = src.Price,
        target.[updateBy] = src.UpdateBy,
        target.[updateAt] = GETDATE(),
        target.[updateIP] = src.UpdateIp
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([stockNo], [closedDate], [price], [createBy], [createAt], [createIP], [updateBy], [updateAt], [updateIP])
    VALUES (src.StockNo, src.ClosedDate, src.Price, src.CreateBy, GETDATE(), src.CreateIp, src.UpdateBy, GETDATE(), src.UpdateIp);
";

        var parameters = items.Select(item => new
        {
            item.StockNo,
            ClosedDate = item.ClosedDate.ToDateTime(TimeOnly.MinValue),
            item.Price,
            item.CreateBy,
            item.CreateIp,
            item.UpdateBy,
            item.UpdateIp
        });

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var command = new CommandDefinition(sql, parameters, transaction, cancellationToken: cancellationToken);
            var affected = await connection.ExecuteAsync(command);
            await transaction.CommitAsync(cancellationToken);
            return affected;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
