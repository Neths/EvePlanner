namespace EveDataCollector.Core.Models.Universe;

/// <summary>
/// Represents an EVE Online solar system
/// </summary>
public class SolarSystem
{
    public int SystemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ConstellationId { get; set; }
    public double? PositionX { get; set; }
    public double? PositionY { get; set; }
    public double? PositionZ { get; set; }
    public double SecurityStatus { get; set; }
    public string? SecurityClass { get; set; }
    public int? StarId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
