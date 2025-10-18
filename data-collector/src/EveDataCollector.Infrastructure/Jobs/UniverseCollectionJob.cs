using EveDataCollector.Core.Interfaces.Jobs;
using EveDataCollector.Infrastructure.Collectors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EveDataCollector.Infrastructure.Jobs;

/// <summary>
/// Scheduled job for collecting Universe static data
/// </summary>
public class UniverseCollectionJob : IScheduledJob
{
    private readonly UniverseCollector _collector;
    private readonly ILogger<UniverseCollectionJob> _logger;
    private readonly string _cronExpression;

    public UniverseCollectionJob(
        UniverseCollector collector,
        IConfiguration configuration,
        ILogger<UniverseCollectionJob> logger)
    {
        _collector = collector;
        _logger = logger;
        _cronExpression = configuration["Scheduling:UniverseCollection:CronExpression"] ?? "0 2 * * *"; // Default: 2 AM daily
    }

    public string JobName => "UniverseCollectionJob";

    public string CronExpression => _cronExpression;

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== Starting scheduled Universe data collection ===");

        try
        {
            await _collector.CollectAllAsync(cancellationToken);
            _logger.LogInformation("=== Scheduled Universe data collection completed successfully ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scheduled Universe data collection failed");
            throw;
        }
    }
}
