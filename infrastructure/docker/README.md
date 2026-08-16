# infrastructure/docker — FASE 1: Docker infrastructure · FASE 2: Observability

Implementa `docs/04-architecture/04-deployment-diagram.md`. Levanta la infraestructura base (perfil `core`), los esqueletos de aplicación (perfil `app`) y, opcionalmente, el stack de observabilidad (perfil `obs`) definidos en el Container Diagram.

## Uso rápido

```bash
cp infrastructure/docker/.env.example infrastructure/docker/.env
# editar .env con credenciales locales (nunca reales/productivas)
make up          # perfiles core + app
make ps          # estado de salud de cada contenedor
make smoke        # scripts/smoke-test.sh: falla si algo no está healthy
make up-obs      # perfil obs (independiente): OTel Collector, Prometheus, Loki, Tempo, Grafana
make smoke-obs   # ídem para el perfil obs
make logs SERVICE=platform-api
make down
```

Rutas vía Traefik (resolución automática de `*.localhost`, RFC 6761 — no requiere editar `/etc/hosts`):

| URL | Servicio |
|---|---|
| http://ppip.localhost | Frontend Angular |
| http://api.ppip.localhost | Platform API (`/health`, `/ready`, `/api/diagnostics/trace-check`) |
| http://auth.ppip.localhost | Keycloak |
| http://grafana.ppip.localhost | Grafana (perfil `obs`) — dashboard `PPIP - Service Overview` provisionado |
| http://localhost:8080 | Dashboard de Traefik (solo dev, inseguro — ver `docker-compose.override.yml`) |
| http://localhost:9090, :3100, :3200 | Prometheus, Loki, Tempo (solo dev, expuestos por `docker-compose.override.yml`) |

## Observabilidad (perfil `obs`, FASE 2)

Los 4 servicios .NET exportan traces+métricas+logs vía OTLP al Collector (`Ppip__Otel__Endpoint`); el Collector reparte a Prometheus (scrape), Loki y Tempo. **No es una dependencia dura**: `core`/`app` arrancan y sirven tráfico igual si `obs` no está activo (los exporters OTLP fallan en silencio en background, sin bloquear el arranque).

Para verificar el trace end-to-end (criterio de éxito de FASE 2): `curl http://api.ppip.localhost/api/diagnostics/trace-check` — llama a los 3 workers, y el `correlationId`/`traceId` devueltos deben aparecer correlacionados en Grafana (panel "Logs recientes" del dashboard, y en Tempo vía "Explore" buscando ese traceId).

**otel-collector no tiene `HEALTHCHECK` de Docker** — su imagen (`otel/opentelemetry-collector-contrib`) es distroless (sin `sh`/`wget`/`curl`), no hay binario invocable para un `CMD`. Se verifica indirectamente: si Prometheus/Loki/Tempo reciben datos, el Collector está sano. `make smoke-obs` lo excluye explícitamente del chequeo de salud (no lo ignora en silencio).

## Decisiones y límites explícitos de esta fase (transparencia de trade-offs)

1. **Sin stack de observabilidad todavía.** OTel Collector/Prometheus/Grafana/Loki son alcance de **FASE 2** (`docs/ROADMAP.md`); esta fase solo reserva la red `obs`. Mezclarlos ahora violaría la disciplina de fases del proyecto.
2. **Keycloak en modo `start-dev`** (almacenamiento `dev-file`, sin Postgres) — válido para POC local; no usar así en producción/OCI. PostgreSQL se incorpora recién en FASE 15 (ADR-002); no se introduce antes solo para Keycloak.
3. **Sin usuarios de aplicación con privilegio mínimo en MongoDB todavía** — se usan credenciales root vía `.env` porque aún no existen colecciones/repositorios reales (eso es FASE 4+). Se documenta como TODO explícito, no como omisión oculta.
4. **`/ready` de los 4 servicios .NET verifica dependencias reales** (Mongo, Redis, RabbitMQ, MinIO, Qdrant, Ollama según corresponda a cada servicio) — no es un stub que siempre responde 200. Esto es intencional: el criterio de éxito de FASE 1 es demostrar que la topología de red y las credenciales están correctamente cableadas, no solo que el proceso arrancó.
5. **Perfil `demo` es un placeholder.** El seeding real (FR-052) depende de dominio que no existe hasta FASE 4+; el contenedor existe pero documenta explícitamente que no hace nada todavía.
6. **Tags de imágenes no fijados por dígest** (se usan tags estables conocidos o `latest` documentado). Fijar por dígest queda como tarea de endurecimiento antes de FASE 19 (migración/producción-like) — ver comentarios en `docker-compose.yml`.
7. **Traefik monta el socket Docker** (`/var/run/docker.sock`) para descubrimiento de contenedores — patrón estándar de desarrollo local; en OCI el edge es un servicio gestionado (ver `docs/04-architecture/11-oci-mapping.md`), no un símil de este mecanismo.
