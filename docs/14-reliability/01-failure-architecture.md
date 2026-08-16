# 01 — Failure Architecture

Formato por escenario: **Detección / Retry / Fallback / Recuperación / UX / Auditoría**.

| # | Escenario | Detección | Retry | Fallback | Recuperación | UX | Auditoría |
|---|---|---|---|---|---|---|---|
| F1 | ChileCompra caído | Timeouts/5xx; health degraded | Backoff exp. + circuit breaker | Operar con datos locales | Ciclo siguiente retoma desde checkpoint | Banner "última sincronización: hh:mm" | SyncExecution failed |
| F2 | ChileCompra 429 | Status + Retry-After | Espera Retry-After; reduce tasa | Pausa del ciclo | Checkpoint intacto; reanuda | Transparente | Quota error métrica+evento |
| F3 | ChileCompra datos incompletos/malformados | Schema validation | No (dato inválido) | Registro quarantined; resto continúa | Corrección upstream → re-sync lo toma | Compra marcada "datos parciales" | Raw guardado + quarantine event |
| F4 | PDF no descargable (404/timeout) | HTTP error | 3 intentos backoff | — | Botón reprocess manual; re-check en próximo sync | Documento "descarga fallida" | Etapa failed |
| F5 | PDF corrupto | Parser exception | 1 reintento | Clasificar unprocessable | Versión nueva del doc lo reemplaza | "Documento no procesable" | parse_failed |
| F6 | OCR falla / baja confianza | Excepción / confidence < umbral | 2 reintentos | Continuar con flag low_confidence | Cambio de proveedor OCR reprocesa | Advertencia de calidad en visor y evidencia | ocr_failed / low_confidence |
| F7 | LLM caído/timeout | Timeout/5xx del provider | Backoff acotado | Provider alternativo si configurado; si no, cola pending | Reintento automático al volver health | "Análisis pendiente" con reintento visible | AIExecution failed |
| F8 | Qdrant caído | Health/errores upsert-search | Retry etapa Indexed | RAG devuelve error explícito (sin fallback paramétrico); resto del sistema opera | Reconciliación re-indexa pendientes por hash | "Búsqueda no disponible" | index_failed |
| F9 | MongoDB caído | Health/timeouts | Reintentos driver | **No hay**: dependencia dura | Restart/restore; outbox garantiza no perder eventos confirmados | Error 503 general | Post-mortem |
| F10 | RabbitMQ caído | Health/conexión | Reconexión automática | Outbox acumula sin publicar (no se pierde nada) | Dispatcher drena outbox al volver | Procesamiento "en cola" | Gap visible en métricas |
| F11 | Worker muere a mitad de etapa | Mensaje sin ack → requeue | Redelivery automático | — | Idempotencia absorbe re-proceso parcial | Transparente | Redelivery contado |
| F12 | Evento duplicado | Dedupe por eventId + idempotency key | — | — | Efecto único garantizado | Transparente | Duplicado métrica |
| F13 | Evento fuera de orden | Precondición del handler falla | Re-encolado con delay (máx N) | Tras N, DLQ | Operador re-inyecta desde DLQ | Transparente | DLQ alertada |
| F14 | Generación de propuesta falla (sección) | Schema/LLM error | Reintento acotado | Demás secciones completan; fallida = generation_failed | Regeneración individual | Sección marcada, regenerable | AIExecution failed |
| F15 | Compliance falla | Rule engine exception / LLM error | Retry | Resultados determinísticos se entregan; asistidos → UNKNOWN pending_llm | Re-ejecución | Matriz parcial con estado claro | Evaluation partial |
| F16 | Usuario edita mientras IA genera la misma sección | Version check (optimistic concurrency) | — | El que llega segundo recibe 409 + diff | Usuario fusiona; nada se pierde (append-only) | Diálogo de conflicto con comparación | Ambas versiones auditadas |

## Estrategia de idempotencia (NFR-001/002, MP2 §29)

| Operación | Idempotency key | Dedupe |
|---|---|---|
| Sync ciclo | lock Redis `lock:sync:chilecompra` | Ejecución concurrente → skipped |
| Alta/actualización compra | codigo + responseHash | Unique index; mismo hash = no-op |
| Descarga documento | documentId + sha256 | Unique en document_versions |
| Etapa pipeline | documentVersionId + etapa + inputHash | Estado de etapa + Redis |
| Embedding/Index | chunkHash + modelVersion | Upsert determinístico por pointId |
| Análisis IA | compraAgilId + inputHash + promptVersion + modelVersion | Cache: mismo input = resultado existente |
| Generación propuesta | Idempotency-Key del cliente (compraId+template+trigger) | Devuelve propuesta original |
| Consumo de evento | eventId | Redis SETNX TTL 7d + unique índice |

Regla de reintentos por defecto: 3 intentos, backoff exponencial con jitter (1s/5s/25s) en llamadas; colas retry 30s/5m/1h antes de DLQ; circuit breaker en clientes externos (5 fallos/30s → open 60s).
