using EveDataCollector.Core.Models.CharacterData;

namespace EveDataCollector.Core.Interfaces.Repositories;

/// <summary>
/// Repository for character-specific data (skills, assets, wallet)
/// </summary>
public interface ICharacterDataRepository
{
    // Skills
    Task UpsertSkillsAsync(long characterId, List<CharacterSkill> skills, CancellationToken cancellationToken = default);
    Task UpsertSkillQueueAsync(long characterId, List<CharacterSkillQueueItem> queue, CancellationToken cancellationToken = default);
    Task<List<CharacterSkill>> GetSkillsAsync(long characterId, CancellationToken cancellationToken = default);
    Task<List<CharacterSkillQueueItem>> GetSkillQueueAsync(long characterId, CancellationToken cancellationToken = default);

    // Assets
    Task UpsertAssetsAsync(long characterId, List<CharacterAsset> assets, CancellationToken cancellationToken = default);
    Task<List<CharacterAsset>> GetAssetsAsync(long characterId, CancellationToken cancellationToken = default);
    Task DeleteAssetsAsync(long characterId, CancellationToken cancellationToken = default);

    // Wallet
    Task UpsertWalletBalanceAsync(long characterId, decimal balance, CancellationToken cancellationToken = default);
    Task<decimal> GetWalletBalanceAsync(long characterId, CancellationToken cancellationToken = default);
    Task InsertWalletJournalAsync(long characterId, List<CharacterWalletJournal> entries, CancellationToken cancellationToken = default);
    Task InsertWalletTransactionsAsync(long characterId, List<CharacterWalletTransaction> transactions, CancellationToken cancellationToken = default);
}
