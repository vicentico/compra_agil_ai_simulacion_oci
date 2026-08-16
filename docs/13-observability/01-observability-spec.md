# 01 — Especificación de observabilidad

Arquitectura: [../04-architecture/08-observability-architecture.md](../04-architecture/08-observability-architecture.md). Decisión: [ADR-011](../05-architecture-decisions/ADR-011-observability.md).

## Logs
JSON estructurado (nivel, timestamp, servicio, versión, traceId, spanId, correlationId, mensaje, propiedades tipadas) → stdout → OTel Collector → Loki. Prohibido: secretos, tokens, contenido documental completo (solo ids/hashes). Niveles: Debug solo en desarrollo.

## Métricas (nombres estables, prefijo ppip_)
`ppip_chilecompra_api_latency_seconds` (histogram, por endpoint/status) · `ppip_chilecompra_api_errors_total` (counter, por tipo) · `ppip_chilecompra_quota_errors_total` (429) · `ppip_queue_depth` (gauge, por cola) · `ppip_worker_stage_duration_seconds` (por etapa del pipeline) · `ppip_ocr_duration_seconds` + `ppip_ocr_confidence` · `ppip_llm_duration_seconds` + `ppip_llm_tokens_total{direction,model,operation}` + `ppip_llm_cost_estimate` · `ppip_embedding_duration_seconds` · `ppip_rag_latency_seconds{stage}` · `ppip_proposal_generation_latency_seconds` · `ppip_compliance_duration_seconds` · ASP.NET/runtime metrics estándar.

## Traces
Spans por: request HTTP, publicación/consumo de evento (link entre productor y consumidor), llamada externa (ChileCompra, LLM, OCR), operación de datos relevante. Propagación W3C + correlationId como atributo y header de mensaje (sobrevive reintentos/DLQ).

## Dashboards Grafana (mínimos)
1. **Sync health**: ciclos, duración, creados/actualizados/sin cambio, errores por tipo, estado de checkpoint, 429s.
2. **Document pipeline**: profundidad de colas, duración por etapa, tasa de fallo por etapa, documentos por estado, confianza OCR.
3. **AI cost & latency**: tokens/costo por modelo/operación/compra, latencias, cache hit rate, validation failures.
4. **RAG quality**: latencia por etapa, scores de retrieval, tasa de UNKNOWN.
5. **API overview**: RPS, latencias p50/p95/p99, errores, rate limits.

## Alertas iniciales
Checkpoint sin avanzar > X horas · DLQ no vacía · tasa de fallo de etapa > umbral · credencial ChileCompra inválida (401) · presupuesto IA diario excedido · servicio unhealthy > 5 min.

## Health checks
`/health` (liveness), `/ready` (Mongo, RabbitMQ, Redis; MinIO/Qdrant donde aplique), `/live`. Dependencias externas (ChileCompra, LLM, OCR) se reportan como `degraded`, nunca tumban readiness (NFR-006). SLIs/SLOs: post-baseline FASE 18.

## Estado de implementación (FASE 2, 2026-08-16)

**Implementado:** OTel (traces+métricas+logs) en los 4 servicios .NET vía `Ppip.BuildingBlocks.Observability`, exportado OTLP al Collector (`infrastructure/docker/config/otel-collector/`) → Prometheus/Loki/Tempo (ADR-011 Amendment: Tempo). Middleware de `correlationId` (`X-Correlation-Id`) por servicio: acepta/genera/devuelve, tag de span, scope de logging. Dashboard base `PPIP - Service Overview` (request rate, p95, error rate, volumen de logs, logs recientes) provisionado en Grafana. Endpoint de diagnóstico `GET /api/diagnostics/trace-check` en Platform API que fuerza una llamada real a los 3 workers para demostrar el trace end-to-end + correlationId propagado (criterio de éxito de FASE 2, `docs/ROADMAP.md`).

**Deliberadamente diferido (recorte explícito, no omisión oculta):**
- Los 4 dashboards de negocio (Sync health, Document pipeline, AI cost & latency, RAG quality) se crean en las fases que producen las métricas `ppip_*` que visualizan (FASE 6, 8, 9-10) — construirlos ahora requeriría datos ficticios, prohibido por las reglas de IA/datos de este proyecto.
- Las alertas iniciales listadas arriba dependen en su mayoría de esas mismas métricas de negocio inexistentes todavía (checkpoint, DLQ, presupuesto IA); se definen junto a sus fases. Única excepción evaluada — "servicio unhealthy > 5 min" — también se difiere: requeriría scrapear `/health` de cada servicio directamente con Prometheus (duplicando la vía OTLP ya establecida) solo para esa alerta; se revisita si en una fase posterior ya existe esa señal por otro camino.
- Propagación de `correlationId` en headers de mensajes RabbitMQ: no aplica todavía porque ningún worker publica/consume eventos reales (son heartbeats placeholder de FASE 1); se implementa junto al primer productor/consumidor real (FASE 6, `docs/07-events/`).
