# Documento Maestro de Traspaso de Contexto (Context Handover)
## Public Procurement Intelligence Platform — OCI Local Simulator + ChileCompra Compra Ágil

| Metadato | Valor |
|---|---|
| Fecha de traspaso | 2026-08-16 |
| Rol que se traspasa | Tech Lead / Arquitecto del proyecto (agente IA con supervisión del usuario) |
| Ubicación del repositorio | `C:\ClaudeCowork\Agente_Compras_Agiles` (raíz del repo = carpeta del proyecto) |
| Sesión de origen | Claude Cowork, modelo configurado `claude-fable-5` |
| Estado global | **FASE 0 aprobada · FASE 1 (Docker infrastructure) implementada · FASE 2 (Observability foundation) implementada y validada end-to-end** (2026-08-16). Próxima: FASE 3 — Identity & security. |
| Documentos rectores | `Master Prompt — OCI Local Simulator...md` (QUÉ construir) + MASTER PROMPT 2 (CÓMO construirlo — entregado por chat, ver §3.6 de este documento) |

> **Instrucción para el asistente entrante:** lee este documento completo, luego `README.md`, `docs/ROADMAP.md` y `docs/architecture-review-package.md`. Con eso puedes asumir el rol sin preguntas repetitivas. Ante cualquier duda de detalle, la respuesta está en `docs/` — este proyecto se rige por el principio "el sistema debe comprenderse desde docs/ sin leer el código".

---

## 1. Resumen del Proyecto

**Nombre provisional:** Public Procurement Intelligence Platform (PPIP).

**Objetivo:** Proof of Concept ejecutable 100% en local con Docker Compose que simula una arquitectura empresarial sobre Oracle Cloud Infrastructure (OCI), usando como fuente real la **API pública de Compra Ágil de ChileCompra / Mercado Público (v2)**. El flujo completo: sincronización incremental de Compras Ágiles → descarga y procesamiento de documentos (clasificación PDF, extracción, OCR condicional, chunking semántico, embeddings) → RAG por compra con evidencia citada → análisis IA estructurado y extracción de requisitos → matching por rubros del perfil de empresa → generación de propuesta comercial/técnica editable y versionada → evaluación de compliance → trazabilidad end-to-end auditable → ruta documentada de migración a OCI.

**Valor de negocio:** reducir fricción de onboarding (rubros inferidos por LLM), eliminar tiempo de búsqueda (matching + score de ganabilidad + notificaciones proactivas), acelerar la postulación (propuesta generada on-demand, exportable a .docx) y evidenciar ROI (dashboard de efectividad/win-rate). La IA es **decision support, no decisor autónomo**: siempre human-in-the-loop.

**Pregunta que el proyecto debe responder:** «¿Cómo construiría una plataforma empresarial de Procurement Intelligence sobre OCI, partiendo de un entorno Docker local?» — el repo (docs + arquitectura + código + tests + trazabilidad) es conjuntamente la respuesta.

## 2. Arquitectura y Stack

### 2.1 Estilo arquitectónico (ADR-001)
**Modular monolith (Platform API) + 3 workers desacoplados** (Sync, Document, AI) comunicados por eventos (RabbitMQ + outbox pattern). Límites lógicos DDD estrictos validados por architecture tests; extracción a microservicios solo con criterios medidos vía strangler pattern (ADR-012). Distinción explícita logical boundary ≠ deployment boundary. Prohibida la sobreingeniería (nada de Kafka/Kubernetes hasta tener razón medida).

### 2.2 Bounded contexts (7)
Procurement (sync/consulta/matching, ACL frente a ChileCompra) · Document Intelligence (pipeline documental de 11 etapas) · Knowledge/RAG (embeddings, retrieval, análisis IA, requisitos) · Proposal Management (perfil de empresa, propuestas versionadas, outcomes) · Compliance (rule engine + LLM asistido) · Audit/Traceability (append-only) · Identity/Access (delegado en Keycloak).

