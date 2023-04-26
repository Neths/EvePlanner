from rest_framework import serializers
from esi_app.models import Character, CharacterWalletJournal, CharacterWalletTransaction


class CharacterSerializer(serializers.ModelSerializer):
    class Meta:
        model = Character
        fields = ('character_id', 'name', 'corporation_id', 'alliance_id')


class CharacterWalletJournalSerializer(serializers.ModelSerializer):
    class Meta:
        model = CharacterWalletJournal
        fields = ('character_id', 'id', 'amount', 'balance', 'context_id', 'context_id_type', 'date', 'description',
                  'first_party_id', 'reason', 'ref_type', 'second_party_id', 'tax', 'tax_receiver_id')


class CharacterWalletTransactionSerializer(serializers.ModelSerializer):
    class Meta:
        model = CharacterWalletTransaction
        fields = ('character_id', 'client_id', 'date', 'is_buy', 'is_personal', 'journal_ref_id', 'location_id',
                  'quantity', 'transaction_id', 'type_id', 'unit_price')


