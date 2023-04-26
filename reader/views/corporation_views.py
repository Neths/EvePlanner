from rest_framework import viewsets
from rest_framework_extensions.mixins import NestedViewSetMixin

from esi_app.models import Corporation, \
    CorporationDivision, CorporationWalletJournal, CorporationWalletTransaction
from reader.serializers.corporate_serializers import CorporationSerializer, CorporationDivisionSerializer, \
    CorporationWalletJournalSerializer, CorporationWalletTransactionSerializer


class CorporationViewSet(NestedViewSetMixin, viewsets.ModelViewSet):
    model = Corporation
    serializer_class = CorporationSerializer
    queryset = Corporation.objects.all()


class CorporationDivisionViewSet(NestedViewSetMixin, viewsets.ModelViewSet):
    model = CorporationDivision
    serializer_class = CorporationDivisionSerializer
    queryset = CorporationDivision.objects.all()


class CorporationWalletJournalViewSet(NestedViewSetMixin, viewsets.ReadOnlyModelViewSet):
    model = CorporationWalletJournal
    serializer_class = CorporationWalletJournalSerializer
    queryset = CorporationWalletJournal.objects.all()


class CharacterWalletTransactionViewSet(viewsets.ReadOnlyModelViewSet):
    model = CorporationWalletTransaction
    serializer_class = CorporationWalletTransactionSerializer
    queryset = CorporationWalletTransaction.objects.all()

