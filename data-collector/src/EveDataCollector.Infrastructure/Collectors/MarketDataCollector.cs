using Microsoft.Extensions.Logging;

namespace EveDataCollector.Infrastructure.Collectors;

/// <summary>
/// Orchestrates market data collection from ESI
/// </summary>
public class MarketDataCollector
{
    private readonly MarketOrdersCollector _ordersCollector;
    private readonly MarketPricesCollector _pricesCollector;
    private readonly MarketHistoryCollector _historyCollector;
    private readonly ILogger<MarketDataCollector> _logger;

    public MarketDataCollector(
        MarketOrdersCollector ordersCollector,
        MarketPricesCollector pricesCollector,
        MarketHistoryCollector historyCollector,
        ILogger<MarketDataCollector> logger)
    {
        _ordersCollector = ordersCollector;
        _pricesCollector = pricesCollector;
        _historyCollector = historyCollector;
        _logger = logger;
    }

    /// <summary>
    /// Collects all market data (prices, orders for specified regions)
    /// </summary>
    public async Task CollectAllMarketDataAsync(
        IEnumerable<int> regionIds,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting market data collection");

        try
        {
            // 1. Collect global market prices first (no region dependency)
            _logger.LogInformation("Collecting global market prices");
            await _pricesCollector.CollectMarketPricesAsync(cancellationToken);

            // 2. Collect market orders for each region
            _logger.LogInformation("Collecting market orders for {Count} regions", regionIds.Count());
            await _ordersCollector.CollectOrdersForRegionsAsync(regionIds, cancellationToken);

            _logger.LogInformation("Market data collection completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during market data collection");
            throw;
        }
    }

    /// <summary>
    /// Collects market orders for specific regions
    /// </summary>
    public async Task CollectMarketOrdersAsync(
        IEnumerable<int> regionIds,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Collecting market orders for {Count} regions", regionIds.Count());

        try
        {
            await _ordersCollector.CollectOrdersForRegionsAsync(regionIds, cancellationToken);
            _logger.LogInformation("Market orders collection completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting market orders");
            throw;
        }
    }

    /// <summary>
    /// Collects global market prices only
    /// </summary>
    public async Task CollectMarketPricesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Collecting global market prices");

        try
        {
            await _pricesCollector.CollectMarketPricesAsync(cancellationToken);
            _logger.LogInformation("Market prices collection completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting market prices");
            throw;
        }
    }

    /// <summary>
    /// Collects market history for specific item types in a region
    /// Note: This can be resource-intensive, use sparingly and with specific type lists
    /// </summary>
    public async Task CollectMarketHistoryAsync(
        int regionId,
        IEnumerable<int> typeIds,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Collecting market history for {Count} types in region {RegionId}",
            typeIds.Count(), regionId);

        try
        {
            await _historyCollector.CollectHistoryForTypesAsync(regionId, typeIds, cancellationToken);
            _logger.LogInformation("Market history collection completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting market history");
            throw;
        }
    }
}
