using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Core.Models.Market;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EveDataCollector.Infrastructure.Collectors;

/// <summary>
/// Collects global market prices from ESI
/// </summary>
public class MarketPricesCollector
{
    private readonly HttpClient _httpClient;
    private readonly IMarketRepository _marketRepository;
    private readonly ILogger<MarketPricesCollector> _logger;

    public MarketPricesCollector(
        HttpClient httpClient,
        IMarketRepository marketRepository,
        ILogger<MarketPricesCollector> logger)
    {
        _httpClient = httpClient;
        _marketRepository = marketRepository;
        _logger = logger;
    }

    /// <summary>
    /// Collects global market prices (CCP calculated)
    /// </summary>
    public async Task CollectMarketPricesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Collecting global market prices");

        try
        {
            var response = await _httpClient.GetAsync("/markets/prices/", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch market prices: {StatusCode}", response.StatusCode);
                return;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var prices = JsonSerializer.Deserialize<List<PriceResponse>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (prices == null || prices.Count == 0)
            {
                _logger.LogWarning("No market prices returned from ESI");
                return;
            }

            var now = DateTime.UtcNow;

            // Map to domain model
            var mappedPrices = prices.Select(p => new MarketPrice
            {
                TypeId = p.TypeId,
                AdjustedPrice = p.AdjustedPrice,
                AveragePrice = p.AveragePrice,
                CreatedAt = now,
                UpdatedAt = now
            }).ToList();

            await _marketRepository.UpsertMarketPricesAsync(mappedPrices, cancellationToken);

            _logger.LogInformation("Collected {Count} market prices", mappedPrices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting market prices");
            throw;
        }
    }

    #region Response DTOs

    private class PriceResponse
    {
        public int TypeId { get; set; }
        public decimal? AdjustedPrice { get; set; }
        public decimal? AveragePrice { get; set; }
    }

    #endregion
}
