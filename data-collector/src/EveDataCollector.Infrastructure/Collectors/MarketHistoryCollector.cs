using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Core.Models.Market;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EveDataCollector.Infrastructure.Collectors;

/// <summary>
/// Collects market history from ESI
/// </summary>
public class MarketHistoryCollector
{
    private readonly HttpClient _httpClient;
    private readonly IMarketRepository _marketRepository;
    private readonly ILogger<MarketHistoryCollector> _logger;

    public MarketHistoryCollector(
        HttpClient httpClient,
        IMarketRepository marketRepository,
        ILogger<MarketHistoryCollector> logger)
    {
        _httpClient = httpClient;
        _marketRepository = marketRepository;
        _logger = logger;
    }

    /// <summary>
    /// Collects market history for a specific item type in a region
    /// </summary>
    public async Task CollectHistoryAsync(int regionId, int typeId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Collecting market history for type {TypeId} in region {RegionId}", typeId, regionId);

        try
        {
            var response = await _httpClient.GetAsync(
                $"/markets/{regionId}/history/?type_id={typeId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    // No history available for this item
                    _logger.LogDebug("No history available for type {TypeId} in region {RegionId}", typeId, regionId);
                    return;
                }

                _logger.LogWarning("Failed to fetch history for type {TypeId} in region {RegionId}: {StatusCode}",
                    typeId, regionId, response.StatusCode);
                return;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var history = JsonSerializer.Deserialize<List<HistoryResponse>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (history == null || history.Count == 0)
            {
                _logger.LogDebug("No history data for type {TypeId} in region {RegionId}", typeId, regionId);
                return;
            }

            var now = DateTime.UtcNow;

            // Map to domain model
            var mappedHistory = history.Select(h => new MarketHistory
            {
                TypeId = typeId,
                RegionId = regionId,
                Date = h.Date,
                Average = h.Average,
                Highest = h.Highest,
                Lowest = h.Lowest,
                Volume = h.Volume,
                OrderCount = h.OrderCount,
                CreatedAt = now
            }).ToList();

            await _marketRepository.UpsertMarketHistoryAsync(mappedHistory, cancellationToken);

            _logger.LogDebug("Collected {Count} history records for type {TypeId} in region {RegionId}",
                mappedHistory.Count, typeId, regionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting history for type {TypeId} in region {RegionId}", typeId, regionId);
        }
    }

    /// <summary>
    /// Collects market history for multiple item types in a region
    /// </summary>
    public async Task CollectHistoryForTypesAsync(
        int regionId,
        IEnumerable<int> typeIds,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Collecting market history for {Count} types in region {RegionId}",
            typeIds.Count(), regionId);

        var collected = 0;
        var failed = 0;

        foreach (var typeId in typeIds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await CollectHistoryAsync(regionId, typeId, cancellationToken);
                collected++;

                // Rate limiting - ESI has limits, add small delay
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect history for type {TypeId} in region {RegionId}",
                    typeId, regionId);
                failed++;
            }
        }

        _logger.LogInformation(
            "Market history collection complete for region {RegionId}: {Collected} succeeded, {Failed} failed",
            regionId, collected, failed);
    }

    #region Response DTOs

    private class HistoryResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("average")]
        public decimal Average { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("highest")]
        public decimal Highest { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("lowest")]
        public decimal Lowest { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("volume")]
        public long Volume { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("order_count")]
        public long OrderCount { get; set; }
    }

    #endregion
}
