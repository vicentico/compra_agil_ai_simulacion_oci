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
