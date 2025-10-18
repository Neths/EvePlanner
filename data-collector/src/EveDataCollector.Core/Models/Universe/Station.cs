namespace EveDataCollector.Core.Models.Universe;

/// <summary>
/// Represents an EVE Online NPC station
/// </summary>
public class Station
{
    public int StationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SystemId { get; set; }
    public int TypeId { get; set; }
    public int? Owner { get; set; }
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public double? PositionZ { get; set; }
    public int? RaceId { get; set; }
    public string[]? Services { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
