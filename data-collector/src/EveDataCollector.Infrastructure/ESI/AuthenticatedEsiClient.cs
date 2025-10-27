using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EveDataCollector.Core.Interfaces.Auth;
using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Core.Models.Auth;
using Microsoft.Extensions.Logging;

namespace EveDataCollector.Infrastructure.ESI;

/// <summary>
/// ESI client that uses authenticated character tokens
/// </summary>
public class AuthenticatedEsiClient
{
    private readonly HttpClient _httpClient;
    private readonly IAuthRepository _authRepository;
    private readonly IEsiOAuthClient _oauthClient;
    private readonly ILogger<AuthenticatedEsiClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuthenticatedEsiClient(
        HttpClient httpClient,
        IAuthRepository authRepository,
        IEsiOAuthClient oauthClient,
        ILogger<AuthenticatedEsiClient> logger)
    {
        _httpClient = httpClient;
        _authRepository = authRepository;
        _oauthClient = oauthClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }

    /// <summary>
    /// Make an authenticated GET request to ESI
    /// </summary>
    public async Task<T?> GetAsync<T>(
        long characterId,
        int applicationId,
        string path,
        CancellationToken cancellationToken = default)
    {
        var token = await GetValidTokenAsync(characterId, applicationId, cancellationToken);
        if (token == null)
        {
            throw new InvalidOperationException($"No valid token found for character {characterId}");
        }

        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        _logger.LogDebug("Authenticated GET request to {Path} for character {CharacterId}", path, characterId);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
    }

    /// <summary>
    /// Make an authenticated POST request to ESI
    /// </summary>
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        long characterId,
        int applicationId,
        string path,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        var token = await GetValidTokenAsync(characterId, applicationId, cancellationToken);
        if (token == null)
        {
            throw new InvalidOperationException($"No valid token found for character {characterId}");
        }

        var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        request.Content = JsonContent.Create(body, options: _jsonOptions);

        _logger.LogDebug("Authenticated POST request to {Path} for character {CharacterId}", path, characterId);

        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions, cancellationToken);
    }

    /// <summary>
    /// Get a valid token for a character, refreshing if necessary
    /// </summary>
    private async Task<EsiToken?> GetValidTokenAsync(
        long characterId,
        int applicationId,
        CancellationToken cancellationToken)
    {
        var token = await _authRepository.GetTokenAsync(applicationId, characterId, cancellationToken);
        if (token == null)
        {
            _logger.LogWarning("No token found for character {CharacterId}", characterId);
            return null;
        }

        if (!token.IsValid)
        {
            _logger.LogWarning("Token for character {CharacterId} is marked as invalid", characterId);
            return null;
        }

        // Refresh if expired or about to expire
        if (token.NeedsRefresh)
        {
            _logger.LogInformation("Token expired for character {CharacterId}, refreshing...", characterId);

            var application = await _authRepository.GetApplicationByIdAsync(applicationId, cancellationToken);
            if (application == null)
            {
                _logger.LogError("Application {ApplicationId} not found", applicationId);
                return null;
            }

            try
            {
                token = await _oauthClient.RefreshTokenAsync(
                    token,
                    application.ClientId,
                    application.ClientSecret,
                    cancellationToken);

                await _authRepository.UpsertTokenAsync(token, cancellationToken);
                _logger.LogInformation("Token refreshed successfully for character {CharacterId}", characterId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh token for character {CharacterId}", characterId);
                await _authRepository.InvalidateTokenAsync(token.Id, cancellationToken);
                return null;
            }
        }

        return token;
    }
}
