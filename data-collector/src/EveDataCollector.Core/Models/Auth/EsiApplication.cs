namespace EveDataCollector.Core.Models.Auth;

/// <summary>
/// Represents an ESI OAuth application
/// </summary>
public class EsiApplication
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
