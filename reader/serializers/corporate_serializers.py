from rest_framework import serializers
from esi_app.models import Corporation, CorporationDivision, CorporationWalletJournal, CorporationWalletTransaction


class CorporationSerializer(serializers.ModelSerializer):
    class Meta:
        model = Corporation
        fields = ('corporation_id', 'name', 'alliance_id', 'ceo_id', 'creator_id', 'date_founded',
                  'description', 'faction_id', 'home_station_id', 'member_count', 'shares', 'tax_rate',
                  'ticker', 'url', 'war_eligible')


class CorporationDivisionSerializer(serializers.ModelSerializer):
    class Meta:
        model = CorporationDivision
        fields = ('corporation_id', 'type', 'division', 'name')


class CorporationWalletJournalSerializer(serializers.ModelSerializer):
    class Meta:
        model = CorporationWalletJournal
        fields = ('corporation_id', 'division_id', 'id', 'amount', 'balance', 'context_id', 'context_id_type', 'date',
                  'description', 'first_party_id', 'reason', 'ref_type', 'second_party_id', 'tax', 'tax_receiver_id')


class CorporationWalletTransactionSerializer(serializers.ModelSerializer):
    class Meta:
        model = CorporationWalletTransaction
        fields = ('corporation_id', 'division_id', 'client_id', 'date', 'is_buy', 'journal_ref_id', 'location_id',
                  'quantity', 'transaction_id', 'type_id', 'unit_price')


