using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Core.Models.Market;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Threading.Channels;

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
    /// Uses a producer/consumer pipeline to parallelize API fetching and database upserts
    /// </summary>
    public async Task CollectOrdersForRegionAsync(int regionId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Collecting market orders for region {RegionId} (pipeline mode)", regionId);
            _logger.LogDebug("HttpClient BaseAddress: {BaseAddress}", _httpClient.BaseAddress);

            var now = DateTime.UtcNow;
            var channel = Channel.CreateUnbounded<List<MarketOrder>>();

        // Producer: Fetch pages from API
        // Note: Don't pass cancellationToken to Task.Run to prevent abrupt cancellation
        var producerTask = Task.Run(async () =>
        {
            var page = 1;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var url = $"/markets/{regionId}/orders/?page={page}";
                    _logger.LogDebug("Producer: Fetching {Url} (BaseAddress: {BaseAddress})",
                        url, _httpClient.BaseAddress);

                    var response = await _httpClient.GetAsync(url, CancellationToken.None);

                    _logger.LogDebug("Producer: Response status {StatusCode} for page {Page}",
                        response.StatusCode, page);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Producer: Failed to fetch page {Page}: {StatusCode}",
                            page, response.StatusCode);
                        break;
                    }

                    var content = await response.Content.ReadAsStringAsync(CancellationToken.None);
                    var orders = JsonSerializer.Deserialize<List<OrderResponse>>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (orders == null || orders.Count == 0)
                    {
                        _logger.LogDebug("Producer: No orders on page {Page}, stopping", page);
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

                    _logger.LogDebug("Producer: Sending {Count} orders from page {Page} to channel",
                        mappedOrders.Count, page);

                    await channel.Writer.WriteAsync(mappedOrders, CancellationToken.None);

                    // Check if we need to fetch more pages
                    if (orders.Count < 1000)
                    {
                        _logger.LogDebug("Producer: Last page detected ({Count} < 1000)", orders.Count);
                        break;
                    }

                    page++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Producer: Error fetching orders for region {RegionId}: {Message}",
                    regionId, ex.Message);
            }
            finally
            {
                channel.Writer.Complete();
                _logger.LogDebug("Producer: Completed, channel closed");
            }
        });

        // Consumer: Upsert to database with batching (single consumer)
        // Note: Don't pass cancellationToken to Task.Run to prevent abrupt cancellation
        var consumerTask = Task.Run(async () =>
        {
            var totalOrders = 0;
            var pageCount = 0;
            const int batchSize = 5; // Accumulate 5 pages (~5000 orders) before upserting
            var batch = new List<MarketOrder>();

            try
            {
                await foreach (var orders in channel.Reader.ReadAllAsync(CancellationToken.None))
                {
                    pageCount++;
                    batch.AddRange(orders);
                    _logger.LogDebug("Consumer: Accumulated page {Page} with {Count} orders (Batch: {BatchSize})",
                        pageCount, orders.Count, batch.Count);

                    // Upsert when batch is full or this is potentially the last page
                    if (batch.Count >= batchSize * 1000 || orders.Count < 1000)
                    {
                        _logger.LogDebug("Consumer: Upserting batch of {Count} orders", batch.Count);
                        await _marketRepository.UpsertMarketOrdersAsync(batch, CancellationToken.None);

                        totalOrders += batch.Count;
                        _logger.LogDebug("Consumer: Batch upserted: {Count} orders (Total: {Total})",
                            batch.Count, totalOrders);

                        batch.Clear();
                    }
                }

                // Upsert any remaining orders in the batch
                if (batch.Count > 0)
                {
                    _logger.LogDebug("Consumer: Upserting final batch of {Count} orders", batch.Count);
                    await _marketRepository.UpsertMarketOrdersAsync(batch, CancellationToken.None);
                    totalOrders += batch.Count;
                }

                _logger.LogInformation("Consumer: Completed {Total} orders across {Pages} pages",
                    totalOrders, pageCount);

                if (totalOrders > 0)
                {
                    // Clean up stale orders (older than 1 hour)
                    _logger.LogDebug("Consumer: Cleaning up stale orders older than {Timestamp}",
                        now.AddHours(-1));
                    await _marketRepository.DeleteStaleOrdersAsync(regionId, now.AddHours(-1), CancellationToken.None);
                }

                return totalOrders;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Consumer: Error upserting orders: {Message}", ex.Message);
                return totalOrders;
            }
        });

            // Wait for both producer and consumer to complete
            await Task.WhenAll(producerTask, consumerTask);
            var totalOrders = await consumerTask;

            if (totalOrders == 0)
            {
                _logger.LogWarning("No orders collected for region {RegionId}", regionId);
            }
            else
            {
                _logger.LogInformation("Successfully collected {Count} orders for region {RegionId}",
                    totalOrders, regionId);
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Market orders collection for region {RegionId} was cancelled", regionId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Market orders collection for region {RegionId} was cancelled", regionId);
        }
    }

    /// <summary>
    /// Collects orders for multiple regions
    /// </summary>
    public async Task CollectOrdersForRegionsAsync(IEnumerable<int> regionIds, CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var regionId in regionIds)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Market orders collection cancelled after processing some regions");
                    break;
                }

                try
                {
                    await CollectOrdersForRegionAsync(regionId, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Market orders collection for region {RegionId} was cancelled", regionId);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Market orders collection for region {RegionId} was cancelled", regionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error collecting orders for region {RegionId}", regionId);
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Market orders collection was cancelled");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Market orders collection was cancelled");
        }
    }

    #region Response DTOs

    private class OrderResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("order_id")]
        public long OrderId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("type_id")]
        public int TypeId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("location_id")]
        public long LocationId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("system_id")]
        public int SystemId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("is_buy_order")]
        public bool IsBuyOrder { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("price")]
        public decimal Price { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("volume_remain")]
        public int VolumeRemain { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("volume_total")]
        public int VolumeTotal { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("min_volume")]
        public int MinVolume { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("duration")]
        public int Duration { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("issued")]
        public DateTime Issued { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("range")]
        public string? Range { get; set; }
    }

    #endregion
}
