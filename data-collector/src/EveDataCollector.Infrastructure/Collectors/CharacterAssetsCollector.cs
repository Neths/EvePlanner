using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Core.Models.CharacterData;
using EveDataCollector.Infrastructure.ESI;
using Microsoft.Extensions.Logging;

namespace EveDataCollector.Infrastructure.Collectors;

/// <summary>
/// Collector for character assets (items, ships, etc.)
/// </summary>
public class CharacterAssetsCollector
{
    private readonly AuthenticatedEsiClient _esiClient;
    private readonly ICharacterDataRepository _repository;
    private readonly ILogger<CharacterAssetsCollector> _logger;

    public CharacterAssetsCollector(
        AuthenticatedEsiClient esiClient,
        ICharacterDataRepository repository,
        ILogger<CharacterAssetsCollector> logger)
    {
        _esiClient = esiClient;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Collect character assets
    /// </summary>
    public async Task CollectAsync(long characterId, int applicationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Collecting assets for character {CharacterId}", characterId);

        try
        {
            // Delete old assets (full refresh)
            await _repository.DeleteAssetsAsync(characterId, cancellationToken);

            var allAssets = new List<CharacterAsset>();
            int page = 1;

            // ESI returns assets paginated
            while (true)
            {
                _logger.LogDebug("Fetching assets page {Page} for character {CharacterId}", page, characterId);

                var pageAssets = await _esiClient.GetAsync<List<AssetResponse>>(
                    characterId,
                    applicationId,
                    $"/characters/{characterId}/assets/?page={page}",
                    cancellationToken);

                if (pageAssets == null || pageAssets.Count == 0)
                {
                    break;
                }

                var assets = pageAssets.Select(a => new CharacterAsset
                {
                    ItemId = a.ItemId,
                    CharacterId = characterId,
                    TypeId = a.TypeId,
                    LocationId = a.LocationId,
                    LocationType = a.LocationType,
                    LocationFlag = a.LocationFlag,
                    Quantity = a.Quantity,
                    IsSingleton = a.IsSingleton,
                    IsBlueprintCopy = a.IsBlueprintCopy
                }).ToList();

                allAssets.AddRange(assets);

                _logger.LogDebug("Fetched {Count} assets from page {Page}", assets.Count, page);

                // If we got less than 1000 items, we're on the last page
                if (pageAssets.Count < 1000)
                {
                    break;
                }

                page++;
            }

            if (allAssets.Count > 0)
            {
                await _repository.UpsertAssetsAsync(characterId, allAssets, cancellationToken);
                _logger.LogInformation("Saved {Count} assets for character {CharacterId}", allAssets.Count, characterId);
            }
            else
            {
                _logger.LogInformation("No assets found for character {CharacterId}", characterId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect assets for character {CharacterId}", characterId);
            throw;
        }
    }

    // ESI Response DTO
    private class AssetResponse
    {
        public long ItemId { get; set; }
        public int TypeId { get; set; }
        public long LocationId { get; set; }
        public string LocationType { get; set; } = string.Empty;
        public string LocationFlag { get; set; } = string.Empty;
        public long Quantity { get; set; }
        public bool IsSingleton { get; set; }
        public bool? IsBlueprintCopy { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("item_id")]
        public long ItemIdJson { set => ItemId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("type_id")]
        public int TypeIdJson { set => TypeId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("location_id")]
        public long LocationIdJson { set => LocationId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("location_type")]
        public string LocationTypeJson { set => LocationType = value; }

        [System.Text.Json.Serialization.JsonPropertyName("location_flag")]
        public string LocationFlagJson { set => LocationFlag = value; }

        [System.Text.Json.Serialization.JsonPropertyName("quantity")]
        public long QuantityJson { set => Quantity = value; }

        [System.Text.Json.Serialization.JsonPropertyName("is_singleton")]
        public bool IsSingletonJson { set => IsSingleton = value; }

        [System.Text.Json.Serialization.JsonPropertyName("is_blueprint_copy")]
        public bool? IsBlueprintCopyJson { set => IsBlueprintCopy = value; }
    }
}
