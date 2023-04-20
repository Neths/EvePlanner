from django.http import HttpResponseRedirect
from django.views.generic import TemplateView
from rest_framework import viewsets
from rest_framework.decorators import api_view
from rest_framework.response import Response
from rest_framework.request import Request

from esi_app.api_caller import fetch_character
from esi_app.models import Character, Api
from eve_planner.settings import EVEONLINE_LOGIN_URL, LOGIN_REDIRECT_URL, EVEONLINE_CLIENTID, UI_URL
from .serializers import CharacterSerializer
from rest_framework_extensions.mixins import NestedViewSetMixin
from esi_app.shared import get_token


class Home(TemplateView):
    template_name = 'base_generic.html'


class CharacterViewSet(NestedViewSetMixin, viewsets.ModelViewSet):
    model = Character
    serializer_class = CharacterSerializer
    queryset = Character.objects.all()


@api_view()
def register_new_character(request):
    return Response({"url": f"{EVEONLINE_LOGIN_URL}/v2/oauth/authorize/"
                            f"?response_type=code"
                            f"&redirect_uri={LOGIN_REDIRECT_URL}"
                            f"&client_id={EVEONLINE_CLIENTID}"
                            f"&scope={'%20'.join(list(map(lambda api: api.scope, Api.objects.filter(scope__isnull=False))))}"
                            f"&state=eve-planner"})


@api_view()
def register_callback(request: Request):
    print(request.query_params)
    character_id = get_token(request.query_params['code'])
    if character_id > 0:
        fetch_character(character_id)
    return HttpResponseRedirect(redirect_to=f'{UI_URL}/characters')

