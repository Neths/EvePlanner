using Dapper;
using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Core.Models.Market;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EveDataCollector.Infrastructure.Repositories;

public class MarketRepository : IMarketRepository
{
    private readonly string _connectionString;
    private readonly ILogger<MarketRepository> _logger;

    public MarketRepository(IConfiguration configuration, ILogger<MarketRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        _logger = logger;
    }

    #region Market Orders

    public async Task UpsertMarketOrdersAsync(IEnumerable<MarketOrder> orders, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO market_orders (
                order_id, type_id, region_id, location_id, system_id,
                is_buy_order, price, volume_remain, volume_total, min_volume,
                duration, issued, range, created_at, updated_at
            ) VALUES (
                @OrderId, @TypeId, @RegionId, @LocationId, @SystemId,
                @IsBuyOrder, @Price, @VolumeRemain, @VolumeTotal, @MinVolume,
                @Duration, @Issued, @Range, @CreatedAt, @UpdatedAt
            )
            ON CONFLICT (order_id)
            DO UPDATE SET
                price = EXCLUDED.price,
                volume_remain = EXCLUDED.volume_remain,
                updated_at = EXCLUDED.updated_at";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var count = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            orders,
            cancellationToken: cancellationToken));

        _logger.LogInformation("Upserted {Count} market orders", count);
    }

    public async Task<IEnumerable<MarketOrder>> GetMarketOrdersAsync(int regionId, int? typeId = null, CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT order_id AS OrderId, type_id AS TypeId, region_id AS RegionId,
                   location_id AS LocationId, system_id AS SystemId,
                   is_buy_order AS IsBuyOrder, price AS Price,
                   volume_remain AS VolumeRemain, volume_total AS VolumeTotal,
                   min_volume AS MinVolume, duration AS Duration,
                   issued AS Issued, range AS Range,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM market_orders
            WHERE region_id = @RegionId";

        if (typeId.HasValue)
        {
            sql += " AND type_id = @TypeId";
        }

        sql += " ORDER BY price";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<MarketOrder>(new CommandDefinition(
            sql,
            new { RegionId = regionId, TypeId = typeId },
            cancellationToken: cancellationToken));
    }

    public async Task DeleteStaleOrdersAsync(int regionId, DateTime olderThan, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            DELETE FROM market_orders
            WHERE region_id = @RegionId
              AND updated_at < @OlderThan";

        await using var connection = new NpgsqlConnection(_connectionString);
        var count = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { RegionId = regionId, OlderThan = olderThan },
            cancellationToken: cancellationToken));

        _logger.LogInformation("Deleted {Count} stale market orders from region {RegionId}", count, regionId);
    }

    #endregion

    #region Market Prices

    public async Task UpsertMarketPricesAsync(IEnumerable<MarketPrice> prices, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO market_prices (
                type_id, adjusted_price, average_price, created_at, updated_at
            ) VALUES (
                @TypeId, @AdjustedPrice, @AveragePrice, @CreatedAt, @UpdatedAt
            )
            ON CONFLICT (type_id)
            DO UPDATE SET
                adjusted_price = EXCLUDED.adjusted_price,
                average_price = EXCLUDED.average_price,
                updated_at = EXCLUDED.updated_at";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var count = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            prices,
            cancellationToken: cancellationToken));

        _logger.LogInformation("Upserted {Count} market prices", count);
    }

    public async Task<MarketPrice?> GetMarketPriceAsync(int typeId, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT type_id AS TypeId,
                   adjusted_price AS AdjustedPrice,
                   average_price AS AveragePrice,
                   created_at AS CreatedAt,
                   updated_at AS UpdatedAt
            FROM market_prices
            WHERE type_id = @TypeId";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<MarketPrice>(new CommandDefinition(
            sql,
            new { TypeId = typeId },
            cancellationToken: cancellationToken));
    }

    public async Task<IEnumerable<MarketPrice>> GetAllMarketPricesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT type_id AS TypeId,
                   adjusted_price AS AdjustedPrice,
                   average_price AS AveragePrice,
                   created_at AS CreatedAt,
                   updated_at AS UpdatedAt
            FROM market_prices
            ORDER BY type_id";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<MarketPrice>(new CommandDefinition(
            sql,
            cancellationToken: cancellationToken));
    }

    #endregion

    #region Market History

    public async Task UpsertMarketHistoryAsync(IEnumerable<MarketHistory> history, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO market_history (
                type_id, region_id, date, average, highest, lowest,
                volume, order_count, created_at
            ) VALUES (
                @TypeId, @RegionId, @Date, @Average, @Highest, @Lowest,
                @Volume, @OrderCount, @CreatedAt
            )
            ON CONFLICT (type_id, region_id, date)
            DO UPDATE SET
                average = EXCLUDED.average,
                highest = EXCLUDED.highest,
                lowest = EXCLUDED.lowest,
                volume = EXCLUDED.volume,
                order_count = EXCLUDED.order_count";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var count = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            history,
            cancellationToken: cancellationToken));

        _logger.LogInformation("Upserted {Count} market history records", count);
    }

    public async Task<IEnumerable<MarketHistory>> GetMarketHistoryAsync(
        int typeId,
        int regionId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var sql = @"
            SELECT type_id AS TypeId, region_id AS RegionId, date AS Date,
                   average AS Average, highest AS Highest, lowest AS Lowest,
                   volume AS Volume, order_count AS OrderCount,
                   created_at AS CreatedAt
            FROM market_history
            WHERE type_id = @TypeId
              AND region_id = @RegionId";

        if (startDate.HasValue)
        {
            sql += " AND date >= @StartDate";
        }

        if (endDate.HasValue)
        {
            sql += " AND date <= @EndDate";
        }

        sql += " ORDER BY date DESC";

        await using var connection = new NpgsqlConnection(_connectionString);
        return await connection.QueryAsync<MarketHistory>(new CommandDefinition(
            sql,
            new { TypeId = typeId, RegionId = regionId, StartDate = startDate, EndDate = endDate },
            cancellationToken: cancellationToken));
    }

    #endregion
}