### 2.3 Stack tecnológico
| Capa | Tecnología |
|---|---|
| Backend | .NET 10, C#, ASP.NET Core, Clean Architecture, DDD, FluentValidation, OpenTelemetry |
| Frontend | Angular 20, Angular Material, RxJS/Signals |
| Datos | MongoDB (operacional + raw), PostgreSQL (auditoría/reporting, diferido a FASE 15 — ADR-002), Qdrant (vectores — ADR-005), Redis (cache/locks/dedupe), MinIO (objetos — ADR-004) |
| Mensajería | RabbitMQ topic exchange `ppip.events`, DLQ + retry queues 30s/5m/1h, outbox (ADR-003) |
| Identidad | Keycloak OIDC (SPA: Code+PKCE), JWT, RBAC `viewer<analyst<editor<admin<superadmin` (ADR-010 + amendment) |
| IA | Puertos propios `ILlmProvider`/`IEmbeddingProvider` (ADR-007): Ollama default local, OpenAI/Gemini opcionales; `IOcrService` (ADR-006): Tesseract default, Mock para tests, OCI Document Understanding futuro |
| Edge | Traefik (ADR-009): TLS, rate limiting, labels de Compose |
| Observabilidad | OTel Collector → Prometheus + Loki + Tempo (ADR-011 + Amendment: Tempo sobre Jaeger) → Grafana, correlacionados (traceId↔logs↔métricas) |
| Infra | Docker Compose con perfiles `core/app/obs/demo`, redes segmentadas edge/app/data/obs, health checks encadenados |

### 2.4 Patrones de diseño obligatorios
Outbox en todo productor de eventos; consumidores idempotentes (dedupe por eventId + idempotency key de negocio — tabla completa en `docs/14-reliability/01`); anti-corruption layer frente a ChileCompra; raw payload inmutable como base de toda derivación; versionado append-only universal (documentos, análisis, prompts, modelos, propuestas — nunca sobrescribir); puertos/adaptadores para toda infraestructura (el dominio no conoce MongoDB/RabbitMQ/HTTP/LLM/Docker/OCI — NFR-013); structured output de LLM validado por JSON Schema antes de persistir; circuit breaker + backoff con jitter en clientes externos.

### 2.5 Simulación OCI
Cada componente local mapea a un servicio OCI con estrategia de migración que **nunca toca el dominio** (solo adapters + configuración). Tabla completa de 20 mapeos: `docs/04-architecture/11-oci-mapping.md`; orden de migración: `docs/17-oci-migration/01-migration-strategy.md`.

## 3. Reglas de Negocio y Convenciones

### 3.1 Reglas de IA (no negociables)
1. NO inventar: requisitos, precios, fechas, capacidades de la empresa, certificaciones, documentación.
2. Toda afirmación clasificada **FACT / INFERENCE / RECOMMENDATION / UNKNOWN**; dato ausente → literal «Información no encontrada en las fuentes analizadas».
3. Evidencia obligatoria para todo lo derivado de documentos: `{documentId, documentVersion, page, chunkId, sourceText, confidence}` — sin evidencia → UNKNOWN.
4. RAG **siempre filtrado por `compraAgilId` inyectado por el servidor** (jamás por el LLM ni el cliente); sin fallback al conocimiento paramétrico del modelo para hechos del proceso (ADR-008).
5. Contenido documental = datos no confiables, nunca instrucciones (anti prompt-injection, T1).
6. Prompts versionados en `/prompts` con frontmatter (promptId, version, changeReason…); **nunca modificar una versión usada históricamente** — siempre versión nueva + re-evaluación.
7. Cada ejecución registra AIExecution: modelo+versión, prompt+versión, tokens, costo, duración, hashes, correlationId.
8. Capacidades declarables en propuestas provienen **exclusivamente** del CompanyProfile; solo rubros `confirmed`/`manual` alimentan el matching.
9. Compliance: reglas determinísticas primero; el LLM nunca es autoridad única; override humano requiere justificación auditada.
10. Nada generado por IA llega a `approved` sin acción humana.

### 3.2 Reglas de datos
Raw payload inmutable e imborrable; todo derivado (normalizado, chunks, vectores, análisis) es regenerable desde su fuente; Qdrant es índice derivado, jamás fuente primaria; binarios solo en MinIO (`chilecompra/{codigo}/original|pages|images|ocr|extracted|generated/` con SHA-256), MongoDB solo metadata+referencias; invalidación en cascada marca `stale`, nunca borra; datos demo siempre con `isDemoData: true`.

### 3.3 Convenciones de API
REST/JSON, prefijo `/api`, recursos kebab-case; errores RFC 7807 con correlationId; paginación `page/pageSize` (default 20, máx 100); 202 + Location para operaciones asíncronas; `Idempotency-Key` en mutaciones no idempotentes; `If-Match` para concurrencia optimista (409 + diff en conflicto); `X-Correlation-Id` aceptado/generado/devuelto siempre. Catálogo completo: `docs/06-api/00-api-conventions.md`.

### 3.4 Convenciones de eventos
Envelope estándar (eventId uuid v7, eventType, version, timestamp, correlationId, causationId, producer, isDemoData, payload); routing key `contexto.nombre-evento.vN`; at-least-once + consumidores idempotentes; orden no garantizado → handlers verifican precondiciones y re-encolan con delay; cambios breaking → versión nueva con dual-publish. Catálogo de 19 eventos: `docs/07-events/00-event-conventions.md`.

