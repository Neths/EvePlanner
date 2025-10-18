namespace EveDataCollector.Core.Models.CharacterData;

/// <summary>
/// Represents a character's wallet journal entry
/// </summary>
public class CharacterWalletJournal
{
    public long Id { get; set; }
    public long CharacterId { get; set; }
    public DateTime Date { get; set; }
    public string RefType { get; set; } = string.Empty;
    public long? FirstPartyId { get; set; }
    public long? SecondPartyId { get; set; }
    public decimal Amount { get; set; }
    public decimal Balance { get; set; }
    public string? Reason { get; set; }
    public decimal? Tax { get; set; }
    public long? TaxReceiverId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Represents a character's market transaction
/// </summary>
public class CharacterWalletTransaction
{
    public long TransactionId { get; set; }
    public long CharacterId { get; set; }
    public DateTime Date { get; set; }
    public int TypeId { get; set; }
    public long LocationId { get; set; }
    public long Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public long ClientId { get; set; }
    public bool IsBuy { get; set; }
    public bool IsPersonal { get; set; }
    public long? JournalRefId { get; set; }
    public DateTime CreatedAt { get; set; }
}
