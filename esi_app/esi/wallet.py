from esi_app.shared import call_get_esi_secure
from esi_app.models import CharacterWallet, CharacterApi, CharacterWalletJournal, \
    CharacterWalletTransaction, CorporationWallet, CorporationDivision, CorporationWalletJournal, \
    CorporationWalletTransaction
from result import Ok, Result

import logging

_logger = logging.getLogger('db')


def character_wallet(character_api: CharacterApi) -> Result:
    _logger.debug('wallet.character_wallet')
    character_id = character_api.character.character_id
    path = character_api.api.path.format(character_id=character_id)

    response_data = call_get_esi_secure(path, character_api.character.access_token)

    result = CharacterWallet.objects.filter(character_id=character_id)
    if result.exists():
        wallet = result[0]
    else:
        wallet = CharacterWallet(character_id=character_id, balance=response_data)

    wallet.save()

    return Ok(0)


def character_wallet_journal(character_api: CharacterApi) -> Result:
    _logger.debug('wallet.character_wallet_journal')
    character_id = character_api.character.character_id
    path = character_api.api.path.format(character_id=character_id)

    response_data = call_get_esi_secure(path, character_api.character.access_token)

    for entry in response_data:
        if CharacterWalletJournal.objects.filter(character_id=character_id, id=entry['id']).exists():
            continue

        new_entry = CharacterWalletJournal(character_id=character_id, **entry)
        new_entry.save()

    return Ok(0)


def character_wallet_transactions(character_api: CharacterApi) -> Result:
    _logger.debug('wallet.character_wallet_transactions')
    character_id = character_api.character.character_id
    path = character_api.api.path.format(character_id=character_id)

    response_data = call_get_esi_secure(path, character_api.character.access_token)

    for entry in response_data:
        if CharacterWalletTransaction.objects.filter(character_id=character_id, transaction_id=entry['transaction_id']).exists():
            continue

        new_entry = CharacterWalletTransaction(character_id=character_id, **entry)
        new_entry.save()

    return Ok(0)


def corporation_wallet(character_api: CharacterApi) -> Result:
    _logger.debug('wallet.corporation_wallet')
    corporation_id = character_api.character.corporation_id
    path = character_api.api.path.format(corporation_id=corporation_id)

    response_data = call_get_esi_secure(path, character_api.character.access_token)

    for entry in response_data:
        wallet = CorporationWallet.objects.filter(corporation_id=corporation_id, division=entry['division'])

        if wallet.exists():
            wallet[0].balance = entry['balance']
            wallet[0].save()
        else:
            new_wallet = CorporationWallet(corporation_id=corporation_id, **entry)
            new_wallet.save()

    return Ok(0)


def corporation_wallet_journal(character_api: CharacterApi) -> Result:
    _logger.debug('wallet.corporation_wallet_journal')
    corporation_id = character_api.character.corporation_id

    for division in CorporationDivision.objects.filter(corporation_id=corporation_id, type='wallet'):
        path = character_api.api.path.format(corporation_id=corporation_id, division=division.division)

        response_data = call_get_esi_secure(path, character_api.character.access_token)

        for entry in response_data:
            if CorporationWalletJournal.objects.filter(corporation_id=corporation_id, division_id=division.division, id=entry['id']).exists():
                continue

            new_entry = CorporationWalletJournal(corporation_id=corporation_id, division_id=division.division, **entry)
            new_entry.save()

    return Ok(0)


def corporation_wallet_transactions(character_api: CharacterApi) -> Result:
    _logger.debug(f'{__file__}.corporation_wallet_transactions')
    corporation_id = character_api.character.corporation_id

    for division in CorporationDivision.objects.filter(corporation_id=corporation_id, type='wallet'):
        path = character_api.api.path.format(corporation_id=corporation_id, division=division.division)

        response_data = call_get_esi_secure(path, character_api.character.access_token)

        for entry in response_data:
            if CorporationWalletTransaction.objects.filter(corporation_id=corporation_id, division_id=division.division, transaction_id=entry['transaction_id']).exists():
                continue

            new_entry = CorporationWalletTransaction(corporation_id=corporation_id, division_id=division.division, **entry)
            new_entry.save()

    return Ok(0)
