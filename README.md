# Public Procurement Intelligence Platform
### OCI Local Simulator + ChileCompra Compra Ágil Intelligence Platform

Plataforma de inteligencia de compras públicas que sincroniza **Compras Ágiles** desde la API oficial de ChileCompra / Mercado Público, procesa sus documentos (extracción de texto, OCR, chunking semántico, embeddings), construye un sistema **RAG por Compra Ágil**, analiza cada proceso con IA auditable, extrae requisitos con evidencia documental y genera **propuestas comerciales/técnicas editables** con evaluación automática de compliance.

Todo el sistema se ejecuta localmente mediante Docker Compose y simula conceptualmente una arquitectura sobre **Oracle Cloud Infrastructure (OCI)**, con una ruta de migración explícita.

## Estado del proyecto

**FASE 0 aprobada** · **FASE 1-6 implementadas** (Docker infra, Observability, Identity & security, Procurement domain, ChileCompra integration, Incremental synchronization) (2026-08-17) — próxima: **FASE 7 — Document storage**.
Especificación completa en [docs/](docs/); infraestructura y esqueletos ejecutables en `infrastructure/docker/` y `src/`. UC-001 (sincronización incremental) completo e idempotente, validado con Testcontainers (Mongo/Redis reales): `Ppip.Procurement.Domain` (49 tests) + `Ppip.Procurement.Application` (`SyncOrchestrator`, 19 tests) + `Ppip.Procurement.Infrastructure` (cliente ChileCompra + persistencia Mongo + lock Redis + outbox dispatcher RabbitMQ, 29 tests) + `Ppip.SyncWorker`. Ver [docs/ROADMAP.md](docs/ROADMAP.md) (incluye qué se validó en cada fase).

## Arrancar FASE 1 + 2 + 3

```bash
cp infrastructure/docker/.env.example infrastructure/docker/.env   # editar credenciales locales
make up          # perfiles core + app (build de imágenes incluido) — incluye Keycloak con realm `ppip`
make smoke       # falla si algo no queda healthy
make up-obs      # perfil obs: OTel Collector, Prometheus, Loki, Tempo, Grafana
make smoke-obs   # ídem, para el perfil obs
```
Detalle y decisiones documentadas: [infrastructure/docker/README.md](infrastructure/docker/README.md).

## Principio rector

> El sistema debe poder comprenderse completamente desde `docs/` sin leer el código.

## Mapa de la documentación

| Área | Ruta |
|---|---|
| Descubrimiento: objetivos, alcance, requisitos (FR/NFR), riesgos | [docs/01-discovery/](docs/01-discovery/) |
| Casos de uso UC-001…UC-009 | [docs/02-use-cases/](docs/02-use-cases/) |
| Modelo de dominio y bounded contexts | [docs/03-domain/](docs/03-domain/) |
| Arquitectura y diagramas (C4, flujos, OCI mapping) | [docs/04-architecture/](docs/04-architecture/) |
| Decisiones arquitectónicas (ADR-001…ADR-012) | [docs/05-architecture-decisions/](docs/05-architecture-decisions/) |
| Contratos de API | [docs/06-api/](docs/06-api/) |
| Contratos de eventos | [docs/07-events/](docs/07-events/) |
| Arquitectura de datos, source of truth, lineage | [docs/08-data/](docs/08-data/) |
| Document intelligence (descarga, OCR, chunking) | [docs/09-document-intelligence/](docs/09-document-intelligence/) |
| Especificación RAG | [docs/10-rag/](docs/10-rag/) |
| Gobernanza de IA y versionado de prompts | [docs/11-ai/](docs/11-ai/) |
| Seguridad y threat model | [docs/12-security/](docs/12-security/) |
| Observabilidad | [docs/13-observability/](docs/13-observability/) |
| Confiabilidad, fallos e idempotencia | [docs/14-reliability/](docs/14-reliability/) |
| Estrategia de testing | [docs/15-testing/](docs/15-testing/) |
| Operaciones, demo mode, seeding | [docs/16-operations/](docs/16-operations/) |
| Migración a OCI | [docs/17-oci-migration/](docs/17-oci-migration/) |
| Matriz de trazabilidad requisito→código→test | [docs/18-traceability/](docs/18-traceability/) |

Documentos raíz de síntesis: [ARCHITECTURE.md](ARCHITECTURE.md) · [DOMAIN.md](DOMAIN.md) · [DATA.md](DATA.md) · [SECURITY.md](SECURITY.md) · [AI.md](AI.md) · [RAG.md](RAG.md) · [OPERATIONS.md](OPERATIONS.md) · [OCI-MIGRATION.md](OCI-MIGRATION.md)

## Estructura del repositorio

```
/docs            Documentación (artefacto de primera clase)
/src             Código fuente (.NET 10 backend, Angular 20 frontend) — FASE 1+
/tests           Pruebas — FASE 1+
/prompts         Prompts versionados por dominio (system/analysis/requirements/rag/proposal/compliance)
/evaluation      Datasets y resultados de evaluación de IA/RAG
/infrastructure  Docker Compose, Dockerfiles, configuración de entorno
/scripts         Scripts operativos y de seeding (/scripts/seed)
```

## Stack objetivo (resumen)

.NET 10 · Angular 20 · MongoDB · PostgreSQL · Qdrant · Redis · MinIO · RabbitMQ · Keycloak · Ollama/OpenAI/Gemini (abstracción) · OpenTelemetry · Prometheus · Grafana · Loki · Traefik · Docker Compose.

## Cómo empezar (FASE 0)

1. Leer [docs/01-discovery/01-objectives.md](docs/01-discovery/01-objectives.md).
2. Leer [docs/04-architecture/00-architecture-overview.md](docs/04-architecture/00-architecture-overview.md).
3. Revisar los ADRs en [docs/05-architecture-decisions/](docs/05-architecture-decisions/).
4. Validar el [Architecture Review Package](docs/architecture-review-package.md) antes de iniciar FASE 1.
