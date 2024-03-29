from result import Result, Ok

from esi_app.models import Character, Api, CharacterApi
from esi_app.shared import call_get_esi

import logging

_logger = logging.getLogger('db')


def character(character_api: CharacterApi) -> Result:
    _logger.debug('character')
    return character_by_id(character_api.character.character_id)


def character_by_id(character_id) -> Result:
    _logger.debug('character by id')
    api = Api.objects.get(name='Character')
    path = api.path.format(character_id=character_id)

    response_data = call_get_esi(path)

    char = Character(character_id=character_id, **response_data)
    char.save()

    return Ok(0)
