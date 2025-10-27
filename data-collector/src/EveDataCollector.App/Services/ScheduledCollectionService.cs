using EveDataCollector.Infrastructure.Collectors;
using Microsoft.Extensions.Configuration;
using Serilog;
using TickerQ;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Models;

namespace EveDataCollector.App.Services;

/// <summary>
/// Service containing all scheduled collection tasks for TickerQ
/// </summary>
public class ScheduledCollectionService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public ScheduledCollectionService(
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    /// <summary>
    /// Scheduled collection of Universe static data
    /// Runs daily at 2 AM by default
    /// </summary>
    [TickerFunction(
        functionName: "UniverseCollection",
        cronExpression: "0 2 * * *")]
    public async Task CollectUniverseDataAsync(
        TickerFunctionContext<string> tickerContext,
        CancellationToken cancellationToken)
    {
        Log.Information("Starting scheduled Universe data collection");

        using var scope = _serviceProvider.CreateScope();
        var collector = scope.ServiceProvider.GetRequiredService<UniverseCollector>();

        try
        {
            await collector.CollectAllAsync(cancellationToken);
            Log.Information("Scheduled Universe data collection completed successfully");
        }
        catch (TaskCanceledException)
        {
            Log.Information("Scheduled Universe data collection was cancelled");
        }
        catch (OperationCanceledException)
        {
            Log.Information("Scheduled Universe data collection was cancelled");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during scheduled Universe data collection");
            throw;
        }
    }

    /// <summary>
    /// Scheduled collection of Market data
    /// Runs every 15 minutes by default
    /// </summary>
    [TickerFunction(
        functionName: "MarketCollection",
        cronExpression: "*/15 * * * *")]
    public async Task CollectMarketDataAsync(
        TickerFunctionContext<string> tickerContext,
        CancellationToken cancellationToken)
    {
        Log.Information("Starting scheduled Market data collection");

        using var scope = _serviceProvider.CreateScope();
        var collector = scope.ServiceProvider.GetRequiredService<MarketDataCollector>();

        try
        {
            // Get regions from configuration
            var regionsConfig = _configuration.GetSection("Scheduling:MarketCollection:Regions").Get<int[]>();
            var regions = regionsConfig ?? new[]
            {
                10000002,  // The Forge (Jita)
                10000043,  // Domain (Amarr)
                10000032,  // Sinq Laison (Dodixie)
                10000030,  // Heimatar (Rens)
                10000042   // Metropolis (Hek)
            };

            Log.Information("Collecting market data for {Count} regions", regions.Length);
            await collector.CollectAllMarketDataAsync(regions, cancellationToken);

            Log.Information("Scheduled Market data collection completed successfully");
        }
        catch (TaskCanceledException)
        {
            Log.Information("Scheduled Market data collection was cancelled");
        }
        catch (OperationCanceledException)
        {
            Log.Information("Scheduled Market data collection was cancelled");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during scheduled Market data collection");
            throw;
        }
    }
}
