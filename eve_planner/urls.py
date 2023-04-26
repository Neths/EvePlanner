"""eve_planner URL Configuration

The `urlpatterns` list routes URLs to views. For more information please see:
    https://docs.djangoproject.com/en/4.1/topics/http/urls/
Examples:
Function views
    1. Add an import:  from my_app import views
    2. Add a URL to urlpatterns:  path('', views.home, name='home')
Class-based views
    1. Add an import:  from other_app.views import Home
    2. Add a URL to urlpatterns:  path('', Home.as_view(), name='home')
Including another URLconf
    1. Import the include() function: from django.urls import include, path
    2. Add a URL to urlpatterns:  path('blog/', include('blog.urls'))
"""
from django.contrib import admin
from django.urls import path, include

import reader
from rest_framework_extensions.routers import ExtendedSimpleRouter

from reader.views.character_views import CharacterViewSet, CharacterWalletJournalViewSet, \
    CharacterWalletTransactionViewSet
from reader.views.corporation_views import CorporationViewSet, CorporationDivisionViewSet, \
    CorporationWalletJournalViewSet
from reader.views.default import Home

router = ExtendedSimpleRouter()
characters_routes = router.register(r'characters', CharacterViewSet, basename='character')
characters_routes.register(r'wallet',
                           CharacterWalletJournalViewSet,
                           basename='wallet',
                           parents_query_lookups=['character_id'])
characters_routes.register(r'transaction',
                           CharacterWalletTransactionViewSet,
                           basename='transaction',
                           parents_query_lookups=['character_id'])

corporations_routes = router.register(r'corporations', CorporationViewSet, basename='corporation')
corp_divisions_routes = corporations_routes.register(r'divisions',
                                                     CorporationDivisionViewSet,
                                                     basename='division',
                                                     parents_query_lookups=['corporation_id'])
corp_divisions_routes.register(r'wallet',
                               CorporationWalletJournalViewSet,
                               basename='wallet',
                               parents_query_lookups=['corporation_id', 'division_id'])
corp_divisions_routes.register(r'transaction',
                               CharacterWalletTransactionViewSet,
                               basename='transaction',
                               parents_query_lookups=['corporation_id', 'division_id'])


urlpatterns = [
    path('admin/', admin.site.urls),
    path('', Home.as_view(), name='home'),
    path('api/', include(router.urls)),
    path('api/register/', reader.views.default.register_new_character),
    path('api/register/callback/', reader.views.default.register_callback)
]
