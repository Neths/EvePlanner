from django.core.management.base import BaseCommand
from esi_app.models import Corporation
from esi_app.shared import call_get_esi, call_get_esi_secure
import datetime

class Command(BaseCommand):
    help = 'Fetches corporation data from the EVE Online ESI API and stores it in the database'

    def handle(self, *args, **options):
        corporations = Corporation.objects.all()
        for corporation in corporations:

            # Fetch data from ESI API
            try:
                path = f'/corporations/{corporation.corporation_id}/'
                data = call_get_esi(path)
            except Exception as e:
                self.stdout.write(self.style.ERROR(str(e)))
                continue
            print(data)
            corporation = Corporation(corporation_id=corporation.corporation_id, **data)
            corporation.save()

            self.stdout.write(self.style.SUCCESS(f'Updated corporation {corporation.name}'))
