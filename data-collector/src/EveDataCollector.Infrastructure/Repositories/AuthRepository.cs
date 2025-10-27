using Dapper;
using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Core.Models;
using EveDataCollector.Core.Models.Auth;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EveDataCollector.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for authentication and character data
/// </summary>
public class AuthRepository : IAuthRepository
{
    private readonly Func<NpgsqlConnection> _connectionFactory;
    private readonly ILogger<AuthRepository> _logger;

    public AuthRepository(Func<NpgsqlConnection> connectionFactory, ILogger<AuthRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    #region ESI Applications

    public async Task<EsiApplication?> GetApplicationByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT id, name, client_id, client_secret, callback_url, scopes, is_active,
                   created_at, updated_at
            FROM esi_applications
            WHERE id = @Id";

        return await connection.QueryFirstOrDefaultAsync<EsiApplication>(sql, new { Id = id });
    }

    public async Task<EsiApplication?> GetApplicationByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT id, name, client_id, client_secret, callback_url, scopes, is_active,
                   created_at, updated_at
            FROM esi_applications
            WHERE client_id = @ClientId";

        return await connection.QueryFirstOrDefaultAsync<EsiApplication>(sql, new { ClientId = clientId });
    }

    public async Task<int> UpsertApplicationAsync(EsiApplication application, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO esi_applications (name, client_id, client_secret, callback_url, scopes, is_active, created_at, updated_at)
            VALUES (@Name, @ClientId, @ClientSecret, @CallbackUrl, @Scopes, @IsActive, @CreatedAt, @UpdatedAt)
            ON CONFLICT (client_id)
            DO UPDATE SET
                name = EXCLUDED.name,
                client_secret = EXCLUDED.client_secret,
                callback_url = EXCLUDED.callback_url,
                scopes = EXCLUDED.scopes,
                is_active = EXCLUDED.is_active,
                updated_at = EXCLUDED.updated_at
            RETURNING id";

        application.UpdatedAt = DateTime.UtcNow;
        if (application.CreatedAt == default)
            application.CreatedAt = DateTime.UtcNow;

        return await connection.ExecuteScalarAsync<int>(sql, application);
    }

    #endregion

    #region ESI Tokens

    public async Task<EsiToken?> GetTokenAsync(int applicationId, long characterId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT id, application_id, character_id, access_token, refresh_token, token_type,
                   expires_at, scopes, is_valid, last_refreshed_at, created_at, updated_at
            FROM esi_tokens
            WHERE application_id = @ApplicationId AND character_id = @CharacterId";

        return await connection.QueryFirstOrDefaultAsync<EsiToken>(sql,
            new { ApplicationId = applicationId, CharacterId = characterId });
    }

    public async Task<List<EsiToken>> GetExpiredTokensAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT id, application_id, character_id, access_token, refresh_token, token_type,
                   expires_at, scopes, is_valid, last_refreshed_at, created_at, updated_at
            FROM esi_tokens
            WHERE is_valid = true AND expires_at < @Now
            ORDER BY expires_at";

        var tokens = await connection.QueryAsync<EsiToken>(sql, new { Now = DateTime.UtcNow });
        return tokens.ToList();
    }

    public async Task<int> UpsertTokenAsync(EsiToken token, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO esi_tokens (application_id, character_id, access_token, refresh_token, token_type,
                                   expires_at, scopes, is_valid, last_refreshed_at, created_at, updated_at)
            VALUES (@ApplicationId, @CharacterId, @AccessToken, @RefreshToken, @TokenType,
                    @ExpiresAt, @Scopes, @IsValid, @LastRefreshedAt, @CreatedAt, @UpdatedAt)
            ON CONFLICT (application_id, character_id)
            DO UPDATE SET
                access_token = EXCLUDED.access_token,
                refresh_token = EXCLUDED.refresh_token,
                token_type = EXCLUDED.token_type,
                expires_at = EXCLUDED.expires_at,
                scopes = EXCLUDED.scopes,
                is_valid = EXCLUDED.is_valid,
                last_refreshed_at = EXCLUDED.last_refreshed_at,
                updated_at = EXCLUDED.updated_at
            RETURNING id";

        token.UpdatedAt = DateTime.UtcNow;
        if (token.CreatedAt == default)
            token.CreatedAt = DateTime.UtcNow;

        return await connection.ExecuteScalarAsync<int>(sql, token);
    }

    public async Task InvalidateTokenAsync(int tokenId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            UPDATE esi_tokens
            SET is_valid = false, updated_at = @Now
            WHERE id = @Id";

        await connection.ExecuteAsync(sql, new { Id = tokenId, Now = DateTime.UtcNow });
    }

    #endregion

    #region Characters

    public async Task<Character?> GetCharacterAsync(long characterId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT character_id, character_name, corporation_id, corporation_name,
                   alliance_id, alliance_name, faction_id, birthday, gender, race_id,
                   bloodline_id, ancestry_id, security_status, description,
                   created_at, updated_at
            FROM characters
            WHERE character_id = @CharacterId";

        return await connection.QueryFirstOrDefaultAsync<Character>(sql, new { CharacterId = characterId });
    }

    public async Task UpsertCharacterAsync(Character character, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO characters (character_id, character_name, corporation_id, corporation_name,
                                   alliance_id, alliance_name, faction_id, birthday, gender, race_id,
                                   bloodline_id, ancestry_id, security_status, description,
                                   created_at, updated_at)
            VALUES (@CharacterId, @CharacterName, @CorporationId, @CorporationName,
                    @AllianceId, @AllianceName, @FactionId, @Birthday, @Gender, @RaceId,
                    @BloodlineId, @AncestryId, @SecurityStatus, @Description,
                    @CreatedAt, @UpdatedAt)
            ON CONFLICT (character_id)
            DO UPDATE SET
                character_name = EXCLUDED.character_name,
                corporation_id = EXCLUDED.corporation_id,
                corporation_name = EXCLUDED.corporation_name,
                alliance_id = EXCLUDED.alliance_id,
                alliance_name = EXCLUDED.alliance_name,
                faction_id = EXCLUDED.faction_id,
                birthday = EXCLUDED.birthday,
                gender = EXCLUDED.gender,
                race_id = EXCLUDED.race_id,
                bloodline_id = EXCLUDED.bloodline_id,
                ancestry_id = EXCLUDED.ancestry_id,
                security_status = EXCLUDED.security_status,
                description = EXCLUDED.description,
                updated_at = EXCLUDED.updated_at";

        character.UpdatedAt = DateTime.UtcNow;
        if (character.CreatedAt == default)
            character.CreatedAt = DateTime.UtcNow;

        await connection.ExecuteAsync(sql, character);
    }

    public async Task<List<Character>> GetAllCharactersAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT character_id, character_name, corporation_id, corporation_name,
                   alliance_id, alliance_name, faction_id, birthday, gender, race_id,
                   bloodline_id, ancestry_id, security_status, description,
                   created_at, updated_at
            FROM characters
            ORDER BY character_name";

        var characters = await connection.QueryAsync<Character>(sql);
        return characters.ToList();
    }

    #endregion
}
