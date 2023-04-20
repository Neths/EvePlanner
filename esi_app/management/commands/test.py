from django.core.management import BaseCommand


class Command(BaseCommand):
    help = 'Fetches all data from the EVE Online ESI API and stores it in the database'

    def handle(self, *args, **options):
        a =[{"a":"apple"},{"b":"banana"}]
        b = [{"c":"cat"}]

        c = a.__add__(b)

        print(c)

