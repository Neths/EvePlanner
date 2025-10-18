using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Core.Models.Market;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EveDataCollector.Infrastructure.Collectors;

/// <summary>
/// Collects market orders from ESI
/// </summary>
public class MarketOrdersCollector
{
    private readonly HttpClient _httpClient;
    private readonly IMarketRepository _marketRepository;
    private readonly ILogger<MarketOrdersCollector> _logger;

    public MarketOrdersCollector(
        HttpClient httpClient,
        IMarketRepository marketRepository,
        ILogger<MarketOrdersCollector> logger)
    {
        _httpClient = httpClient;
        _marketRepository = marketRepository;
        _logger = logger;
    }

    /// <summary>
    /// Collects all market orders for a specific region
    /// </summary>
    public async Task CollectOrdersForRegionAsync(int regionId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Collecting market orders for region {RegionId}", regionId);

        var allOrders = new List<MarketOrder>();
        var page = 1;
        var now = DateTime.UtcNow;

        while (true)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"/markets/{regionId}/orders/?page={page}",
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to fetch orders page {Page} for region {RegionId}: {StatusCode}",
                        page, regionId, response.StatusCode);
                    break;
                }

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var orders = JsonSerializer.Deserialize<List<OrderResponse>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (orders == null || orders.Count == 0)
                {
                    break;
                }

                // Map to domain model
                var mappedOrders = orders.Select(o => new MarketOrder
                {
                    OrderId = o.OrderId,
                    TypeId = o.TypeId,
                    RegionId = regionId,
                    LocationId = o.LocationId,
                    SystemId = o.SystemId,
                    IsBuyOrder = o.IsBuyOrder,
                    Price = o.Price,
                    VolumeRemain = o.VolumeRemain,
                    VolumeTotal = o.VolumeTotal,
                    MinVolume = o.MinVolume,
                    Duration = o.Duration,
                    Issued = o.Issued,
                    Range = o.Range,
                    CreatedAt = now,
                    UpdatedAt = now
                }).ToList();

                allOrders.AddRange(mappedOrders);

                _logger.LogDebug("Fetched page {Page} with {Count} orders", page, orders.Count);

                // Check if we need to fetch more pages
                // ESI returns up to 1000 items per page
                if (orders.Count < 1000)
                {
                    break;
                }

                page++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching orders page {Page} for region {RegionId}", page, regionId);
                break;
            }
        }

        if (allOrders.Count > 0)
        {
            await _marketRepository.UpsertMarketOrdersAsync(allOrders, cancellationToken);
            _logger.LogInformation("Collected {Count} orders for region {RegionId}", allOrders.Count, regionId);

            // Clean up stale orders (older than 1 hour)
            await _marketRepository.DeleteStaleOrdersAsync(regionId, now.AddHours(-1), cancellationToken);
        }
        else
        {
            _logger.LogWarning("No orders collected for region {RegionId}", regionId);
        }
    }

    /// <summary>
    /// Collects orders for multiple regions
    /// </summary>
    public async Task CollectOrdersForRegionsAsync(IEnumerable<int> regionIds, CancellationToken cancellationToken = default)
    {
        foreach (var regionId in regionIds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await CollectOrdersForRegionAsync(regionId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting orders for region {RegionId}", regionId);
            }
        }
    }

    #region Response DTOs

    private class OrderResponse
    {
        public long OrderId { get; set; }
        public int TypeId { get; set; }
        public long LocationId { get; set; }
        public int SystemId { get; set; }
        public bool IsBuyOrder { get; set; }
        public decimal Price { get; set; }
        public int VolumeRemain { get; set; }
        public int VolumeTotal { get; set; }
        public int MinVolume { get; set; }
        public int Duration { get; set; }
        public DateTime Issued { get; set; }
        public string? Range { get; set; }
    }

    #endregion
}
