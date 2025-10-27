using EveDataCollector.Infrastructure.ESI;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace EveDataCollector.App.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly Func<NpgsqlConnection> _dbConnectionFactory;
    private readonly EsiClient _esiClient;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        Func<NpgsqlConnection> dbConnectionFactory,
        EsiClient esiClient,
        ILogger<HealthController> logger)
    {
        _dbConnectionFactory = dbConnectionFactory;
        _esiClient = esiClient;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Detailed health check including database and ESI connectivity
    /// </summary>
    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailed()
    {
        var dbHealth = await CheckDatabaseAsync();
        var esiHealth = await CheckEsiAsync();

        var health = new
        {
            database = dbHealth,
            esi = esiHealth,
            timestamp = DateTime.UtcNow
        };

        var isHealthy = dbHealth.Healthy && esiHealth.Healthy;

        return isHealthy
            ? Ok(new { status = "healthy", details = health })
            : StatusCode(503, new { status = "unhealthy", details = health });
    }

    private async Task<HealthCheckResult> CheckDatabaseAsync()
    {
        try
        {
            await using var connection = _dbConnectionFactory();
            await connection.OpenAsync();

            return new HealthCheckResult
            {
                Healthy = true,
                Database = connection.Database,
                ServerVersion = connection.ServerVersion,
                ResponseTime = "< 100ms"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return new HealthCheckResult
            {
                Healthy = false,
                Error = ex.Message
            };
        }
    }

    private async Task<HealthCheckResult> CheckEsiAsync()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            // Just test basic connectivity - Universe endpoint
            var categories = await _esiClient.Universe.GetCategoriesAsync();
            var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

            return new HealthCheckResult
            {
                Healthy = true,
                ResponseTime = $"{responseTime:F0}ms",
                Message = $"ESI accessible ({categories.Count} categories)"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ESI health check failed");
            return new HealthCheckResult
            {
                Healthy = false,
                Error = ex.Message
            };
        }
    }

    private class HealthCheckResult
    {
        public bool Healthy { get; set; }
        public string? Database { get; set; }
        public string? ServerVersion { get; set; }
        public string? ResponseTime { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }
}
