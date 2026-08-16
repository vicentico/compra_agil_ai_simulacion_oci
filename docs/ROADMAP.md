# ROADMAP de implementación

Orden según MASTER PROMPT 2 §35. Cada fase cumple el Definition of Done (MP2 §34) y actualiza la matriz de trazabilidad. Nunca REQUISITO → CÓDIGO directo: siempre especificación y diseño primero.

| Fase | Contenido | Entregable verificable | Estado |
|---|---|---|---|
| **0** | Repository + documentation foundation (este bootstrap) | docs/ completa + Architecture Review Package aprobado | ✅ Aprobada (gate 2026-08-16, incluye cambio «Propuesta de Plataforma» como SHOULD) |
| **1** | Docker infrastructure: compose con MongoDB, RabbitMQ, MinIO, Redis, Qdrant, Keycloak, Ollama, Traefik + esqueletos .NET 10 / Angular 20 | `docker compose up -d` todo healthy | ✅ Implementada (2026-08-16) — ver nota abajo |
| **2** | Observability foundation: OTel en esqueletos, Collector, Prometheus, Grafana, Loki, dashboards base | Trace end-to-end visible de un request de prueba | ✅ Implementada (2026-08-16) — ver nota abajo |
| **3** | Identity & security: realm Keycloak, JWT en API, RBAC, secretos, rate limiting | AuthZ matrix tests verdes | Pendiente |
| **4** | Procurement domain: dominio + building blocks (outbox, envelope de eventos, idempotencia) + architecture tests | Dominio testeado sin infraestructura | Pendiente |
| **5** | ChileCompra integration: client resiliente + contract tests + spike API real (cierra OQ-01/02, ASM-01/08) | Fixtures reales grabados; manejo de errores probado | Pendiente |
| **6** | Incremental synchronization: SyncWorker + checkpoint + eventos | UC-001 completo e idempotente | Pendiente |
| **7** | Document storage: descarga validada (SSRF), MinIO, versionado por hash | UC-003 etapas 1-3 | Pendiente |
| **8** | Document intelligence: clasificación, extracción, OCR, chunking | UC-003 etapas 4-9 | Pendiente |
| **9** | RAG: embeddings, Qdrant, pipeline de retrieval, evidencia (cierra OQ-03) | UC-005 + evaluación retrieval | Pendiente |
| **10** | AI analysis: análisis estructurado con schemas y evaluación | UC-004 parte 1 | Pendiente |
| **11** | Requirements engine: extracción + revisión humana | UC-004 parte 2, matriz de requisitos | Pendiente |
| **12** | Company profile | CRUD + validaciones + auditoría | Pendiente |
| **13** | Proposal management: plantilla, generación, editor versionado | UC-006/007 | Pendiente |
| **14** | Compliance: rule engine + asistencia LLM | UC-008 | Pendiente |
| **15** | Audit & traceability: trace graph, PostgreSQL reporting (cierra OQ-04) | UC-009 navegable | Pendiente |
| **16** | Angular UX completa: todos los features/módulos | Guion de demo ejecutable por UI | Pendiente |
| **17** | End-to-end validation: e2e Playwright + demo mode pulido | Pipeline demo verde en CI | Pendiente |
| **18** | Performance & resilience: baseline k6, fault injection F1-F16, SLOs | Informe de baseline + resiliencia | Pendiente |
| **19** | OCI migration simulation: arquitectura objetivo, IaC esqueleto, gap analysis | Paquete de migración revisable | Pendiente |

## Asignación de funcionalidades SHOULD (Propuesta de Plataforma, 2026-08-16)

Se implementan en su fase natural **solo si el núcleo MUST de esa fase está estable**; si no, se difieren explícitamente (nunca recorte silencioso):

