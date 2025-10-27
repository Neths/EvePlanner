using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using EveDataCollector.Core.Interfaces.Auth;
using EveDataCollector.Core.Models.Auth;
using Microsoft.Extensions.Logging;

namespace EveDataCollector.Infrastructure.Auth;

/// <summary>
/// ESI OAuth2 client implementation
/// </summary>
public class EsiOAuthClient : IEsiOAuthClient
{
    private const string AuthorizationEndpoint = "https://login.eveonline.com/v2/oauth/authorize";
    private const string TokenEndpoint = "https://login.eveonline.com/v2/oauth/token";
    private const string JwksEndpoint = "https://login.eveonline.com/oauth/jwks";

    private readonly HttpClient _httpClient;
    private readonly ILogger<EsiOAuthClient> _logger;

    public EsiOAuthClient(HttpClient httpClient, ILogger<EsiOAuthClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public string GetAuthorizationUrl(string clientId, string redirectUri, string[] scopes, string state)
    {
        var scopeString = string.Join(" ", scopes);
        var queryParams = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["redirect_uri"] = redirectUri,
            ["client_id"] = clientId,
            ["scope"] = scopeString,
            ["state"] = state
        };

        var queryString = string.Join("&",
            queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return $"{AuthorizationEndpoint}?{queryString}";
    }

    public async Task<EsiToken> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        string clientId,
        string clientSecret,
        string redirectUri,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Exchanging authorization code for access token");

        var requestBody = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = authorizationCode,
            ["redirect_uri"] = redirectUri
        };

        var token = await RequestTokenAsync(clientId, clientSecret, requestBody, cancellationToken);

        _logger.LogInformation("Successfully exchanged authorization code for token");
        return token;
    }

    public async Task<EsiToken> RefreshTokenAsync(
        EsiToken token,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refreshing access token for character {CharacterId}", token.CharacterId);

        var requestBody = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = token.RefreshToken
        };

        try
        {
            var newToken = await RequestTokenAsync(clientId, clientSecret, requestBody, cancellationToken);

            // Preserve database identifiers
            newToken.Id = token.Id;
            newToken.ApplicationId = token.ApplicationId;
            newToken.CharacterId = token.CharacterId;
            newToken.LastRefreshedAt = DateTime.UtcNow;

            _logger.LogInformation("Successfully refreshed token for character {CharacterId}", token.CharacterId);
            return newToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh token for character {CharacterId}", token.CharacterId);
            throw;
        }
    }

    public async Task<TokenVerificationResult> VerifyTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Decode JWT (simplified - in production, use proper JWT validation library)
            var parts = accessToken.Split('.');
            if (parts.Length != 3)
            {
                return new TokenVerificationResult
                {
                    IsValid = false,
                    Error = "Invalid JWT format"
                };
            }

            var payload = DecodeBase64Url(parts[1]);
            var claims = JsonSerializer.Deserialize<JsonElement>(payload);

            var characterId = claims.GetProperty("sub").GetString()?.Split(':').Last();
            var characterName = claims.GetProperty("name").GetString();
            var exp = claims.GetProperty("exp").GetInt64();
            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;

            var scopes = new List<string>();
            if (claims.TryGetProperty("scp", out var scopesElement))
            {
                if (scopesElement.ValueKind == JsonValueKind.Array)
                {
                    scopes = scopesElement.EnumerateArray()
                        .Select(s => s.GetString() ?? string.Empty)
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                }
                else if (scopesElement.ValueKind == JsonValueKind.String)
                {
                    var scopeString = scopesElement.GetString();
                    if (!string.IsNullOrEmpty(scopeString))
                    {
                        scopes = scopeString.Split(' ').ToList();
                    }
                }
            }

            return new TokenVerificationResult
            {
                IsValid = true,
                CharacterId = long.Parse(characterId ?? "0"),
                CharacterName = characterName ?? string.Empty,
                Scopes = scopes.ToArray(),
                ExpiresAt = expiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify access token");
            return new TokenVerificationResult
            {
                IsValid = false,
                Error = ex.Message
            };
        }
    }

    private async Task<EsiToken> RequestTokenAsync(
        string clientId,
        string clientSecret,
        Dictionary<string, string> requestBody,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint);

        // Basic authentication
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        // Form-encoded body
        request.Content = new FormUrlEncodedContent(requestBody);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Token request failed: {StatusCode} - {Body}", response.StatusCode, responseBody);
            throw new HttpRequestException($"Token request failed: {response.StatusCode} - {responseBody}");
        }

        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseBody)
            ?? throw new InvalidOperationException("Failed to deserialize token response");

        var expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        return new EsiToken
        {
            AccessToken = tokenResponse.AccessToken,
            RefreshToken = tokenResponse.RefreshToken,
            TokenType = tokenResponse.TokenType,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private static string DecodeBase64Url(string input)
    {
        var base64 = input.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        var bytes = Convert.FromBase64String(base64);
        return Encoding.UTF8.GetString(bytes);
    }

    private class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Bearer";
        public int ExpiresIn { get; set; }

        // JSON property names from ESI
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string AccessTokenJson { set => AccessToken = value; }

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string RefreshTokenJson { set => RefreshToken = value; }

        [System.Text.Json.Serialization.JsonPropertyName("token_type")]
        public string TokenTypeJson { set => TokenType = value; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresInJson { set => ExpiresIn = value; }
    }
}
