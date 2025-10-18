using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Core.Models.CharacterData;
using EveDataCollector.Infrastructure.ESI;
using Microsoft.Extensions.Logging;

namespace EveDataCollector.Infrastructure.Collectors;

/// <summary>
/// Collector for character skills and skill queue
/// </summary>
public class CharacterSkillsCollector
{
    private readonly AuthenticatedEsiClient _esiClient;
    private readonly ICharacterDataRepository _repository;
    private readonly ILogger<CharacterSkillsCollector> _logger;

    public CharacterSkillsCollector(
        AuthenticatedEsiClient esiClient,
        ICharacterDataRepository repository,
        ILogger<CharacterSkillsCollector> logger)
    {
        _esiClient = esiClient;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Collect character skills and skill queue
    /// </summary>
    public async Task CollectAsync(long characterId, int applicationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Collecting skills for character {CharacterId}", characterId);

        try
        {
            // Collect skills
            await CollectSkillsAsync(characterId, applicationId, cancellationToken);

            // Collect skill queue
            await CollectSkillQueueAsync(characterId, applicationId, cancellationToken);

            _logger.LogInformation("Skills collection completed for character {CharacterId}", characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect skills for character {CharacterId}", characterId);
            throw;
        }
    }

    private async Task CollectSkillsAsync(long characterId, int applicationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching skills for character {CharacterId}", characterId);

        var response = await _esiClient.GetAsync<SkillsResponse>(
            characterId,
            applicationId,
            $"/characters/{characterId}/skills/",
            cancellationToken);

        if (response == null || response.Skills == null)
        {
            _logger.LogWarning("No skills data returned for character {CharacterId}", characterId);
            return;
        }

        var skills = response.Skills.Select(s => new CharacterSkill
        {
            CharacterId = characterId,
            SkillId = s.SkillId,
            ActiveSkillLevel = s.ActiveSkillLevel,
            TrainedSkillLevel = s.TrainedSkillLevel,
            SkillpointsInSkill = s.SkillpointsInSkill
        }).ToList();

        await _repository.UpsertSkillsAsync(characterId, skills, cancellationToken);

        _logger.LogInformation("Saved {Count} skills for character {CharacterId}", skills.Count, characterId);
    }

    private async Task CollectSkillQueueAsync(long characterId, int applicationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching skill queue for character {CharacterId}", characterId);

        var response = await _esiClient.GetAsync<List<SkillQueueItemResponse>>(
            characterId,
            applicationId,
            $"/characters/{characterId}/skillqueue/",
            cancellationToken);

        if (response == null)
        {
            _logger.LogWarning("No skill queue data returned for character {CharacterId}", characterId);
            return;
        }

        var queue = response.Select(q => new CharacterSkillQueueItem
        {
            CharacterId = characterId,
            SkillId = q.SkillId,
            QueuePosition = q.QueuePosition,
            FinishedLevel = q.FinishedLevel,
            StartDate = q.StartDate,
            FinishDate = q.FinishDate,
            TrainingStartSp = q.TrainingStartSp,
            LevelStartSp = q.LevelStartSp,
            LevelEndSp = q.LevelEndSp
        }).ToList();

        await _repository.UpsertSkillQueueAsync(characterId, queue, cancellationToken);

        _logger.LogInformation("Saved {Count} skill queue items for character {CharacterId}", queue.Count, characterId);
    }

    // ESI Response DTOs
    private class SkillsResponse
    {
        public List<SkillResponse> Skills { get; set; } = new();
        public long TotalSp { get; set; }
        public long? UnallocatedSp { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("skills")]
        public List<SkillResponse> SkillsJson { set => Skills = value; }

        [System.Text.Json.Serialization.JsonPropertyName("total_sp")]
        public long TotalSpJson { set => TotalSp = value; }

        [System.Text.Json.Serialization.JsonPropertyName("unallocated_sp")]
        public long? UnallocatedSpJson { set => UnallocatedSp = value; }
    }

    private class SkillResponse
    {
        public int SkillId { get; set; }
        public int ActiveSkillLevel { get; set; }
        public int TrainedSkillLevel { get; set; }
        public long SkillpointsInSkill { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("skill_id")]
        public int SkillIdJson { set => SkillId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("active_skill_level")]
        public int ActiveSkillLevelJson { set => ActiveSkillLevel = value; }

        [System.Text.Json.Serialization.JsonPropertyName("trained_skill_level")]
        public int TrainedSkillLevelJson { set => TrainedSkillLevel = value; }

        [System.Text.Json.Serialization.JsonPropertyName("skillpoints_in_skill")]
        public long SkillpointsInSkillJson { set => SkillpointsInSkill = value; }
    }

    private class SkillQueueItemResponse
    {
        public int SkillId { get; set; }
        public int QueuePosition { get; set; }
        public int FinishedLevel { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? FinishDate { get; set; }
        public long? TrainingStartSp { get; set; }
        public long? LevelStartSp { get; set; }
        public long? LevelEndSp { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("skill_id")]
        public int SkillIdJson { set => SkillId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("queue_position")]
        public int QueuePositionJson { set => QueuePosition = value; }

        [System.Text.Json.Serialization.JsonPropertyName("finished_level")]
        public int FinishedLevelJson { set => FinishedLevel = value; }

        [System.Text.Json.Serialization.JsonPropertyName("start_date")]
        public DateTime? StartDateJson { set => StartDate = value; }

        [System.Text.Json.Serialization.JsonPropertyName("finish_date")]
        public DateTime? FinishDateJson { set => FinishDate = value; }

        [System.Text.Json.Serialization.JsonPropertyName("training_start_sp")]
        public long? TrainingStartSpJson { set => TrainingStartSp = value; }

        [System.Text.Json.Serialization.JsonPropertyName("level_start_sp")]
        public long? LevelStartSpJson { set => LevelStartSp = value; }

        [System.Text.Json.Serialization.JsonPropertyName("level_end_sp")]
        public long? LevelEndSpJson { set => LevelEndSp = value; }
    }
}
