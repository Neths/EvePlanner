namespace EveDataCollector.Core.Models.CharacterData;

/// <summary>
/// Represents a character asset (item, ship, etc.)
/// </summary>
public class CharacterAsset
{
    public long ItemId { get; set; }
    public long CharacterId { get; set; }
    public int TypeId { get; set; }
    public long LocationId { get; set; }
    public string LocationType { get; set; } = string.Empty;
    public string LocationFlag { get; set; } = string.Empty;
    public long Quantity { get; set; }
    public bool IsSingleton { get; set; }
    public bool? IsBlueprintCopy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
