#!/usr/bin/env bash
# ==========================================================================
# PPIP — smoke test de FASE 1/2 (ComposeHealthSmokeTest, docs/18-traceability).
# Falla (exit != 0) si algún contenedor de los perfiles indicados no está
# "healthy" dentro del timeout. Pensado para `make smoke` (core+app, default)
# o `make smoke-obs` (perfil obs; excluye otel-collector, que no tiene
# healthcheck por ser una imagen distroless — ver docker-compose.yml).
# ==========================================================================
set -euo pipefail

COMPOSE_FILE="infrastructure/docker/docker-compose.yml"
OVERRIDE_FILE="infrastructure/docker/docker-compose.override.yml"
ENV_FILE="infrastructure/docker/.env"
TIMEOUT_SECONDS="${SMOKE_TIMEOUT:-180}"
POLL_INTERVAL=5
SMOKE_PROFILES="${SMOKE_PROFILES:-core,app}"
SMOKE_EXCLUDE="${SMOKE_EXCLUDE:-}"

compose() {
  # -f explícito en ambos archivos: ver nota en Makefile sobre por qué
  # docker-compose.override.yml no se auto-descubre en este modo.
  COMPOSE_PROFILES="$SMOKE_PROFILES" docker compose \
    --project-directory infrastructure/docker \
    -f "$COMPOSE_FILE" -f "$OVERRIDE_FILE" --env-file "$ENV_FILE" "$@"
}

echo "[smoke] esperando hasta ${TIMEOUT_SECONDS}s a que todos los servicios (perfiles ${SMOKE_PROFILES}) estén healthy…"

elapsed=0
while true; do
  # Formato: "<nombre> <estado_salud>". Todo servicio de docker-compose.yml
  # tiene healthcheck salvo las excepciones documentadas ahí mismo (p.ej.
  # otel-collector); esas excepciones se filtran vía SMOKE_EXCLUDE, no
  # ignorando su ausencia de estado en silencio.
  # docker compose ps --format json: distintas versiones emiten NDJSON
  # (un objeto por línea) o un único array JSON. Se soportan ambos formatos
  # sin depender de una versión específica del CLI.
  statuses="$(compose ps --format json | SMOKE_EXCLUDE="$SMOKE_EXCLUDE" python3 -c '
import json, os, sys

exclude = [s.strip() for s in os.environ.get("SMOKE_EXCLUDE", "").split(",") if s.strip()]

raw = sys.stdin.read().strip()
services = []
if not raw:
    services = []
else:
    try:
        parsed = json.loads(raw)
        services = parsed if isinstance(parsed, list) else [parsed]
    except json.JSONDecodeError:
        for line in raw.splitlines():
            line = line.strip()
            if line:
                services.append(json.loads(line))

for svc in services:
    name = svc.get("Service", svc.get("Name", "?"))
    if any(x in name for x in exclude):
        continue
    print(svc.get("Name", "?"), svc.get("Health", "none"))
')"

  total=$(echo "$statuses" | grep -c . || true)
  healthy=$(echo "$statuses" | awk '$2=="healthy"' | grep -c . || true)
  unhealthy=$(echo "$statuses" | awk '$2=="unhealthy"' | grep -c . || true)

  echo "[smoke] ${healthy}/${total} healthy (unhealthy: ${unhealthy}) — ${elapsed}s transcurridos"

  if [ "$unhealthy" -gt 0 ]; then
    echo "[smoke] FALLO: hay servicios unhealthy:"
    echo "$statuses" | awk '$2=="unhealthy"'
    exit 1
  fi

  if [ "$total" -gt 0 ] && [ "$healthy" -eq "$total" ]; then
    echo "[smoke] OK: todos los servicios (${total}) están healthy."
    exit 0
  fi

  if [ "$elapsed" -ge "$TIMEOUT_SECONDS" ]; then
    echo "[smoke] FALLO: timeout de ${TIMEOUT_SECONDS}s alcanzado. Estado final:"
    echo "$statuses"
    exit 1
  fi

  sleep "$POLL_INTERVAL"
  elapsed=$((elapsed + POLL_INTERVAL))
done
