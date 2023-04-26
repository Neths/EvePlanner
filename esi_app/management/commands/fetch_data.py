from django.core.management.base import BaseCommand
from esi_app.api_caller import fetch_data
from esi_app.models import Character
import logging


class Command(BaseCommand):
    help = 'Fetches all data from the EVE Online ESI API and stores it in the database'
    _logger = logging.getLogger('db')

    def handle(self, *args, **options):
        for character in Character.objects.all():
            self._logger.info(f'fetch data for user {character.name}')

            fetch_data(character)

