using EveDataCollector.Core.Models.Market;

namespace EveDataCollector.Core.Interfaces.Repositories;

/// <summary>
/// Repository for market data operations
/// </summary>
public interface IMarketRepository
{
    // Market Orders
    Task UpsertMarketOrdersAsync(IEnumerable<MarketOrder> orders, CancellationToken cancellationToken = default);
    Task<IEnumerable<MarketOrder>> GetMarketOrdersAsync(int regionId, int? typeId = null, CancellationToken cancellationToken = default);
    Task DeleteStaleOrdersAsync(int regionId, DateTime olderThan, CancellationToken cancellationToken = default);

    // Market Prices
    Task UpsertMarketPricesAsync(IEnumerable<MarketPrice> prices, CancellationToken cancellationToken = default);
    Task<MarketPrice?> GetMarketPriceAsync(int typeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MarketPrice>> GetAllMarketPricesAsync(CancellationToken cancellationToken = default);

    // Market History
    Task UpsertMarketHistoryAsync(IEnumerable<MarketHistory> history, CancellationToken cancellationToken = default);
    Task<IEnumerable<MarketHistory>> GetMarketHistoryAsync(int typeId, int regionId, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
}
