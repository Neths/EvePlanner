from django.core.management.base import BaseCommand
from esi_app.api_caller import fetch_character
from esi_app.models import Character


class Command(BaseCommand):
    help = 'Fetches character data from the EVE Online ESI API and stores it in the database'

    def handle(self, *args, **options):
        characters = Character.objects.all()
        for character in characters:

            fetch_character(character.character_id)

            self.stdout.write(self.style.SUCCESS(f'Updated character {character.name}'))
