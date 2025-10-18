namespace EveDataCollector.Core.Models.Market;

/// <summary>
/// Represents global market prices for an item type
/// </summary>
public class MarketPrice
{
    public int TypeId { get; set; }
    public decimal? AdjustedPrice { get; set; }
    public decimal? AveragePrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
