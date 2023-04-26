from django.db import models


class ApiGroup(models.Model):
    id = models.IntegerField(unique=True, primary_key=True)
    name = models.CharField(max_length=255)

    def __str__(self):
        return self.name


class Api(models.Model):
    id = models.IntegerField(unique=True, primary_key=True)
    name = models.CharField(max_length=255)
    path = models.CharField(max_length=255)
    cache_duration = models.IntegerField(null=True)
    # expire_time = models.TimeField(null=True)
    # protected = models.BooleanField(null=True, default=False)
    scope = models.CharField(max_length=255, null=True)
    group = models.ForeignKey(ApiGroup, on_delete=models.CASCADE)
    handler = models.CharField(max_length=100)

    def __str__(self):
        return self.name


class Character(models.Model):
    character_id = models.BigIntegerField(unique=True, primary_key=True)
    name = models.CharField(max_length=255)
    corporation_id = models.BigIntegerField(null=True)
    alliance_id = models.BigIntegerField(null=True)
    birthday = models.CharField(max_length=50,null=True)
    bloodline_id = models.IntegerField(null=True)
    description = models.TextField(null=True)
    faction_id = models.IntegerField(null=True)
    gender = models.CharField(max_length=6,null=True)
    race_id = models.IntegerField(null=True)
    security_status = models.FloatField(null=True)
    title = models.CharField(max_length=50,null=True)

    def __str__(self):
        return self.name


class CharacterAccessToken(models.Model):
    character = models.OneToOneField(Character, on_delete=models.CASCADE, related_name='access_token')
    access_token = models.CharField(max_length=1500)
    refresh_token = models.CharField(max_length=50)
    issued_at = models.BigIntegerField(null=True)
    expire_time = models.BigIntegerField(null=True)
    scopes = models.CharField(max_length=3000,null=True)

    def __str__(self):
        return f"{self.character.name}'s access_token"


class CharacterApi(models.Model):
    character = models.ForeignKey(Character, on_delete=models.CASCADE, related_name='apis')
    api = models.ForeignKey(Api, on_delete=models.CASCADE)
    last_execution = models.BigIntegerField(null=True)
    last_result = models.CharField(max_length=1500,null=True)

    def __str__(self):
        return f"{self.character.name}'s activated apis"


class Corporation(models.Model):
    corporation_id = models.BigIntegerField(unique=True, primary_key=True)
    name = models.CharField(max_length=255, null=False)
    alliance_id = models.BigIntegerField(null=True)
    ceo_id = models.BigIntegerField()
    creator_id = models.BigIntegerField()
    date_founded = models.CharField(max_length=50, null=True)
    description = models.TextField(null=True)
    faction_id = models.IntegerField(null=True)
    home_station_id = models.BigIntegerField(null=True)
    member_count = models.IntegerField(null=True)
    shares = models.BigIntegerField(null=True)
    tax_rate = models.FloatField(null=False)
    ticker = models.CharField(max_length=5, null=False)
    url = models.CharField(null=True, max_length=255)
    war_eligible = models.BooleanField(null=True)

    def __str__(self):
        return self.name


class CorporationDivision(models.Model):
    corporation = models.ForeignKey(Corporation, on_delete=models.CASCADE)
    type = models.CharField(max_length=6)
    division = models.IntegerField()
    name = models.CharField(max_length=50)

    def __str__(self):
        return f"{self.corporation.name}'s division"


class CharacterWallet(models.Model):
    character = models.OneToOneField(Character, on_delete=models.CASCADE, related_name='wallet', primary_key=True)
    balance = models.FloatField(null=False)

    def __str__(self):
        return f"{self.character.name}'s wallet"


class CorporationWallet(models.Model):
    corporation = models.ForeignKey(Corporation, on_delete=models.CASCADE, related_name='wallets')
    division = models.IntegerField(null=False)
    balance = models.FloatField(null=False)

    def __str__(self):
        return f"{self.corporation.name}'s wallet"


class CharacterWalletJournal(models.Model):
    character = models.ForeignKey(Character, on_delete=models.CASCADE)
    id = models.BigIntegerField(unique=True, primary_key=True)
    amount = models.FloatField(null=False)
    balance = models.FloatField(null=False)
    context_id = models.BigIntegerField(null=True)
    context_id_type = models.CharField(null=True, max_length=50)
    date = models.CharField(null=False, max_length=50)
    description = models.TextField(null=False)
    first_party_id = models.BigIntegerField(null=False)
    reason = models.TextField()
    ref_type = models.CharField(null=False, max_length=100)
    second_party_id = models.BigIntegerField(null=False)
    tax = models.FloatField(null=True)
    tax_receiver_id = models.FloatField(null=True)

    def __str__(self):
        return f"{self.character.name}'s wallet journal entry - id {self.id}"


class CorporationWalletJournal(models.Model):
    corporation = models.ForeignKey(Corporation, on_delete=models.CASCADE)
    division_id = models.IntegerField(null=False)
    id = models.BigIntegerField(unique=True, primary_key=True)
    amount = models.FloatField(null=False)
    balance = models.FloatField(null=False)
    context_id = models.BigIntegerField(null=True)
    context_id_type = models.CharField(null=True, max_length=50)
    date = models.CharField(null=False, max_length=50)
    description = models.TextField(null=False)
    first_party_id = models.BigIntegerField(null=False)
    reason = models.TextField()
    ref_type = models.CharField(null=False, max_length=100)
    second_party_id = models.BigIntegerField(null=False)
    tax = models.FloatField(null=True)
    tax_receiver_id = models.FloatField(null=True)

    def __str__(self):
        return f"{self.corporation.name}'s wallet journal entry"


class CharacterWalletTransaction(models.Model):
    character = models.ForeignKey(Character, on_delete=models.CASCADE)
    client_id = models.BigIntegerField()
    date = models.CharField(null=False, max_length=50)
    is_buy = models.BooleanField()
    is_personal = models.BooleanField()
    journal_ref_id = models.BigIntegerField()
    location_id = models.BigIntegerField()
    quantity = models.BigIntegerField()
    transaction_id = models.BigIntegerField(primary_key=True)
    type_id = models.BigIntegerField()
    unit_price = models.FloatField()

    def __str__(self):
        return f"{self.character.name}'s wallet transaction entry"


class CorporationWalletTransaction(models.Model):
    corporation = models.ForeignKey(Corporation, on_delete=models.CASCADE)
    division_id = models.IntegerField(null=False)
    client_id = models.BigIntegerField()
    date = models.CharField(null=False, max_length=50)
    is_buy = models.BooleanField()
    journal_ref_id = models.BigIntegerField()
    location_id = models.BigIntegerField()
    quantity = models.BigIntegerField()
    transaction_id = models.BigIntegerField(primary_key=True)
    type_id = models.BigIntegerField()
    unit_price = models.FloatField()

    def __str__(self):
        return f"{self.corporation.name}'s wallet transaction entry"

