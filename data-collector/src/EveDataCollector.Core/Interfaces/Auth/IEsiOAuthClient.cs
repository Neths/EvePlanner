using EveDataCollector.Core.Models.Auth;

namespace EveDataCollector.Core.Interfaces.Auth;

/// <summary>
/// Interface for ESI OAuth operations
/// </summary>
public interface IEsiOAuthClient
{
    /// <summary>
    /// Generate the authorization URL for OAuth flow
    /// </summary>
    string GetAuthorizationUrl(string clientId, string redirectUri, string[] scopes, string state);

    /// <summary>
    /// Exchange authorization code for access token
    /// </summary>
    Task<EsiToken> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        string clientId,
        string clientSecret,
        string redirectUri,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh an expired access token
    /// </summary>
    Task<EsiToken> RefreshTokenAsync(
        EsiToken token,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify and decode a JWT access token
    /// </summary>
    Task<TokenVerificationResult> VerifyTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of token verification
/// </summary>
public class TokenVerificationResult
{
    public bool IsValid { get; set; }
    public long CharacterId { get; set; }
    public string CharacterName { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public DateTime ExpiresAt { get; set; }
    public string? Error { get; set; }
}
