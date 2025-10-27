using EveDataCollector.Core.Interfaces.Auth;
using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Core.Models;
using EveDataCollector.Core.Models.Auth;
using EveDataCollector.Infrastructure.ESI;
using Microsoft.Extensions.Logging;

namespace EveDataCollector.Infrastructure.Auth;

/// <summary>
/// Service for managing character authentication
/// </summary>
public class CharacterAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IEsiOAuthClient _oauthClient;
    private readonly EsiClient _esiClient;
    private readonly ILogger<CharacterAuthService> _logger;
    private readonly ILoggerFactory _loggerFactory;

    public CharacterAuthService(
        IAuthRepository authRepository,
        IEsiOAuthClient oauthClient,
        EsiClient esiClient,
        ILogger<CharacterAuthService> logger,
        ILoggerFactory loggerFactory)
    {
        _authRepository = authRepository;
        _oauthClient = oauthClient;
        _esiClient = esiClient;
        _logger = logger;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Register an OAuth application
    /// </summary>
    public async Task<EsiApplication> RegisterApplicationAsync(
        string name,
        string clientId,
        string clientSecret,
        string callbackUrl,
        string[] scopes,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Registering OAuth application: {Name}", name);

        var application = new EsiApplication
        {
            Name = name,
            ClientId = clientId,
            ClientSecret = clientSecret,
            CallbackUrl = callbackUrl,
            Scopes = scopes,
            IsActive = true
        };

        var id = await _authRepository.UpsertApplicationAsync(application, cancellationToken);
        application.Id = id;

        _logger.LogInformation("Application registered with ID: {Id}", id);
        return application;
    }

    /// <summary>
    /// Authorize a new character (interactive OAuth flow)
    /// </summary>
    public async Task<(Character Character, EsiToken Token)?> AuthorizeCharacterAsync(
        EsiApplication application,
        string[] scopes,
        CancellationToken cancellationToken = default)
    {
        var callbackServer = new OAuthCallbackServer(_oauthClient, _authRepository,
            _loggerFactory.CreateLogger<OAuthCallbackServer>());

        var token = await callbackServer.AuthorizeCharacterAsync(application, scopes, cancellationToken);
        if (token == null)
        {
            _logger.LogWarning("Character authorization failed or was cancelled");
            return null;
        }

        // Fetch character details from ESI
        var character = await FetchCharacterDetailsAsync(token.CharacterId, cancellationToken);
        if (character == null)
        {
            _logger.LogError("Failed to fetch character details for {CharacterId}", token.CharacterId);
            return null;
        }

        // Save character to database
        await _authRepository.UpsertCharacterAsync(character, cancellationToken);

        _logger.LogInformation("Character {CharacterName} ({CharacterId}) successfully authorized",
            character.CharacterName, character.CharacterId);

        return (character, token);
    }

    /// <summary>
    /// Fetch character details from ESI public endpoint
    /// </summary>
    private async Task<Character?> FetchCharacterDetailsAsync(
        long characterId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching character details for {CharacterId}", characterId);

            var characterInfo = await _esiClient.GetAsync<CharacterPublicInfo>(
                $"/characters/{characterId}/",
                cancellationToken);

            if (characterInfo == null)
            {
                _logger.LogError("ESI returned null for character {CharacterId}", characterId);
                return null;
            }

            var character = new Character
            {
                CharacterId = characterId,
                CharacterName = characterInfo.Name,
                CorporationId = characterInfo.CorporationId,
                AllianceId = characterInfo.AllianceId,
                FactionId = characterInfo.FactionId,
                Birthday = characterInfo.Birthday,
                Gender = characterInfo.Gender,
                RaceId = characterInfo.RaceId,
                BloodlineId = characterInfo.BloodlineId,
                AncestryId = characterInfo.AncestryId,
                SecurityStatus = characterInfo.SecurityStatus,
                Description = characterInfo.Description
            };

            // Fetch corporation name if available
            if (characterInfo.CorporationId > 0)
            {
                try
                {
                    var corpInfo = await _esiClient.GetAsync<CorporationPublicInfo>(
                        $"/corporations/{characterInfo.CorporationId}/",
                        cancellationToken);
                    character.CorporationName = corpInfo?.Name;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch corporation name for {CorporationId}",
                        characterInfo.CorporationId);
                }
            }

            // Fetch alliance name if available
            if (characterInfo.AllianceId.HasValue && characterInfo.AllianceId > 0)
            {
                try
                {
                    var allianceInfo = await _esiClient.GetAsync<AlliancePublicInfo>(
                        $"/alliances/{characterInfo.AllianceId}/",
                        cancellationToken);
                    character.AllianceName = allianceInfo?.Name;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch alliance name for {AllianceId}",
                        characterInfo.AllianceId);
                }
            }

            return character;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching character details for {CharacterId}", characterId);
            return null;
        }
    }

    // DTOs for ESI responses
    private class CharacterPublicInfo
    {
        public string Name { get; set; } = string.Empty;
        public long CorporationId { get; set; }
        public long? AllianceId { get; set; }
        public int? FactionId { get; set; }
        public DateTime? Birthday { get; set; }
        public string? Gender { get; set; }
        public int? RaceId { get; set; }
        public int? BloodlineId { get; set; }
        public int? AncestryId { get; set; }
        public decimal? SecurityStatus { get; set; }
        public string? Description { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string NameJson { set => Name = value; }

        [System.Text.Json.Serialization.JsonPropertyName("corporation_id")]
        public long CorporationIdJson { set => CorporationId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("alliance_id")]
        public long? AllianceIdJson { set => AllianceId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("faction_id")]
        public int? FactionIdJson { set => FactionId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("birthday")]
        public DateTime? BirthdayJson { set => Birthday = value; }

        [System.Text.Json.Serialization.JsonPropertyName("gender")]
        public string? GenderJson { set => Gender = value; }

        [System.Text.Json.Serialization.JsonPropertyName("race_id")]
        public int? RaceIdJson { set => RaceId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("bloodline_id")]
        public int? BloodlineIdJson { set => BloodlineId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("ancestry_id")]
        public int? AncestryIdJson { set => AncestryId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("security_status")]
        public decimal? SecurityStatusJson { set => SecurityStatus = value; }

        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string? DescriptionJson { set => Description = value; }
    }

    private class CorporationPublicInfo
    {
        public string Name { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string NameJson { set => Name = value; }
    }

    private class AlliancePublicInfo
    {
        public string Name { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string NameJson { set => Name = value; }
    }
}
