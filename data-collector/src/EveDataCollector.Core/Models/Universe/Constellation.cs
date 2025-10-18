namespace EveDataCollector.Core.Models.Universe;

/// <summary>
/// Represents an EVE Online constellation
/// </summary>
public class Constellation
{
    public int ConstellationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RegionId { get; set; }
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public double? PositionZ { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
