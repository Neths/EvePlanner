namespace EveDataCollector.Core.Models.Universe;

/// <summary>
/// Represents an EVE Online region
/// </summary>
public class Region
{
    public int RegionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
