using Dapper;
using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Core.Models.CharacterData;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace EveDataCollector.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for character-specific data
/// </summary>
public class CharacterDataRepository : ICharacterDataRepository
{
    private readonly Func<NpgsqlConnection> _connectionFactory;
    private readonly ILogger<CharacterDataRepository> _logger;

    public CharacterDataRepository(
        Func<NpgsqlConnection> connectionFactory,
        ILogger<CharacterDataRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    #region Skills

    public async Task UpsertSkillsAsync(long characterId, List<CharacterSkill> skills, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO character_skills (character_id, skill_id, active_skill_level, trained_skill_level,
                                         skillpoints_in_skill, created_at, updated_at)
            VALUES (@CharacterId, @SkillId, @ActiveSkillLevel, @TrainedSkillLevel,
                    @SkillpointsInSkill, @CreatedAt, @UpdatedAt)
            ON CONFLICT (character_id, skill_id)
            DO UPDATE SET
                active_skill_level = EXCLUDED.active_skill_level,
                trained_skill_level = EXCLUDED.trained_skill_level,
                skillpoints_in_skill = EXCLUDED.skillpoints_in_skill,
                updated_at = EXCLUDED.updated_at";

        var now = DateTime.UtcNow;
        foreach (var skill in skills)
        {
            skill.UpdatedAt = now;
            if (skill.CreatedAt == default)
                skill.CreatedAt = now;
        }

        await connection.ExecuteAsync(sql, skills);
    }

    public async Task UpsertSkillQueueAsync(long characterId, List<CharacterSkillQueueItem> queue, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        // Delete old queue first
        await connection.ExecuteAsync(
            "DELETE FROM character_skill_queue WHERE character_id = @CharacterId",
            new { CharacterId = characterId });

        if (queue.Count == 0)
            return;

        const string sql = @"
            INSERT INTO character_skill_queue (character_id, skill_id, queue_position, finished_level,
                                              start_date, finish_date, training_start_sp, level_start_sp,
                                              level_end_sp, created_at, updated_at)
            VALUES (@CharacterId, @SkillId, @QueuePosition, @FinishedLevel,
                    @StartDate, @FinishDate, @TrainingStartSp, @LevelStartSp,
                    @LevelEndSp, @CreatedAt, @UpdatedAt)";

        var now = DateTime.UtcNow;
        foreach (var item in queue)
        {
            item.UpdatedAt = now;
            if (item.CreatedAt == default)
                item.CreatedAt = now;
        }

        await connection.ExecuteAsync(sql, queue);
    }

    public async Task<List<CharacterSkill>> GetSkillsAsync(long characterId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT character_id, skill_id, active_skill_level, trained_skill_level,
                   skillpoints_in_skill, created_at, updated_at
            FROM character_skills
            WHERE character_id = @CharacterId
            ORDER BY skill_id";

        var skills = await connection.QueryAsync<CharacterSkill>(sql, new { CharacterId = characterId });
        return skills.ToList();
    }

    public async Task<List<CharacterSkillQueueItem>> GetSkillQueueAsync(long characterId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT id, character_id, skill_id, queue_position, finished_level,
                   start_date, finish_date, training_start_sp, level_start_sp,
                   level_end_sp, created_at, updated_at
            FROM character_skill_queue
            WHERE character_id = @CharacterId
            ORDER BY queue_position";

        var queue = await connection.QueryAsync<CharacterSkillQueueItem>(sql, new { CharacterId = characterId });
        return queue.ToList();
    }

    #endregion

    #region Assets

    public async Task UpsertAssetsAsync(long characterId, List<CharacterAsset> assets, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO character_assets (item_id, character_id, type_id, location_id, location_type,
                                         location_flag, quantity, is_singleton, is_blueprint_copy,
                                         created_at, updated_at)
            VALUES (@ItemId, @CharacterId, @TypeId, @LocationId, @LocationType,
                    @LocationFlag, @Quantity, @IsSingleton, @IsBlueprintCopy,
                    @CreatedAt, @UpdatedAt)
            ON CONFLICT (item_id)
            DO UPDATE SET
                type_id = EXCLUDED.type_id,
                location_id = EXCLUDED.location_id,
                location_type = EXCLUDED.location_type,
                location_flag = EXCLUDED.location_flag,
                quantity = EXCLUDED.quantity,
                is_singleton = EXCLUDED.is_singleton,
                is_blueprint_copy = EXCLUDED.is_blueprint_copy,
                updated_at = EXCLUDED.updated_at";

        var now = DateTime.UtcNow;
        foreach (var asset in assets)
        {
            asset.UpdatedAt = now;
            if (asset.CreatedAt == default)
                asset.CreatedAt = now;
        }

        await connection.ExecuteAsync(sql, assets);
    }

    public async Task<List<CharacterAsset>> GetAssetsAsync(long characterId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            SELECT item_id, character_id, type_id, location_id, location_type,
                   location_flag, quantity, is_singleton, is_blueprint_copy,
                   created_at, updated_at
            FROM character_assets
            WHERE character_id = @CharacterId";

        var assets = await connection.QueryAsync<CharacterAsset>(sql, new { CharacterId = characterId });
        return assets.ToList();
    }

    public async Task DeleteAssetsAsync(long characterId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        await connection.ExecuteAsync(
            "DELETE FROM character_assets WHERE character_id = @CharacterId",
            new { CharacterId = characterId });
    }

    #endregion

    #region Wallet

    public async Task UpsertWalletBalanceAsync(long characterId, decimal balance, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO character_wallet (character_id, balance, created_at, updated_at)
            VALUES (@CharacterId, @Balance, @Now, @Now)
            ON CONFLICT (character_id)
            DO UPDATE SET
                balance = EXCLUDED.balance,
                updated_at = EXCLUDED.updated_at";

        await connection.ExecuteAsync(sql, new { CharacterId = characterId, Balance = balance, Now = DateTime.UtcNow });
    }

    public async Task<decimal> GetWalletBalanceAsync(long characterId, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = "SELECT balance FROM character_wallet WHERE character_id = @CharacterId";

        return await connection.ExecuteScalarAsync<decimal>(sql, new { CharacterId = characterId });
    }

    public async Task InsertWalletJournalAsync(long characterId, List<CharacterWalletJournal> entries, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO character_wallet_journal (id, character_id, date, ref_type, first_party_id,
                                                  second_party_id, amount, balance, reason, tax,
                                                  tax_receiver_id, description, created_at)
            VALUES (@Id, @CharacterId, @Date, @RefType, @FirstPartyId,
                    @SecondPartyId, @Amount, @Balance, @Reason, @Tax,
                    @TaxReceiverId, @Description, @CreatedAt)
            ON CONFLICT (id) DO NOTHING";

        var now = DateTime.UtcNow;
        foreach (var entry in entries)
        {
            if (entry.CreatedAt == default)
                entry.CreatedAt = now;
        }

        await connection.ExecuteAsync(sql, entries);
    }

    public async Task InsertWalletTransactionsAsync(long characterId, List<CharacterWalletTransaction> transactions, CancellationToken cancellationToken = default)
    {
        await using var connection = _connectionFactory();
        await connection.OpenAsync(cancellationToken);

        const string sql = @"
            INSERT INTO character_wallet_transactions (transaction_id, character_id, date, type_id,
                                                       location_id, quantity, unit_price, client_id,
                                                       is_buy, is_personal, journal_ref_id, created_at)
            VALUES (@TransactionId, @CharacterId, @Date, @TypeId,
                    @LocationId, @Quantity, @UnitPrice, @ClientId,
                    @IsBuy, @IsPersonal, @JournalRefId, @CreatedAt)
            ON CONFLICT (transaction_id) DO NOTHING";

        var now = DateTime.UtcNow;
        foreach (var transaction in transactions)
        {
            if (transaction.CreatedAt == default)
                transaction.CreatedAt = now;
        }

        await connection.ExecuteAsync(sql, transactions);
    }

    #endregion
}