| FR | Funcionalidad | Fase objetivo |
|---|---|---|
| FR-053/054 | Human-in-the-Loop de extracción (tareas de revisión + carga manual) | FASE 8 (document intelligence) |
| FR-055/056 | Inferencia de rubros por LLM + auditoría del usuario | FASE 12 (company profile) |
| FR-057 | Dashboard de oportunidades por matching de rubros | FASE 12 (matching) + FASE 16 (UI) — depende de OQ-09 (taxonomía, spike FASE 5) |
| FR-058 | Export de propuesta a .docx editable | FASE 13 (proposal management) |
| FR-059/060 | Outcomes de propuestas + dashboard de efectividad | FASE 13 (outcome) + FASE 16 (dashboard) |
| FR-061 | Score de ganabilidad heurístico explicable | FASE 12 (junto al matching) + FASE 16 (UI) |
| FR-063/064 | Monitoreo proactivo + notificaciones (in-app + email digest) | FASE 6 (detección post-sync) + FASE 16 (centro de notificaciones y SMTP/MailHog) |
| FR-065/066 | Throttling dinámico + rol superadmin y panel de cuotas | FASE 5 (rate limiter dinámico en el client) + FASE 3 (rol) + FASE 16 (panel) |

FUTURE explícito: FR-062 (recalibración ML del score) — se especificará cuando exista volumen real de outcomes (RSK-14).

## Nota de cierre de FASE 1 (2026-08-16)

Implementado y validado en el entorno de build: `infrastructure/docker/docker-compose.yml` (14 servicios, perfiles `core`/`app`/`demo`, redes segmentadas edge/app/data/obs-reservada) + `docker-compose.override.yml` de dev; esqueletos `Ppip.PlatformApi`, `Ppip.SyncWorker`, `Ppip.DocumentWorker`, `Ppip.AiWorker` (.NET 10, `/health` liveness + `/ready` verificando dependencias reales); `Ppip.BuildingBlocks.Health` compartido; frontend Angular 20.3 generado con el CLI oficial (build y 3 tests unitarios en verde); `Makefile` + `scripts/smoke-test.sh`.

**Validado:** `docker compose config` (YAML/interpolación/perfiles/rutas de build, 14 servicios), XML de los 5 `.csproj`, balance de código de los `.cs`, `ng build`/`ng test` reales del frontend (compilan y pasan).
**No validado — limitación del entorno que generó el scaffold, no del código:** build real de las 5 imágenes Docker y arranque end-to-end de los contenedores, porque ese entorno no tenía acceso de red a Docker Hub ni el SDK de .NET 10 instalado. **Primera acción recomendada al retomar en un entorno con Docker Desktop/SDK real: `make up && make smoke`** y corregir lo que la validación estática no pudo anticipar (típicamente: pequeños ajustes de healthcheck o versión de imagen).
**Deliberadamente fuera de esta fase:** stack de observabilidad (FASE 2), seeding real (`scripts/seed` es un placeholder que falla explícitamente), usuarios Mongo con privilegio mínimo (FASE 4+). Detalle completo de cada decisión y su trade-off en `infrastructure/docker/README.md`.

**Corrección (2026-08-16, al iniciar FASE 2):** el entorno que retomó el trabajo sí tenía Docker Desktop y el SDK de .NET 10, y la validación real expuso 3 defectos pre-existentes de este scaffold (nunca compilados hasta ahora): (1) `Ppip.BuildingBlocks.Health.csproj` no referenciaba el shared framework de ASP.NET Core, por lo que `HealthCheckResult`/`IHealthCheck` no resolvían; (2) las 3 registraciones `AddCheck(name, sp => new HttpEndpointHealthCheck(...), tags)` usaban una sobrecarga de `AddCheck` que no existe (se corrigió a `AddTypeActivatedCheck<T>`); (3) los 4 Dockerfiles no copiaban `src/Directory.Build.props`/`src/global.json`, dejando `TargetFramework` vacío dentro del contexto de build, y creaban un usuario `appuser` con UID 1654 que colisiona con el usuario `app` que ya trae la imagen base `mcr.microsoft.com/dotnet/aspnet:10.0`. Los 3 se corrigieron como parte de FASE 2 (no eran parte del alcance nuevo, pero bloqueaban validar cualquier cosa). Las 5 imágenes compilan y los 4 servicios .NET compilan limpio (`dotnet build`) tras la corrección.

## Nota de cierre de FASE 2 (2026-08-16)

