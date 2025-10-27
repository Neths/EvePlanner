using EveDataCollector.Infrastructure.Collectors;
using Microsoft.AspNetCore.Mvc;

namespace EveDataCollector.App.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CollectorController : ControllerBase
{
    private readonly UniverseCollector _universeCollector;
    private readonly MarketDataCollector _marketDataCollector;
    private readonly CharacterDataCollector _characterDataCollector;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CollectorController> _logger;

    public CollectorController(
        UniverseCollector universeCollector,
        MarketDataCollector marketDataCollector,
        CharacterDataCollector characterDataCollector,
        IConfiguration configuration,
        ILogger<CollectorController> logger)
    {
        _universeCollector = universeCollector;
        _marketDataCollector = marketDataCollector;
        _characterDataCollector = characterDataCollector;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Trigger Universe data collection manually
    /// </summary>
    [HttpPost("universe")]
    public async Task<IActionResult> CollectUniverse()
    {
        _logger.LogInformation("Manual Universe data collection triggered");

        try
        {
            await _universeCollector.CollectAllAsync();
            return Ok(new { message = "Universe data collection completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual Universe data collection");
            return StatusCode(500, new { error = "Universe data collection failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Trigger Market data collection manually
    /// </summary>
    [HttpPost("market")]
    public async Task<IActionResult> CollectMarket([FromQuery] int[]? regions = null)
    {
        _logger.LogInformation("Manual Market data collection triggered");

        try
        {
            var targetRegions = regions ?? _configuration.GetSection("Scheduling:MarketCollection:Regions").Get<int[]>() ?? new[]
            {
                10000002,  // The Forge (Jita)
                10000043,  // Domain (Amarr)
                10000032,  // Sinq Laison (Dodixie)
                10000030,  // Heimatar (Rens)
                10000042   // Metropolis (Hek)
            };

            await _marketDataCollector.CollectAllMarketDataAsync(targetRegions);
            return Ok(new { message = "Market data collection completed successfully", regions = targetRegions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual Market data collection");
            return StatusCode(500, new { error = "Market data collection failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Trigger Market prices collection only
    /// </summary>
    [HttpPost("market/prices")]
    public async Task<IActionResult> CollectMarketPrices()
    {
        _logger.LogInformation("Manual Market prices collection triggered");

        try
        {
            await _marketDataCollector.CollectMarketPricesAsync();
            return Ok(new { message = "Market prices collection completed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual Market prices collection");
            return StatusCode(500, new { error = "Market prices collection failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Trigger Market orders collection for specific regions
    /// </summary>
    [HttpPost("market/orders")]
    public async Task<IActionResult> CollectMarketOrders([FromQuery] int[] regions)
    {
        if (regions == null || regions.Length == 0)
        {
            return BadRequest(new { error = "At least one region ID is required" });
        }

        _logger.LogInformation("Manual Market orders collection triggered for {Count} region(s)", regions.Length);

        try
        {
            await _marketDataCollector.CollectMarketOrdersAsync(regions);
            return Ok(new { message = "Market orders collection completed successfully", regions });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual Market orders collection");
            return StatusCode(500, new { error = "Market orders collection failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Trigger Character data collection for a specific character
    /// </summary>
    [HttpPost("character/{characterId}/{applicationId}")]
    public async Task<IActionResult> CollectCharacter(int characterId, int applicationId)
    {
        _logger.LogInformation("Manual Character data collection triggered for character {CharacterId}", characterId);

        try
        {
            await _characterDataCollector.CollectAllAsync(characterId, applicationId);
            return Ok(new { message = "Character data collection completed successfully", characterId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual Character data collection for {CharacterId}", characterId);
            return StatusCode(500, new { error = "Character data collection failed", details = ex.Message });
        }
    }
}
