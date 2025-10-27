namespace EveDataCollector.Core.Models.Market;

/// <summary>
/// Represents a market order (buy or sell)
/// </summary>
public class MarketOrder
{
    public long OrderId { get; set; }
    public int TypeId { get; set; }
    public int RegionId { get; set; }
    public long LocationId { get; set; }
    public int SystemId { get; set; }
    public bool IsBuyOrder { get; set; }
    public decimal Price { get; set; }
    public int VolumeRemain { get; set; }
    public int VolumeTotal { get; set; }
    public int MinVolume { get; set; }
    public int Duration { get; set; }
    public DateTime Issued { get; set; }
    public string? Range { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
