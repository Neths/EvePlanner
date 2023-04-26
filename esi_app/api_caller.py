import importlib
import datetime
import jwt
from result import Ok, Err

from esi_app.esi.character import character_by_id
from esi_app.models import Character, CharacterApi

import logging

_logger = logging.getLogger('db')


def fetch_character(character_id):
    _logger.debug('api_caller.fetch_character')
    character_by_id(character_id)

    return


def fetch_data(character: Character):
    _logger.debug('api_caller.fetch_data')
    for character_api in character.apis.all():
        api = character_api.api

        now = datetime.datetime.now(datetime.timezone.utc)

        if character_api.last_execution:
            last_execution_date = datetime.datetime.fromtimestamp(character_api.last_execution, tz=datetime.timezone.utc)
            if (now - last_execution_date).total_seconds() < api.cache_duration:
                _logger.info(f'data {api.handler} for character {character.name} already in cache, skip call')
                continue

        token_data = jwt.decode(jwt=character.access_token.access_token, options={"verify_signature": False})
        if api.scope and api.scope not in token_data['scp']:
            _logger.info(f'access_token for character {character.name} do not allow call api {api.handler}')
            continue

        _logger.info(f'call handler {api.handler} for character {character.name}')

        _generic_call(character_api)

    return


def _generic_call(character_api: CharacterApi):
    _logger.debug('api_caller._generic_call')
    mod_name, func_name = character_api.api.handler.rsplit('.', 1)
    module = importlib.import_module(mod_name)

    func = getattr(module, func_name)
    esi_response = func(character_api)

    match esi_response:
        case Ok(value):
            character_api.last_execution = datetime.datetime.now(datetime.timezone.utc).timestamp()
            character_api.last_result = value
            character_api.save()
        case Err(e):
            character_api.last_result = e
            character_api.save()

    return

