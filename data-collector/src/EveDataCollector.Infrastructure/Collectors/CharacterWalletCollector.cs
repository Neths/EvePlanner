using EveDataCollector.Core.Interfaces.Repositories;
using EveDataCollector.Core.Models.CharacterData;
using EveDataCollector.Infrastructure.ESI;
using Microsoft.Extensions.Logging;

namespace EveDataCollector.Infrastructure.Collectors;

/// <summary>
/// Collector for character wallet data (balance, journal, transactions)
/// </summary>
public class CharacterWalletCollector
{
    private readonly AuthenticatedEsiClient _esiClient;
    private readonly ICharacterDataRepository _repository;
    private readonly ILogger<CharacterWalletCollector> _logger;

    public CharacterWalletCollector(
        AuthenticatedEsiClient esiClient,
        ICharacterDataRepository repository,
        ILogger<CharacterWalletCollector> logger)
    {
        _esiClient = esiClient;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Collect character wallet data
    /// </summary>
    public async Task CollectAsync(long characterId, int applicationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Collecting wallet data for character {CharacterId}", characterId);

        try
        {
            // Collect wallet balance
            await CollectBalanceAsync(characterId, applicationId, cancellationToken);

            // Collect wallet journal (last 30 days)
            await CollectJournalAsync(characterId, applicationId, cancellationToken);

            // Collect wallet transactions (last 30 days)
            await CollectTransactionsAsync(characterId, applicationId, cancellationToken);

            _logger.LogInformation("Wallet collection completed for character {CharacterId}", characterId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect wallet data for character {CharacterId}", characterId);
            throw;
        }
    }

    private async Task CollectBalanceAsync(long characterId, int applicationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching wallet balance for character {CharacterId}", characterId);

        var balance = await _esiClient.GetAsync<decimal>(
            characterId,
            applicationId,
            $"/characters/{characterId}/wallet/",
            cancellationToken);

        await _repository.UpsertWalletBalanceAsync(characterId, balance, cancellationToken);

        _logger.LogInformation("Wallet balance for character {CharacterId}: {Balance:N2} ISK", characterId, balance);
    }

    private async Task CollectJournalAsync(long characterId, int applicationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching wallet journal for character {CharacterId}", characterId);

        var allEntries = new List<CharacterWalletJournal>();
        int page = 1;

        while (true)
        {
            var pageEntries = await _esiClient.GetAsync<List<JournalResponse>>(
                characterId,
                applicationId,
                $"/characters/{characterId}/wallet/journal/?page={page}",
                cancellationToken);

            if (pageEntries == null || pageEntries.Count == 0)
            {
                break;
            }

            var entries = pageEntries.Select(j => new CharacterWalletJournal
            {
                Id = j.Id,
                CharacterId = characterId,
                Date = j.Date,
                RefType = j.RefType,
                FirstPartyId = j.FirstPartyId,
                SecondPartyId = j.SecondPartyId,
                Amount = j.Amount ?? 0,
                Balance = j.Balance ?? 0,
                Reason = j.Reason,
                Tax = j.Tax,
                TaxReceiverId = j.TaxReceiverId,
                Description = j.Description
            }).ToList();

            allEntries.AddRange(entries);

            if (pageEntries.Count < 1000)
            {
                break;
            }

            page++;
        }

        if (allEntries.Count > 0)
        {
            await _repository.InsertWalletJournalAsync(characterId, allEntries, cancellationToken);
            _logger.LogInformation("Saved {Count} wallet journal entries for character {CharacterId}",
                allEntries.Count, characterId);
        }
    }

    private async Task CollectTransactionsAsync(long characterId, int applicationId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Fetching wallet transactions for character {CharacterId}", characterId);

        // Transactions are returned with latest first, max 2500
        var response = await _esiClient.GetAsync<List<TransactionResponse>>(
            characterId,
            applicationId,
            $"/characters/{characterId}/wallet/transactions/",
            cancellationToken);

        if (response == null || response.Count == 0)
        {
            _logger.LogInformation("No wallet transactions found for character {CharacterId}", characterId);
            return;
        }

        var transactions = response.Select(t => new CharacterWalletTransaction
        {
            TransactionId = t.TransactionId,
            CharacterId = characterId,
            Date = t.Date,
            TypeId = t.TypeId,
            LocationId = t.LocationId,
            Quantity = t.Quantity,
            UnitPrice = t.UnitPrice,
            ClientId = t.ClientId,
            IsBuy = t.IsBuy,
            IsPersonal = t.IsPersonal,
            JournalRefId = t.JournalRefId
        }).ToList();

        await _repository.InsertWalletTransactionsAsync(characterId, transactions, cancellationToken);

        _logger.LogInformation("Saved {Count} wallet transactions for character {CharacterId}",
            transactions.Count, characterId);
    }

    // ESI Response DTOs
    private class JournalResponse
    {
        public long Id { get; set; }
        public DateTime Date { get; set; }
        public string RefType { get; set; } = string.Empty;
        public long? FirstPartyId { get; set; }
        public long? SecondPartyId { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Balance { get; set; }
        public string? Reason { get; set; }
        public decimal? Tax { get; set; }
        public long? TaxReceiverId { get; set; }
        public string? Description { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public long IdJson { set => Id = value; }

        [System.Text.Json.Serialization.JsonPropertyName("date")]
        public DateTime DateJson { set => Date = value; }

        [System.Text.Json.Serialization.JsonPropertyName("ref_type")]
        public string RefTypeJson { set => RefType = value; }

        [System.Text.Json.Serialization.JsonPropertyName("first_party_id")]
        public long? FirstPartyIdJson { set => FirstPartyId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("second_party_id")]
        public long? SecondPartyIdJson { set => SecondPartyId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("amount")]
        public decimal? AmountJson { set => Amount = value; }

        [System.Text.Json.Serialization.JsonPropertyName("balance")]
        public decimal? BalanceJson { set => Balance = value; }

        [System.Text.Json.Serialization.JsonPropertyName("reason")]
        public string? ReasonJson { set => Reason = value; }

        [System.Text.Json.Serialization.JsonPropertyName("tax")]
        public decimal? TaxJson { set => Tax = value; }

        [System.Text.Json.Serialization.JsonPropertyName("tax_receiver_id")]
        public long? TaxReceiverIdJson { set => TaxReceiverId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("description")]
        public string? DescriptionJson { set => Description = value; }
    }

    private class TransactionResponse
    {
        public long TransactionId { get; set; }
        public DateTime Date { get; set; }
        public int TypeId { get; set; }
        public long LocationId { get; set; }
        public long Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public long ClientId { get; set; }
        public bool IsBuy { get; set; }
        public bool IsPersonal { get; set; }
        public long? JournalRefId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("transaction_id")]
        public long TransactionIdJson { set => TransactionId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("date")]
        public DateTime DateJson { set => Date = value; }

        [System.Text.Json.Serialization.JsonPropertyName("type_id")]
        public int TypeIdJson { set => TypeId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("location_id")]
        public long LocationIdJson { set => LocationId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("quantity")]
        public long QuantityJson { set => Quantity = value; }

        [System.Text.Json.Serialization.JsonPropertyName("unit_price")]
        public decimal UnitPriceJson { set => UnitPrice = value; }

        [System.Text.Json.Serialization.JsonPropertyName("client_id")]
        public long ClientIdJson { set => ClientId = value; }

        [System.Text.Json.Serialization.JsonPropertyName("is_buy")]
        public bool IsBuyJson { set => IsBuy = value; }

        [System.Text.Json.Serialization.JsonPropertyName("is_personal")]
        public bool IsPersonalJson { set => IsPersonal = value; }

        [System.Text.Json.Serialization.JsonPropertyName("journal_ref_id")]
        public long? JournalRefIdJson { set => JournalRefId = value; }
    }
}