### 3.5 Convenciones de identificadores y documentación
Documentación **en español** con términos técnicos en inglés. IDs: FR-xxx (funcionales, numeración con huecos intencionales por bloques), NFR-xxx, UC-xxx, ADR-xxx, RSK-xx, OQ-xx (preguntas abiertas), ASM-xx (supuestos), ACT-xx (actores), T-x (amenazas), F-x (escenarios de fallo). Ningún cambio de decisión sin ADR nuevo o amendment explícito (ejemplo: ADR-010 Amendment superadmin). Ninguna pregunta abierta se cierra silenciosamente.

### 3.6 Método de trabajo (MASTER PROMPT 2 — resumen operativo)
- **Regla principal:** nunca REQUISITO → CÓDIGO. Siempre: especificación → arquitectura → diseño → implementación → tests → validación → documentación.
- **Ciclo:** DISCOVER→SPECIFY→ARCHITECT→DESIGN→REVIEW→IMPLEMENT→TEST→OBSERVE→VALIDATE→DOCUMENT→NEXT.
- **Al recibir una nueva funcionalidad (§49):** primero indicar requisito relacionado, bounded context, impacto arquitectónico, datos, API/eventos, seguridad, observabilidad, testing y documentación afectada; después implementar; al cerrar, reportar archivos modificados, decisiones, tests, riesgos y siguiente paso.
- **Definition of Done (§34):** requisito+diseño+contratos documentados, código, tests, error handling, logging, metrics, tracing, security, docs, docker, health check, validación y fila actualizada en la matriz de trazabilidad.
- **Alcance MoSCoW estricto**; nada se implementa "porque parece interesante"; recortes siempre explícitos.
- Tests se crean junto con cada feature, nunca después. Medir antes de optimizar (sin objetivos de performance hasta baseline de FASE 18).

### 3.7 Preferencias del usuario observadas
Responde en español; prefiere decisiones recomendadas con justificación (eligió "Recommended" consistentemente); las mejoras nuevas entran como SHOULD HAVE salvo indicación contraria; aprueba avanzar rápido pero valorando el método (gates formales). Cambios integrados siempre con análisis de impacto §49 antes de tocar archivos.

## 4. Estructura Documental (índice de lo ya creado)

```
C:\ClaudeCowork\Agente_Compras_Agiles\
├── README.md                     ← mapa general y estado
├── ARCHITECTURE / DOMAIN / DATA / SECURITY / AI / RAG / OPERATIONS / OCI-MIGRATION .md   ← síntesis raíz
├── CONTEXT-HANDOVER.md           ← este documento
├── Master Prompt — OCI Local Simulator...md  ← master prompt principal (rector)
├── docs/
│   ├── ROADMAP.md                ← fases 0-19 con estados y asignación de SHOULD
│   ├── architecture-review-package.md  ← gate FASE 0→1: APROBADO 2026-08-16 + changelog de cambios
│   ├── 01-discovery/             ← 10 docs: objetivos, alcance MoSCoW, actores (ACT-01..13),
│   │                                FR-001..066 (54 activos), NFR-001..021, supuestos ASM-01..08,
│   │                                riesgos RSK-01..15, preguntas OQ-01..10, glosario
│   ├── 02-use-cases/             ← UC-001..014 (flujo principal, alternativos, eventos, APIs)
│   ├── 03-domain/                ← bounded contexts + modelo de dominio completo
│   ├── 04-architecture/          ← 00-overview, 01-04 C4 (contexto/contenedor/componente/deploy),
│   │                                05 data flow, 06 event flow, 07 seguridad, 08 observabilidad,
│   │                                09 IA, 10 RAG, 11 OCI mapping — diagramas Mermaid
│   ├── 05-architecture-decisions/ ← ADR-001..012 (todos Accepted; ADR-010 con Amendment superadmin)
│   ├── 06-api/                   ← convenciones + catálogo por contexto + ejemplo normativo RAG query
│   ├── 07-events/                ← envelope, reglas NFR-019, catálogo 19 eventos + ejemplo normativo
│   ├── 08-data/                  ← arquitectura de datos, source of truth, lineage con cascada stale
│   ├── 09-document-intelligence/ ← pipeline 11 etapas + clasificación PDF + chunking + OCR
│   ├── 10-rag/                   ← especificación completa del pipeline RAG + evaluación
│   ├── 11-ai/                    ← gobernanza, formato de prompts, output contracts (4 schemas)
│   ├── 12-security/              ← controles + RBAC (5 roles) + threat model T1..T12
│   ├── 13-observability/         ← logs/métricas ppip_* /traces/dashboards/alertas/health
│   ├── 14-reliability/           ← failure architecture F1..F16 + tabla de idempotencia
│   ├── 15-testing/               ← 12 tipos de test con herramientas y momentos
│   ├── 16-operations/            ← DX, Demo Mode, seeding, runbooks
│   ├── 17-oci-migration/         ← estrategia por componente en 10 pasos
│   └── 18-traceability/          ← matriz FR→UC→componente→API/evento→código→test→doc
├── prompts/  (system/analysis/requirements/rag/proposal/compliance — README, se pobla en F9-10)
├── evaluation/ scripts/                              ← stubs con README; se implementan por fase
├── infrastructure/docker/            ← FASE 1+2: compose (perfiles core/app/obs/demo) + config/
│   └── config/{otel-collector,prometheus,loki,tempo,grafana}/  ← FASE 2
├── src/
│   ├── building-blocks/Ppip.BuildingBlocks.Health/          ← FASE 1
│   ├── building-blocks/Ppip.BuildingBlocks.Observability/   ← FASE 2 (OTel + CorrelationId)
│   ├── services/Ppip.PlatformApi/, workers/Ppip.{Sync,Document,Ai}Worker/  ← FASE 1, instrumentados en FASE 2
│   └── apps/frontend/                ← FASE 1 (Angular 20.3)
├── tests/Ppip.BuildingBlocks.Observability.Tests/  ← FASE 2, xUnit (4 tests, CorrelationIdMiddleware)
└── _to_delete/                   ← tar.gz de entregas pasadas; el usuario puede borrarlo
```

