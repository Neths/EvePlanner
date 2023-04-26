FROM python:3-slim-bullseye
LABEL authors="Neths"

RUN apt-get update && apt-get install -y cron

RUN mkdir /etc/eve-planner

WORKDIR /etc/eve-planner

COPY requirements.txt ./
RUN pip install -r requirements.txt
COPY . .

ENV PYTHONDONTWRITEBYTECODE=1
ENV PYTHONUNBUFFERED=1

EXPOSE 8000

CMD ["/etc/eve-planner/entrypoint.sh"]