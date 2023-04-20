from esi_app.shared import call_get_esi_secure
from esi_app.models import CharacterApi, CorporationWallet, CorporationDivision
from result import Ok, Result


def divisions(character_api: CharacterApi) -> Result:
    print("corporation_divisions")
    corporation_id = character_api.character.corporation_id
    if corporation_id is None:
        return Ok(0)

    path = character_api.api.path.format(corporation_id=corporation_id)

    response_data = call_get_esi_secure(path, character_api.character.access_token)

    for entry in response_data['hangar']:
        result = CorporationDivision.objects.filter(corporation_id=corporation_id,
                                                    division=entry['division'],
                                                    type='hangar')

        if result.exists():
            result[0].name = entry['name'] if 'name' in entry else ''
            result[0].save()
        else:
            new_entry = CorporationDivision(corporation_id=corporation_id, type='hangar', **entry)
            new_entry.save()

    for entry in response_data['wallet']:
        result = CorporationDivision.objects.filter(corporation_id=corporation_id,
                                                    division=entry['division'],
                                                    type='wallet')

        if result.exists():
            result[0].name = entry['name'] if 'name' in entry else ''
            result[0].save()
        else:
            new_entry = CorporationDivision(corporation_id=corporation_id, type='wallet', **entry)
            new_entry.save()

    return Ok(0)
