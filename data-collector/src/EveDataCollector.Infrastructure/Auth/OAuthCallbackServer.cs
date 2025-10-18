using System.Net;
using System.Text;
using EveDataCollector.Core.Interfaces.Auth;
using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Core.Models.Auth;
using Microsoft.Extensions.Logging;

namespace EveDataCollector.Infrastructure.Auth;

/// <summary>
/// Simple HTTP server to handle OAuth callback
/// </summary>
public class OAuthCallbackServer
{
    private readonly IEsiOAuthClient _oauthClient;
    private readonly IAuthRepository _authRepository;
    private readonly ILogger<OAuthCallbackServer> _logger;
    private HttpListener? _listener;
    private string? _state;

    public OAuthCallbackServer(
        IEsiOAuthClient oauthClient,
        IAuthRepository authRepository,
        ILogger<OAuthCallbackServer> logger)
    {
        _oauthClient = oauthClient;
        _authRepository = authRepository;
        _logger = logger;
    }

    /// <summary>
    /// Start the OAuth flow for a character
    /// </summary>
    public async Task<EsiToken?> AuthorizeCharacterAsync(
        EsiApplication application,
        string[] scopes,
        CancellationToken cancellationToken = default)
    {
        _state = Guid.NewGuid().ToString();

        var authUrl = _oauthClient.GetAuthorizationUrl(
            application.ClientId,
            application.CallbackUrl,
            scopes,
            _state);

        _logger.LogInformation("=== EVE Online Character Authorization ===");
        _logger.LogInformation("");
        _logger.LogInformation("Please open the following URL in your browser:");
        _logger.LogInformation("");
        _logger.LogInformation("{AuthUrl}", authUrl);
        _logger.LogInformation("");
        _logger.LogInformation("Waiting for authorization callback...");

        // Start listening for callback
        var callbackTask = StartCallbackListenerAsync(application, cancellationToken);

        // Wait for callback with timeout
        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
        var completedTask = await Task.WhenAny(callbackTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            _logger.LogWarning("Authorization timeout after 5 minutes");
            StopListener();
            return null;
        }

        return await callbackTask;
    }

    private async Task<EsiToken?> StartCallbackListenerAsync(
        EsiApplication application,
        CancellationToken cancellationToken)
    {
        try
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(application.CallbackUrl.TrimEnd('/') + "/");
            _listener.Start();

            _logger.LogDebug("Callback listener started on {CallbackUrl}", application.CallbackUrl);

            var context = await _listener.GetContextAsync();
            var request = context.Request;
            var response = context.Response;

            try
            {
                // Extract query parameters
                var code = request.QueryString["code"];
                var state = request.QueryString["state"];
                var error = request.QueryString["error"];

                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogError("OAuth error: {Error}", error);
                    await SendHtmlResponseAsync(response, "Authorization Failed", $"Error: {error}", false);
                    return null;
                }

                if (string.IsNullOrEmpty(code))
                {
                    _logger.LogError("No authorization code received");
                    await SendHtmlResponseAsync(response, "Authorization Failed", "No authorization code received", false);
                    return null;
                }

                if (state != _state)
                {
                    _logger.LogError("State mismatch: expected {Expected}, got {Actual}", _state, state);
                    await SendHtmlResponseAsync(response, "Authorization Failed", "State mismatch - possible CSRF attack", false);
                    return null;
                }

                _logger.LogInformation("Received authorization code, exchanging for token...");

                // Exchange code for token
                var token = await _oauthClient.ExchangeAuthorizationCodeAsync(
                    code,
                    application.ClientId,
                    application.ClientSecret,
                    application.CallbackUrl,
                    cancellationToken);

                // Verify and get character info from token
                var verification = await _oauthClient.VerifyTokenAsync(token.AccessToken, cancellationToken);
                if (!verification.IsValid)
                {
                    _logger.LogError("Token verification failed: {Error}", verification.Error);
                    await SendHtmlResponseAsync(response, "Authorization Failed", $"Token verification failed: {verification.Error}", false);
                    return null;
                }

                token.ApplicationId = application.Id;
                token.CharacterId = verification.CharacterId;
                token.Scopes = verification.Scopes;

                // Save token to database
                await _authRepository.UpsertTokenAsync(token, cancellationToken);

                _logger.LogInformation("Successfully authorized character {CharacterName} ({CharacterId})",
                    verification.CharacterName, verification.CharacterId);

                await SendHtmlResponseAsync(response, "Authorization Successful",
                    $"Successfully authorized character: {verification.CharacterName}<br>You can close this window now.", true);

                return token;
            }
            finally
            {
                response.Close();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in callback listener");
            return null;
        }
        finally
        {
            StopListener();
        }
    }

    private void StopListener()
    {
        if (_listener != null)
        {
            try
            {
                _listener.Stop();
                _listener.Close();
                _logger.LogDebug("Callback listener stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping listener");
            }
        }
    }

    private static async Task SendHtmlResponseAsync(HttpListenerResponse response, string title, string message, bool success)
    {
        var color = success ? "#28a745" : "#dc3545";
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>{title}</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
            background-color: #f5f5f5;
        }}
        .container {{
            background: white;
            padding: 40px;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            text-align: center;
            max-width: 500px;
        }}
        h1 {{
            color: {color};
            margin-top: 0;
        }}
        p {{
            color: #666;
            line-height: 1.6;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>{title}</h1>
        <p>{message}</p>
    </div>
</body>
</html>";

        var buffer = Encoding.UTF8.GetBytes(html);
        response.ContentLength64 = buffer.Length;
        response.ContentType = "text/html; charset=utf-8";
        await response.OutputStream.WriteAsync(buffer);
    }
}
