namespace EveDataCollector.Core.Models.Universe;

/// <summary>
/// Represents an EVE Online item group
/// </summary>
public class Group
{
    public int GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public bool Published { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
