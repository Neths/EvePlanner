namespace EveDataCollector.Core.Models.Market;

/// <summary>
/// Represents historical market data for a specific day
/// </summary>
public class MarketHistory
{
    public int TypeId { get; set; }
    public int RegionId { get; set; }
    public DateTime Date { get; set; }
    public decimal Average { get; set; }
    public decimal Highest { get; set; }
    public decimal Lowest { get; set; }
    public long Volume { get; set; }
    public long OrderCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
