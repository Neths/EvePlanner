import datetime
import base64
from typing import Any

import jwt
import requests
import urllib.parse

from django.conf import settings
from esi_app.models import CharacterAccessToken, Character


def validate_token(token: CharacterAccessToken) -> bool:
    now = datetime.datetime.now(datetime.timezone.utc)
    if not token.access_token:
        return False
    token_data = jwt.decode(jwt=token.access_token, options={"verify_signature": False})

    # Check if token is expired
    token_expire_date = datetime.datetime.fromtimestamp(token_data['exp'], tz=datetime.timezone.utc)
    if (now - token_expire_date).total_seconds() > 0:
        print("token expired")
        print(now - token_expire_date)
        return False

    return True


def refresh_token(token: CharacterAccessToken) -> CharacterAccessToken:
    basic_auth = f'{settings.EVEONLINE_CLIENTID}:{settings.EVEONLINE_SECRET}'
    encoded_basic_auth = base64.b64encode(basic_auth.encode('utf-8'))
    headers = {
        'Authorization': f'Basic {str(encoded_basic_auth, "utf-8")}',
        'Content-Type': 'application/x-www-form-urlencoded',
        'Host': 'login.eveonline.com'
    }
    url = f'{settings.EVEONLINE_LOGIN_URL}/v2/oauth/token'
    data = {'grant_type': 'refresh_token', 'refresh_token': token.refresh_token}
    try:
        response = requests.post(url, headers=headers, data=urllib.parse.urlencode(data))
        response.raise_for_status()
    except Exception as e:
        print(e)
        return

    response_data = response.json()

    token_data = jwt.decode(jwt=response_data['access_token'], options={"verify_signature": False})

    token.access_token = response_data['access_token']
    token.scopes = token_data['scp']
    token.issued_at = token_data['iat']
    token.expire_time = token_data['exp']
    token.save()

    return token


def get_token(code: str):  # sourcery skip: extract-method
    basic_auth = f'{settings.EVEONLINE_CLIENTID}:{settings.EVEONLINE_SECRET}'
    encoded_basic_auth = base64.b64encode(basic_auth.encode('utf-8'))
    headers = {
        'Authorization': f'Basic {str(encoded_basic_auth, "utf-8")}',
        'Content-Type': 'application/x-www-form-urlencoded',
        'Host': 'login.eveonline.com'
    }
    url = f'{settings.EVEONLINE_LOGIN_URL}/v2/oauth/token'
    data = {'grant_type': 'authorization_code', 'code': code}
    try:
        response = requests.post(url, headers=headers, data=urllib.parse.urlencode(data))
        response.raise_for_status()
    except Exception as e:
        print(e)
        return -1

    response_data = response.json()

    token_data = jwt.decode(jwt=response_data['access_token'], options={"verify_signature": False})

    _, character_id = token_data['sub'].rsplit(':', 1)

    if Character.objects.filter(pk=character_id).exists():
        character = Character.objects.get(pk=character_id)
        character_access_token = character.access_token
        character_access_token.access_token = response_data['access_token']
        character_access_token.scopes = token_data['scp']
        character_access_token.issued_at = token_data['iat']
        character_access_token.expire_time = token_data['exp']
        character_access_token.save()
    else:
        new_character = Character(pk=character_id)
        new_character.save()
        new_character_token = CharacterAccessToken(character=new_character,
                                                   access_token=response_data['access_token'],
                                                   refresh_token=response_data['refresh_token'],
                                                   issued_at=token_data['iat'],
                                                   expire_time=token_data['exp'],
                                                   scopes=token_data['scp'])
        new_character_token.save()
    return int(character_id)


def call_get_esi_secure(path, token: CharacterAccessToken, page=1) -> Any:
    if not validate_token(token):
        token = refresh_token(token)

    headers = {
        'Authorization': f'Bearer {token.access_token}'
    }

    url = f"{settings.ESI_BASE_URL}{path}"

    params = {"page": page} if page > 1 else {}

    response = requests.get(url, headers=headers, params=params)
    response.raise_for_status()
    response_data = response.json()

    if 'X-Pages' in response.headers and int(response.headers['X-Pages']) > page:
        r = call_get_esi_secure(path, token, page+1)
        response_data.__add__(r)

    return response_data


def call_get_esi(path, page=1) -> Any:
    url = f"{settings.ESI_BASE_URL}{path}"

    params = {"page": page} if page > 1 else {}

    response = requests.get(url, params=params)
    response.raise_for_status()
    response_data = response.json()

    if 'X-Pages' in response.headers and int(response.headers['X-Pages']) > page:
        r = call_get_esi(path, page+1)
        response_data.__add__(r)

    return response_data
