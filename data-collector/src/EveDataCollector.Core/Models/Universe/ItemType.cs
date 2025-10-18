namespace EveDataCollector.Core.Models.Universe;

/// <summary>
/// Represents an EVE Online item type
/// </summary>
public class ItemType
{
    public int TypeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int GroupId { get; set; }
    public int? MarketGroupId { get; set; }
    public double? Volume { get; set; }
    public double? Capacity { get; set; }
    public double? PackagedVolume { get; set; }
    public double? Mass { get; set; }
    public int? PortionSize { get; set; }
    public double? Radius { get; set; }
    public bool Published { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
