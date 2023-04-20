from django.core.management.base import BaseCommand
from esi_app.api_caller import fetch_data
from esi_app.models import Character


class Command(BaseCommand):
    help = 'Fetches all data from the EVE Online ESI API and stores it in the database'

    def handle(self, *args, **options):
        for character in Character.objects.all():

            fetch_data(character)

