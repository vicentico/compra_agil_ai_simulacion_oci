# ==========================================================================
# PPIP — Makefile (FASE 1: Docker infrastructure)
# Ver infrastructure/docker/README.md para el detalle de cada decisión.
# ==========================================================================
COMPOSE := docker compose --project-directory infrastructure/docker -f infrastructure/docker/docker-compose.yml --env-file infrastructure/docker/.env
SERVICE ?=

.PHONY: help env up up-core down ps logs build smoke seed clean

help:
	@echo "Targets: env, up, up-core, down, ps, logs [SERVICE=nombre], build, smoke, seed, clean"

env:
	@test -f infrastructure/docker/.env || cp infrastructure/docker/.env.example infrastructure/docker/.env
	@echo "infrastructure/docker/.env listo (editar credenciales antes de 'make up' si es necesario)."

up: env
	COMPOSE_PROFILES=core,app $(COMPOSE) up -d --build

up-core: env
	COMPOSE_PROFILES=core $(COMPOSE) up -d

down:
	COMPOSE_PROFILES=core,app,demo $(COMPOSE) down

ps:
	COMPOSE_PROFILES=core,app $(COMPOSE) ps

logs:
	COMPOSE_PROFILES=core,app $(COMPOSE) logs -f --tail=200 $(SERVICE)

build:
	COMPOSE_PROFILES=core,app $(COMPOSE) build

smoke: env
	bash scripts/smoke-test.sh

seed: env
	@echo "Perfil demo: placeholder no funcional todavía (ver scripts/seed/seed.py y docs/16-operations/01-operations.md)."
	COMPOSE_PROFILES=demo $(COMPOSE) run --rm seed

clean:
	COMPOSE_PROFILES=core,app,demo $(COMPOSE) down -v