## 5. Estado Actual Exacto

### 5.1 Hitos 100% terminados
1. **FASE 0 — Architecture & Specification Bootstrap: COMPLETA Y APROBADA** (gate 2026-08-16, registrado en `docs/architecture-review-package.md`). 0 enlaces rotos, IDs consistentes.
2. **Cambio 1 «Propuesta de Plataforma» integrado (SHOULD):** HITL en extracción documental (FR-053/054, UC-003 A6), rubros por LLM + auditoría (FR-055/056, UC-010), dashboard de matching (FR-057, UC-011), export .docx (FR-058).
3. **Cambio 2 «Mejoras Evolutivas» integrado (SHOULD):** outcomes + dashboard de efectividad (FR-059/060, UC-012), score de ganabilidad heurístico (FR-061; ML = FUTURE FR-062), monitoreo proactivo + notificaciones (FR-063/064, UC-013), throttling dinámico + rol superadmin (FR-065/066, UC-014, NFR-021).
4. **FASE 1 — Docker infrastructure: IMPLEMENTADA (2026-08-16).** `infrastructure/docker/docker-compose.yml` (perfiles core/app/obs/demo, redes edge/app/data/obs) + override de dev; esqueletos .NET 10 `Ppip.PlatformApi`/`Ppip.SyncWorker`/`Ppip.DocumentWorker`/`Ppip.AiWorker` con `/health`+`/ready` (dependencias reales); `Ppip.BuildingBlocks.Health` compartido; frontend Angular 20.3 (CLI oficial, build+3 tests en verde); `Makefile` + `scripts/smoke-test.sh`. Deliberadamente fuera de esta fase: seeding real (placeholder que falla explícito), usuarios Mongo de mínimo privilegio (FASE 4+).
5. **FASE 2 — Observability foundation: IMPLEMENTADA Y VALIDADA END-TO-END (2026-08-16).** Perfil `obs` (OTel Collector, Prometheus, Loki, Tempo, Grafana); `Ppip.BuildingBlocks.Observability` (OTel traces+métricas+logs vía OTLP, `CorrelationIdMiddleware`+`CorrelationIdDelegatingHandler`) referenciado desde los 4 servicios; endpoint temporal `GET /api/diagnostics/trace-check` en Platform API; dashboard `PPIP - Service Overview` (5 paneles) provisionado en Grafana; Tempo elegido sobre Jaeger (ADR-011 Amendment); 4 tests xUnit para `CorrelationIdMiddleware`. **Este entorno sí tenía Docker Desktop + .NET 10 SDK reales** (a diferencia del que generó FASE 1) y se usó para validar de punta a punta: `docker compose up -d` con los 3 perfiles, `/api/diagnostics/trace-check` devolvió 200, el mismo `traceId` apareció con 7 spans en Tempo y el mismo `traceId`+`correlationId` en los logs de Loki, Prometheus mostró métricas separadas por servicio, Grafana provisionó datasources+dashboard correctamente. Deliberadamente fuera de esta fase: los 4 dashboards de negocio y las alertas iniciales (dependen de métricas `ppip_*` inexistentes hasta sus fases), correlationId en eventos RabbitMQ (ningún worker publica eventos reales todavía).

