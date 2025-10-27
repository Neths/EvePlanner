using Microsoft.Extensions.Logging;

namespace EveDataCollector.Infrastructure.Collectors;

/// <summary>
/// Orchestrator for collecting all character data
/// </summary>
public class CharacterDataCollector
{
    private readonly CharacterSkillsCollector _skillsCollector;
    private readonly CharacterAssetsCollector _assetsCollector;
    private readonly CharacterWalletCollector _walletCollector;
    private readonly ILogger<CharacterDataCollector> _logger;

    public CharacterDataCollector(
        CharacterSkillsCollector skillsCollector,
        CharacterAssetsCollector assetsCollector,
        CharacterWalletCollector walletCollector,
        ILogger<CharacterDataCollector> logger)
    {
        _skillsCollector = skillsCollector;
        _assetsCollector = assetsCollector;
        _walletCollector = walletCollector;
        _logger = logger;
    }

    /// <summary>
    /// Collect all character data (skills, assets, wallet)
    /// </summary>
    public async Task CollectAllAsync(
        long characterId,
        int applicationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("=== Starting character data collection for {CharacterId} ===", characterId);

        var startTime = DateTime.UtcNow;
        var errors = new List<string>();

        try
        {
            // Collect skills
            try
            {
                await _skillsCollector.CollectAsync(characterId, applicationId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect skills");
                errors.Add($"Skills: {ex.Message}");
            }

            // Collect assets
            try
            {
                await _assetsCollector.CollectAsync(characterId, applicationId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect assets");
                errors.Add($"Assets: {ex.Message}");
            }

            // Collect wallet
            try
            {
                await _walletCollector.CollectAsync(characterId, applicationId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect wallet data");
                errors.Add($"Wallet: {ex.Message}");
            }

            var duration = DateTime.UtcNow - startTime;

            if (errors.Count == 0)
            {
                _logger.LogInformation("=== Character data collection completed successfully in {Duration:F2}s ===",
                    duration.TotalSeconds);
            }
            else
            {
                _logger.LogWarning("=== Character data collection completed with {ErrorCount} error(s) in {Duration:F2}s ===",
                    errors.Count, duration.TotalSeconds);
                foreach (var error in errors)
                {
                    _logger.LogWarning("  - {Error}", error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Character data collection failed");
            throw;
        }
    }
}
