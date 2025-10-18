using EveDataCollector.Core.Interfaces.Jobs;
using EveDataCollector.Infrastructure.Collectors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EveDataCollector.Infrastructure.Jobs;

/// <summary>
/// Scheduled job for collecting market data
/// </summary>
public class MarketCollectionJob : IScheduledJob
{
    private readonly MarketDataCollector _collector;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MarketCollectionJob> _logger;

    public string JobName => "Market Data Collection";

    public string CronExpression { get; }

    public MarketCollectionJob(
        MarketDataCollector collector,
        IConfiguration configuration,
        ILogger<MarketCollectionJob> logger)
    {
        _collector = collector;
        _configuration = configuration;
        _logger = logger;

        // Default: Every 15 minutes (market data updates frequently)
        CronExpression = configuration["Scheduling:MarketCollection:CronExpression"] ?? "*/15 * * * *";
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting scheduled market data collection");

        try
        {
            // Get regions from configuration
            var regionsConfig = _configuration.GetSection("Scheduling:MarketCollection:Regions").Get<int[]>();

            // Default to major trade hubs if not configured
            var regions = regionsConfig ?? new[]
            {
                10000002,  // The Forge (Jita)
                10000043,  // Domain (Amarr)
                10000032,  // Sinq Laison (Dodixie)
                10000030,  // Heimatar (Rens)
                10000042   // Metropolis (Hek)
            };

            _logger.LogInformation("Collecting market data for {Count} regions", regions.Length);

            await _collector.CollectAllMarketDataAsync(regions, cancellationToken);

            _logger.LogInformation("Scheduled market data collection completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled market data collection");
            throw;
        }
    }
}
