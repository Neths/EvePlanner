using EveDataCollector.Core.Interfaces.Auth;
using EveDataCollector.Core.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EveDataCollector.Shared.Auth;

/// <summary>
/// Background service that automatically refreshes expired ESI tokens
/// </summary>
public class TokenRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenRefreshService> _logger;

    public TokenRefreshService(
        IServiceProvider serviceProvider,
        ILogger<TokenRefreshService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Token Refresh Service is starting...");
        return base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token Refresh Service is running");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RefreshExpiredTokensAsync(stoppingToken);

                // Check every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in token refresh loop");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        _logger.LogInformation("Token Refresh Service is stopping");
    }

    private async Task RefreshExpiredTokensAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var authRepository = scope.ServiceProvider.GetRequiredService<IAuthRepository>();
        var oauthClient = scope.ServiceProvider.GetRequiredService<IEsiOAuthClient>();

        var expiredTokens = await authRepository.GetExpiredTokensAsync(cancellationToken);

        if (expiredTokens.Count == 0)
        {
            _logger.LogDebug("No expired tokens to refresh");
            return;
        }

        _logger.LogInformation("Found {Count} expired token(s) to refresh", expiredTokens.Count);

        foreach (var token in expiredTokens)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                _logger.LogInformation("Refreshing token for character {CharacterId}", token.CharacterId);

                // Get the application for this token
                var application = await authRepository.GetApplicationByIdAsync(token.ApplicationId, cancellationToken);
                if (application == null)
                {
                    _logger.LogError("Application {ApplicationId} not found for token {TokenId}",
                        token.ApplicationId, token.Id);
                    await authRepository.InvalidateTokenAsync(token.Id, cancellationToken);
                    continue;
                }

                // Refresh the token
                var refreshedToken = await oauthClient.RefreshTokenAsync(
                    token,
                    application.ClientId,
                    application.ClientSecret,
                    cancellationToken);

                // Update in database
                await authRepository.UpsertTokenAsync(refreshedToken, cancellationToken);

                _logger.LogInformation("Successfully refreshed token for character {CharacterId}. Next expiry: {ExpiresAt:yyyy-MM-dd HH:mm:ss} UTC",
                    token.CharacterId, refreshedToken.ExpiresAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh token for character {CharacterId}", token.CharacterId);

                // Mark token as invalid if refresh fails
                await authRepository.InvalidateTokenAsync(token.Id, cancellationToken);
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Token Refresh Service is stopping");
        return base.StopAsync(cancellationToken);
    }
}
