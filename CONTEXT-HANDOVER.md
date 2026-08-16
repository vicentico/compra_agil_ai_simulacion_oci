# Documento Maestro de Traspaso de Contexto (Context Handover)
## Public Procurement Intelligence Platform — OCI Local Simulator + ChileCompra Compra Ágil

| Metadato | Valor |
|---|---|
| Fecha de traspaso | 2026-08-16 |
| Rol que se traspasa | Tech Lead / Arquitecto del proyecto (agente IA con supervisión del usuario) |
| Ubicación del repositorio | `C:\ClaudeCowork\Agente_Compras_Agiles` (raíz del repo = carpeta del proyecto) |
| Sesión de origen | Claude Cowork, modelo configurado `claude-fable-5` |
| Estado global | **FASE 0 completada y APROBADA** (gate FASE 0→1 aprobado el 2026-08-16). Cero código de producción, por diseño. |
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
| Observabilidad | OTel Collector → Prometheus + Loki + traces (Tempo/Jaeger a decidir en FASE 2) → Grafana (ADR-011) |
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
├── evaluation/ infrastructure/ scripts/ src/ tests/   ← stubs con README; se implementan por fase
└── _to_delete/                   ← tar.gz de entregas pasadas; el usuario puede borrarlo
```

## 5. Estado Actual Exacto

### 5.1 Hitos 100% terminados
1. **FASE 0 — Architecture & Specification Bootstrap: COMPLETA Y APROBADA** (gate 2026-08-16, registrado en `docs/architecture-review-package.md`). Incluye toda la estructura documental anterior con verificación de coherencia (0 enlaces rotos, IDs consistentes).
2. **Cambio 1 «Propuesta de Plataforma» integrado (SHOULD):** HITL en extracción documental (FR-053/054, UC-003 A6, ExtractionReviewTask), rubros inferidos por LLM + auditoría del usuario (FR-055/056, UC-010), dashboard de matching (FR-057, UC-011), export .docx (FR-058).
3. **Cambio 2 «Mejoras Evolutivas» integrado (SHOULD):** outcomes de propuestas + dashboard de efectividad (FR-059/060, UC-012), score de ganabilidad heurístico explicable (FR-061; ML = FUTURE FR-062 por cold start), monitoreo proactivo + notificaciones in-app/email digest (FR-063/064, UC-013), throttling dinámico hot-reload + rol superadmin con panel de cuotas (FR-065/066, UC-014, NFR-021).

**NO existe código de producción, docker-compose, ni prompts materializados.** Es intencional: la regla principal del método prohíbe codificar antes del gate — que ya está aprobado, por lo que la implementación puede comenzar.

### 5.2 Decisiones del usuario ya tomadas (no volver a preguntar)
Idioma docs: español · repo en la raíz de la carpeta · cambios 1 y 2 como SHOULD · gate FASE 0→1 aprobado · recomendador heurístico ahora, ML después · outcomes manuales + API si el spike la confirma · notificaciones in-app + email digest (MailHog local).

### 5.3 Las 3 tareas inmediatas
1. **FASE 1 — Docker infrastructure** (siguiente acción concreta): `infrastructure/docker/` con docker-compose (perfiles core/app/obs/demo), 13 servicios (MongoDB, RabbitMQ, MinIO, Redis, Qdrant, Keycloak, Ollama, Traefik, Prometheus, Grafana, Loki, OTel Collector + esqueletos), redes segmentadas, volúmenes, `.env.example`, health checks encadenados, esqueletos .NET 10 (Platform API + 3 workers con `/health` `/ready`) y Angular 20. Criterio de éxito: `docker compose up -d` → todo healthy. Guía: `docs/04-architecture/04-deployment-diagram.md`.
2. **FASE 2 — Observability foundation:** OTel en los 4 servicios esqueleto, Collector → Prometheus/Loki/traces (decidir Tempo vs Jaeger — pendiente de ADR menor), Grafana con dashboards base, propagación traceId+correlationId demostrada end-to-end.
3. **FASE 3 — Identity & security:** realm Keycloak `ppip`, 5 roles, JWT en Platform API, matriz RBAC con tests de autorización, secretos vía Docker secrets/.env, rate limiting en Traefik.

En paralelo (preparación de FASE 5, puede adelantarse): conseguir el ticket/API key de ChileCompra y ejecutar el **spike** que cierra OQ-01 (contrato real y paginación), OQ-02 (descarga de adjuntos), OQ-10 (¿expone adjudicaciones/OC?) y valida ASM-01/ASM-08 (términos de uso).

### 5.4 Riesgos y cuestiones abiertas a vigilar
RSK-02 (alucinaciones — mitigación en cada capa), RSK-05 (sobreingeniería — resistir), RSK-12 (alcance MUST grande — gates), RSK-14 (cold start del score), OQ-03 (modelo de embeddings, decide dimensión de Qdrant en F9), OQ-09 (taxonomía de rubros para matching, F12).

### 5.5 Notas operativas del entorno (Cowork)
- La carpeta del usuario está montada vía device bridge; **en la carpeta montada no se puede sobrescribir con tar ni borrar con rm** (unlink prohibido): extraer a `/tmp` y copiar con `cp -f`; para "borrar", mover a `_to_delete/`.
- Existe memoria persistente de proyecto en el escritorio del usuario (`fase0_bootstrap.md` vía project memory) con el mismo estado que este documento; este handover la supersede si divergen.
- Flujo de entrega de archivos: generar en workspace cloud → SendUserFile → device_commit_files → (si hay que sobrescribir) device_bash con cp -f.

---
*Fin del handover. El asistente entrante debe confirmar lectura de README + ROADMAP + review package y proceder con la tarea inmediata #1 (FASE 1) siguiendo el formato de respuesta §49.*
