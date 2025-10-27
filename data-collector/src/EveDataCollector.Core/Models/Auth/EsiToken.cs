namespace EveDataCollector.Core.Models.Auth;

/// <summary>
/// Represents an ESI OAuth token for a character
/// </summary>
public class EsiToken
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public long CharacterId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAt { get; set; }
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public bool IsValid { get; set; } = true;
    public DateTime? LastRefreshedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Check if the access token is expired or about to expire (within 60 seconds)
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt.AddSeconds(-60);

    /// <summary>
    /// Check if the token needs refresh
    /// </summary>
    public bool NeedsRefresh => IsValid && IsExpired;
}
