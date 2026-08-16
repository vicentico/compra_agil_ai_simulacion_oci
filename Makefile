# ==========================================================================
# PPIP — Makefile (FASE 1: Docker infrastructure; FASE 2: observability)
# Ver infrastructure/docker/README.md para el detalle de cada decisión.
# ==========================================================================
# Los dos -f son obligatorios: al pasar -f explícito, Docker Compose deja de
# auto-descubrir docker-compose.override.yml (solo lo hace sin -f). Sin la
# segunda ruta, los puertos de desarrollo (Mongo/Redis/.../Grafana/Prometheus)
# nunca se publican en el host — defecto pre-existente detectado en FASE 2.
COMPOSE := docker compose --project-directory infrastructure/docker -f infrastructure/docker/docker-compose.yml -f infrastructure/docker/docker-compose.override.yml --env-file infrastructure/docker/.env
SERVICE ?=

.PHONY: help env up up-core up-obs down ps ps-obs logs build smoke smoke-obs seed clean

help:
	@echo "Targets: env, up, up-core, up-obs, down, ps, ps-obs, logs [SERVICE=nombre], build, smoke, smoke-obs, seed, clean"

env:
	@test -f infrastructure/docker/.env || cp infrastructure/docker/.env.example infrastructure/docker/.env
	@echo "infrastructure/docker/.env listo (editar credenciales antes de 'make up' si es necesario)."

up: env
	COMPOSE_PROFILES=core,app $(COMPOSE) up -d --build

up-core: env
	COMPOSE_PROFILES=core $(COMPOSE) up -d

# Perfil independiente (FASE 2): añade OTel Collector + Prometheus + Loki +
# Tempo + Grafana sobre core/app ya levantados. No es requisito de `up`.
up-obs: env
	COMPOSE_PROFILES=obs $(COMPOSE) up -d

down:
	COMPOSE_PROFILES=core,app,obs,demo $(COMPOSE) down

ps:
	COMPOSE_PROFILES=core,app $(COMPOSE) ps

ps-obs:
	COMPOSE_PROFILES=obs $(COMPOSE) ps

logs:
	COMPOSE_PROFILES=core,app,obs $(COMPOSE) logs -f --tail=200 $(SERVICE)

build:
	COMPOSE_PROFILES=core,app $(COMPOSE) build

smoke: env
	bash scripts/smoke-test.sh

# otel-collector no tiene healthcheck (imagen distroless, ver docker-compose.yml)
# y queda fuera a propósito: smoke-test.sh falla ante cualquier servicio sin
# estado "healthy".
smoke-obs: env
	SMOKE_PROFILES=obs SMOKE_EXCLUDE=otel-collector bash scripts/smoke-test.sh

seed: env
	@echo "Perfil demo: placeholder no funcional todavía (ver scripts/seed/seed.py y docs/16-operations/01-operations.md)."
	COMPOSE_PROFILES=demo $(COMPOSE) run --rm seed

clean:
	COMPOSE_PROFILES=core,app,obs,demo $(COMPOSE) down -v