**Bugs pre-existentes de FASE 1 encontrados y corregidos al validar con herramientas reales (ver `docs/ROADMAP.md` para el detalle completo de los 6):** `Ppip.BuildingBlocks.Health` sin `FrameworkReference` a ASP.NET Core; 3 registros `AddCheck` con una sobrecarga inexistente (→ `AddTypeActivatedCheck`); los 4 Dockerfiles no copiaban `Directory.Build.props`/`global.json` (`TargetFramework` vacío) y creaban un usuario con UID que colisiona con el `app`/`$APP_UID` que ya trae la imagen base; `Makefile`/`smoke-test.sh` nunca cargaban `docker-compose.override.yml` (puertos de dev nunca se publicaban); Prometheus sin `honor_labels: true` (colisión de la label `job`); provisioning de Grafana sin escapar `$` (`${__value.raw}` se guardaba vacío). Ninguno era parte del alcance nuevo de FASE 2, pero todos bloqueaban validar cualquier cosa — corregidos como prerrequisito.

Prompts y lógica de dominio real **todavía no existen** — eso es FASE 4+.

### 5.2 Decisiones del usuario ya tomadas (no volver a preguntar)
Idioma docs: español · repo en la raíz de la carpeta · cambios 1 y 2 como SHOULD · gate FASE 0→1 aprobado · recomendador heurístico ahora, ML después · outcomes manuales + API si el spike la confirma · notificaciones in-app + email digest (MailHog local).

### 5.3 La tarea inmediata
**FASE 3 — Identity & security:** realm Keycloak `ppip`, 5 roles, JWT en Platform API, matriz RBAC con tests de autorización, secretos vía Docker secrets/.env, rate limiting en Traefik.

Nota operativa para retomar el stack: `make up && make up-obs && make smoke && make smoke-obs`. Traefik puede fallar a bindear el puerto 80 si el host ya lo tiene ocupado (pasó en la sesión de FASE 2, por otro proceso ajeno al proyecto) — no es un bug del compose; en ese caso los servicios de app siguen accesibles vía `docker exec <container> curl http://localhost:8080/...` o publicando temporalmente otro puerto.

En paralelo (preparación de FASE 5, puede adelantarse): conseguir el ticket/API key de ChileCompra y ejecutar el **spike** que cierra OQ-01 (contrato real y paginación), OQ-02 (descarga de adjuntos), OQ-10 (¿expone adjudicaciones/OC?) y valida ASM-01/ASM-08 (términos de uso).

### 5.4 Riesgos y cuestiones abiertas a vigilar
RSK-02 (alucinaciones — mitigación en cada capa), RSK-05 (sobreingeniería — resistir), RSK-12 (alcance MUST grande — gates), RSK-14 (cold start del score), OQ-03 (modelo de embeddings, decide dimensión de Qdrant en F9), OQ-09 (taxonomía de rubros para matching, F12).

### 5.5 Notas operativas del entorno (Cowork)
- La carpeta del usuario está montada vía device bridge; **en la carpeta montada no se puede sobrescribir con tar ni borrar con rm** (unlink prohibido): extraer a `/tmp` y copiar con `cp -f`; para "borrar", mover a `_to_delete/`.
- Existe memoria persistente de proyecto en el escritorio del usuario (`fase0_bootstrap.md` vía project memory) con el mismo estado que este documento; este handover la supersede si divergen.
- Flujo de entrega de archivos: generar en workspace cloud → SendUserFile → device_commit_files → (si hay que sobrescribir) device_bash con cp -f.

---
### 5.6 Nota de versión — Angular (transparencia de decisión, no oculta el trade-off)
El registro npm ya publica Angular 22 estable (2026-08). Se implementó el frontend en **Angular 20.3.x** por ser la versión ya documentada en el stack (ARCHITECTURE.md) — no se saltó de versión sin ADR. Si el equipo quiere adoptar 22, requiere una decisión explícita, no un cambio silencioso de un scaffold.

---
*Fin del handover. El asistente entrante debe confirmar lectura de README + ROADMAP + `infrastructure/docker/README.md` y proceder con la tarea inmediata #1 (verificar FASE 1 con Docker/SDK reales) siguiendo el formato de respuesta §49.*