Implementado y validado con Docker Desktop + .NET 10 SDK reales (a diferencia de FASE 1, este entorno sí tenía ambos): perfil `obs` en `docker-compose.yml` (OTel Collector, Prometheus, Loki, Tempo, Grafana — 5 servicios, red `obs`); `Ppip.BuildingBlocks.Observability` (traces+métricas+logs vía OTLP al Collector, `CorrelationIdMiddleware` + `CorrelationIdDelegatingHandler` per docs/06-api/00) referenciado desde los 4 servicios .NET; endpoint temporal `GET /api/diagnostics/trace-check` en Platform API que llama a los 3 workers para demostrar el trace end-to-end exigido por el criterio de éxito de la fase; dashboard `PPIP - Service Overview` provisionado en Grafana; decisión Tempo vs Jaeger resuelta a favor de Tempo (ADR-011 Amendment). 4 tests unitarios xUnit para `CorrelationIdMiddleware` (`tests/Ppip.BuildingBlocks.Observability.Tests`) — detectaron y permitieron corregir 2 bugs reales antes de integrar (header en blanco no se generaba de nuevo; el header de respuesta se fijaba vía `OnStarting`, que no se dispara de forma fiable sin un servidor real).

Validar el stack completo levantado expuso 3 defectos adicionales, también corregidos: (4) `Makefile`/`scripts/smoke-test.sh` invocaban `docker compose` con `-f docker-compose.yml` explícito, lo que **desactiva** el auto-descubrimiento de `docker-compose.override.yml` — los puertos de desarrollo (Mongo, Redis, RabbitMQ, MinIO, Qdrant, Ollama y ahora Grafana/Prometheus/Loki/Tempo) nunca se habían publicado realmente en el host desde FASE 1; se agregó el segundo `-f` explícito en ambos scripts. (5) `prometheus.yml` sin `honor_labels: true` hacía que Prometheus renombrara la label `job` real (nombre del servicio) a `exported_job` por colisión con la del propio `scrape_config` — se corrigió, y ahora `job` distingue cada servicio tal como espera el dashboard. (6) `datasources.yaml` de Grafana: el campo `url` del derived field de Loki (`${__value.raw}`) se guardaba vacío porque el provisioner de Grafana expande `$VAR`/`${VAR}` como variables de entorno al leer el YAML; se corrigió escapando a `$${__value.raw}`.

**Validado end-to-end, con el stack completo arriba (`docker compose up -d`, perfiles `core`+`app`+`obs`, 18 contenedores) en este mismo entorno:** las 17 imágenes relevantes healthy (Traefik quedó en `Created` — puerto 80 ya ocupado en este host por otro proceso, conflicto del entorno local, no del compose); `GET /api/diagnostics/trace-check` devolvió 200 con los 3 workers healthy; el mismo `traceId` apareció con sus 7 spans completos (4 en Platform API, 1 por worker) consultando la API HTTP de Tempo directamente; el mismo `correlationId`+`traceId` aparecieron en los logs estructurados de Loki (`job=ppip-platform-api`); Prometheus mostró `http_server_request_duration_seconds_count` separado por `job` para los 4 servicios; Grafana provisionó los 3 datasources (con correlación Loki↔Tempo↔Prometheus) y el dashboard `PPIP - Service Overview` con sus 5 paneles.
**Deliberadamente fuera de esta fase (recorte explícito):** los 4 dashboards de negocio (dependen de métricas `ppip_*` que no existen hasta sus fases — FASE 6/8/9-10); alertas iniciales (misma razón); propagación de correlationId en eventos RabbitMQ (ningún worker publica eventos reales todavía). Detalle en `docs/13-observability/01-observability-spec.md`.

## Gates

- **Gate FASE 0→1**: Architecture Review Package coherente y aprobado ([architecture-review-package.md](architecture-review-package.md)).
- **Gate por fase**: Definition of Done completo; preguntas abiertas de la fase cerradas o replanificadas explícitamente en 01-discovery/09-open-questions.md.
- **Revisión de boundaries** (ADR-012): al cierre de FASE 10, 15 y 18.
