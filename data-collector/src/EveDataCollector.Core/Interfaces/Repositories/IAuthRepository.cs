using EveDataCollector.Core.Models.Auth;
using EveDataCollector.Core.Models;

namespace EveDataCollector.Core.Interfaces.Repositories;

/// <summary>
/// Repository for authentication and character data
/// </summary>
public interface IAuthRepository
{
    // ESI Applications
    Task<EsiApplication?> GetApplicationByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<EsiApplication?> GetApplicationByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
    Task<int> UpsertApplicationAsync(EsiApplication application, CancellationToken cancellationToken = default);

    // ESI Tokens
    Task<EsiToken?> GetTokenAsync(int applicationId, long characterId, CancellationToken cancellationToken = default);
    Task<List<EsiToken>> GetExpiredTokensAsync(CancellationToken cancellationToken = default);
    Task<int> UpsertTokenAsync(EsiToken token, CancellationToken cancellationToken = default);
    Task InvalidateTokenAsync(int tokenId, CancellationToken cancellationToken = default);

    // Characters
    Task<Character?> GetCharacterAsync(long characterId, CancellationToken cancellationToken = default);
    Task UpsertCharacterAsync(Character character, CancellationToken cancellationToken = default);
    Task<List<Character>> GetAllCharactersAsync(CancellationToken cancellationToken = default);
}
