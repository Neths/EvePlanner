from rest_framework import serializers
from esi_app.models import Character


class CharacterSerializer(serializers.ModelSerializer):

    class Meta:
        model = Character
        fields = ('character_id', 'name', 'corporation_id', 'alliance_id')

