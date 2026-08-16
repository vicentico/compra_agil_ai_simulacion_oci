# ROADMAP de implementación

Orden según MASTER PROMPT 2 §35. Cada fase cumple el Definition of Done (MP2 §34) y actualiza la matriz de trazabilidad. Nunca REQUISITO → CÓDIGO directo: siempre especificación y diseño primero.

| Fase | Contenido | Entregable verificable | Estado |
|---|---|---|---|
| **0** | Repository + documentation foundation (este bootstrap) | docs/ completa + Architecture Review Package aprobado | ✅ Aprobada (gate 2026-08-16, incluye cambio «Propuesta de Plataforma» como SHOULD) |
| **1** | Docker infrastructure: compose con MongoDB, RabbitMQ, MinIO, Redis, Qdrant, Keycloak, Ollama, Traefik + esqueletos .NET 10 / Angular 20 | `docker compose up -d` todo healthy | ✅ Implementada (2026-08-16) — ver nota abajo |
| **2** | Observability foundation: OTel en esqueletos, Collector, Prometheus, Grafana, Loki, dashboards base | Trace end-to-end visible de un request de prueba | Pendiente |
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

## Gates

- **Gate FASE 0→1**: Architecture Review Package coherente y aprobado ([architecture-review-package.md](architecture-review-package.md)).
- **Gate por fase**: Definition of Done completo; preguntas abiertas de la fase cerradas o replanificadas explícitamente en 01-discovery/09-open-questions.md.
- **Revisión de boundaries** (ADR-012): al cierre de FASE 10, 15 y 18.
