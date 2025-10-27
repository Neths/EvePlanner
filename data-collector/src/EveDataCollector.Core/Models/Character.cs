namespace EveDataCollector.Core.Models;

/// <summary>
/// Represents an EVE Online character
/// </summary>
public class Character
{
    public long CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public long? CorporationId { get; set; }
    public string? CorporationName { get; set; }
    public long? AllianceId { get; set; }
    public string? AllianceName { get; set; }
    public int? FactionId { get; set; }
    public DateTime? Birthday { get; set; }
    public string? Gender { get; set; }
    public int? RaceId { get; set; }
    public int? BloodlineId { get; set; }
    public int? AncestryId { get; set; }
    public decimal? SecurityStatus { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
