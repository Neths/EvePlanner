namespace EveDataCollector.Core.Models.CharacterData;

/// <summary>
/// Represents a trained character skill
/// </summary>
public class CharacterSkill
{
    public long CharacterId { get; set; }
    public int SkillId { get; set; }
    public int ActiveSkillLevel { get; set; }
    public int TrainedSkillLevel { get; set; }
    public long SkillpointsInSkill { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Represents an item in the character's skill training queue
/// </summary>
public class CharacterSkillQueueItem
{
    public int Id { get; set; }
    public long CharacterId { get; set; }
    public int SkillId { get; set; }
    public int QueuePosition { get; set; }
    public int FinishedLevel { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? FinishDate { get; set; }
    public long? TrainingStartSp { get; set; }
    public long? LevelStartSp { get; set; }
    public long? LevelEndSp { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
