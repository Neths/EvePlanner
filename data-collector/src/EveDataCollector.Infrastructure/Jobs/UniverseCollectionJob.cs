using EveDataCollector.Core.Interfaces.Jobs;
using EveDataCollector.Infrastructure.Collectors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EveDataCollector.Infrastructure.Jobs;

/// <summary>
/// Scheduled job for collecting universe data
/// </summary>
public class UniverseCollectionJob : IScheduledJob
{
    private readonly UniverseCollector _collector;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UniverseCollectionJob> _logger;

    public string JobName => "Universe Data Collection";

    public string CronExpression { get; }

    public UniverseCollectionJob(
        UniverseCollector collector,
        IConfiguration configuration,
        ILogger<UniverseCollectionJob> logger)
    {
        _collector = collector;
        _configuration = configuration;
        _logger = logger;

        // Default: Daily at 2 AM (universe data changes rarely)
        CronExpression = configuration["Scheduling:UniverseCollection:CronExpression"] ?? "0 2 * * *";
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting scheduled universe data collection");

        try
        {
            await _collector.CollectAllAsync(cancellationToken);

            _logger.LogInformation("Scheduled universe data collection completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled universe data collection");
            throw;
        }
    }
}
