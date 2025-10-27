using EveDataCollector.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace EveDataCollector.App.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CharactersController : ControllerBase
{
    private readonly IAuthRepository _authRepository;
    private readonly ILogger<CharactersController> _logger;

    public CharactersController(
        IAuthRepository authRepository,
        ILogger<CharactersController> logger)
    {
        _authRepository = authRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all authorized characters
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCharacters()
    {
        try
        {
            var characters = await _authRepository.GetAllCharactersAsync();
            return Ok(characters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving characters");
            return StatusCode(500, new { error = "Failed to retrieve characters", details = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific character by ID
    /// </summary>
    [HttpGet("{characterId}")]
    public async Task<IActionResult> GetCharacter(int characterId)
    {
        try
        {
            var character = await _authRepository.GetCharacterAsync(characterId);

            if (character == null)
            {
                return NotFound(new { error = $"Character {characterId} not found" });
            }

            return Ok(character);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving character {CharacterId}", characterId);
            return StatusCode(500, new { error = "Failed to retrieve character", details = ex.Message });
        }
    }

    /// <summary>
    /// Get list of authorized characters (tokens info implied by character authorization)
    /// </summary>
    [HttpGet("tokens")]
    public async Task<IActionResult> GetTokens()
    {
        try
        {
            var characters = await _authRepository.GetAllCharactersAsync();
            return Ok(characters.Select(c => new
            {
                c.CharacterId,
                c.CharacterName,
                c.CorporationId,
                c.CorporationName,
                c.AllianceId,
                c.AllianceName
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving characters with tokens");
            return StatusCode(500, new { error = "Failed to retrieve tokens", details = ex.Message });
        }
    }

    /// <summary>
    /// Invalidate a specific token
    /// </summary>
    [HttpDelete("tokens/{tokenId}")]
    public async Task<IActionResult> InvalidateToken(int tokenId)
    {
        try
        {
            await _authRepository.InvalidateTokenAsync(tokenId);
            return Ok(new { message = $"Token {tokenId} has been invalidated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating token {TokenId}", tokenId);
            return StatusCode(500, new { error = "Failed to invalidate token", details = ex.Message });
        }
    }

    /// <summary>
    /// Get OAuth authorization URL
    /// </summary>
    [HttpGet("auth/url")]
    public IActionResult GetAuthorizationUrl([FromQuery] string? state = null)
    {
        try
        {
            var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var oauthConfig = config.GetSection("EsiOAuth");

            var clientId = oauthConfig["ClientId"];
            var callbackUrl = oauthConfig["CallbackUrl"];
            var scopes = string.Join(" ", oauthConfig.GetSection("Scopes").Get<string[]>() ?? Array.Empty<string>());

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return BadRequest(new { error = "OAuth ClientId is not configured" });
            }

            var actualState = state ?? Guid.NewGuid().ToString("N");
            var authUrl = $"https://login.eveonline.com/v2/oauth/authorize?" +
                          $"response_type=code&" +
                          $"redirect_uri={Uri.EscapeDataString(callbackUrl ?? "")}&" +
                          $"client_id={clientId}&" +
                          $"scope={Uri.EscapeDataString(scopes)}&" +
                          $"state={actualState}";

            return Ok(new
            {
                authorizationUrl = authUrl,
                state = actualState,
                callbackUrl,
                scopes = scopes.Split(' ')
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating authorization URL");
            return StatusCode(500, new { error = "Failed to generate authorization URL", details = ex.Message });
        }
    }
}
