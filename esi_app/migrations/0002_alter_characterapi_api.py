# Generated by Django 4.2 on 2023-04-19 23:17

from django.db import migrations, models
import django.db.models.deletion


class Migration(migrations.Migration):

    dependencies = [
        ('esi_app', '0001_initial'),
    ]

    operations = [
        migrations.AlterField(
            model_name='characterapi',
            name='api',
            field=models.ForeignKey(on_delete=django.db.models.deletion.CASCADE, to='esi_app.api'),
        ),
    ]
