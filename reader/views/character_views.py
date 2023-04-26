from rest_framework import viewsets
from rest_framework_extensions.mixins import NestedViewSetMixin

from esi_app.models import Character, CharacterWalletJournal, CharacterWalletTransaction
from reader.serializers.character_serializers import CharacterSerializer, CharacterWalletJournalSerializer, \
    CharacterWalletTransactionSerializer


class CharacterViewSet(NestedViewSetMixin, viewsets.ModelViewSet):
    model = Character
    serializer_class = CharacterSerializer
    queryset = Character.objects.all()


class CharacterWalletJournalViewSet(NestedViewSetMixin, viewsets.ReadOnlyModelViewSet):
    model = CharacterWalletJournal
    serializer_class = CharacterWalletJournalSerializer
    queryset = CharacterWalletJournal.objects.all()


class CharacterWalletTransactionViewSet(viewsets.ReadOnlyModelViewSet):
    model = CharacterWalletTransaction
    serializer_class = CharacterWalletTransactionSerializer
    queryset = CharacterWalletTransaction.objects.all()

