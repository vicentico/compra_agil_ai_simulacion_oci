# ROADMAP de implementación

Orden según MASTER PROMPT 2 §35. Cada fase cumple el Definition of Done (MP2 §34) y actualiza la matriz de trazabilidad. Nunca REQUISITO → CÓDIGO directo: siempre especificación y diseño primero.

| Fase | Contenido | Entregable verificable | Estado |
|---|---|---|---|
| **0** | Repository + documentation foundation (este bootstrap) | docs/ completa + Architecture Review Package aprobado | ✅ Hecha (pendiente revisión humana) |
| **1** | Docker infrastructure: compose con MongoDB, RabbitMQ, MinIO, Redis, Qdrant, Keycloak, Ollama, Traefik + esqueletos .NET 10 / Angular 20 | `docker compose up -d` todo healthy | Pendiente |
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

## Gates

- **Gate FASE 0→1**: Architecture Review Package coherente y aprobado ([architecture-review-package.md](architecture-review-package.md)).
- **Gate por fase**: Definition of Done completo; preguntas abiertas de la fase cerradas o replanificadas explícitamente en 01-discovery/09-open-questions.md.
- **Revisión de boundaries** (ADR-012): al cierre de FASE 10, 15 y 18.
