# 00 — Convenciones de eventos

## Envelope estándar

Todo evento se publica en el exchange topic `ppip.events` con routing key `contexto.nombre-evento.vN` y este envelope:

```json
{
  "eventId": "uuid v7",
  "eventType": "CompraAgilDetected",
  "version": 1,
  "timestamp": "2026-08-16T12:00:00Z",
  "correlationId": "flujo de negocio end-to-end",
  "causationId": "eventId del evento/comando que lo causó",
  "producer": "sync-worker@1.4.0",
  "isDemoData": false,
  "payload": { }
}
```

## Reglas (NFR-019)

1. **Versionado**: cambios aditivos mantienen `vN`; breaking → `vN+1`, publicación dual durante la transición, consumidores declaran versiones soportadas.
2. **Idempotencia**: consumidores dedupean por `eventId` (Redis SETNX + unique index); el efecto de negocio usa además idempotency key propia (ej. documentId+etapa+hash).
3. **At-least-once**: outbox en el productor; ack tras efecto persistido; DLQ tras reintentos escalonados (30s/5m/1h).
4. **Orden no garantizado**: cada handler verifica precondiciones; si el insumo no existe aún, re-encola con delay.
5. **Schema**: JSON Schema por evento en [`schemas/`](schemas/) (envelope compartido + un schema por evento, `$ref` al envelope); validado en contract tests productor/consumidor. Implementado desde FASE 4 para los 2 eventos con productor real (`CompraAgilDetected.v1`, `CompraAgilUpdated.v1`); el resto del catálogo gana su schema cuando su productor se implemente — no antes (evita contratos especulativos sin código real que los emita).
6. **Payload mínimo**: ids + hechos del cambio; los consumidores consultan el estado actual por id (evita payloads obesos y datos stale).

## Catálogo

| Evento | Productor | Consumidores | Payload (resumen) |
|---|---|---|---|
| CompraAgilDetected.v1 | Sync Worker | Document Worker, API (notif) | compraAgilId, codigo, rawPayloadId, documentRefs[] |
| CompraAgilUpdated.v1 | Sync Worker | Document Worker, AI Worker | compraAgilId, version, changedFields[], rawPayloadId |
| DocumentDetected.v1 | Sync/Document Worker | Document Worker | documentId, compraAgilId, sourceUrl, declaredType |
| DocumentDownloaded.v1 | Document Worker | Document Worker | documentId, versionId, sha256, storageRef, sizeBytes |
| DocumentExtracted.v1 | Document Worker | Document Worker | documentId, versionId, pages, classification, textDensity |
| OcrCompleted.v1 | Document Worker | Document Worker | documentId, versionId, pagesOcr[], avgConfidence |
| DocumentChunked.v1 | Document Worker | Document Worker | documentId, versionId, chunkCount, chunkIds[] |
| EmbeddingCreated.v1 | Document Worker | AI Worker (trigger análisis) | documentId, versionId, modelVersion, indexedCount, isLastOfCompra |
| DocumentReviewRequested.v1 | Document Worker | API (tarea UI) | documentId, versionId, reviewTaskId, motivo, confidence |
| DocumentReviewCompleted.v1 | API | Document Worker (reanuda chunking) | documentId, versionId, reviewTaskId, resolución, resolvedBy |
| AIAnalysisCompleted.v1 | AI Worker | API, Compliance | compraAgilId, analysisId, analysisVersion, opportunityScore |
| RequirementsExtracted.v1 | AI Worker | API, Compliance | compraAgilId, requirementIds[], counts por categoría |
| ProposalGenerated.v1 | AI Worker | API, Compliance (auto-eval) | proposalId, versionId, compraAgilId, sectionStates |
| ProposalUpdated.v1 | API | Compliance (marca stale) | proposalId, versionId, sectionId, author, authorType |
| ComplianceEvaluated.v1 | API/AI Worker | API (notif) | evaluationId, proposalVersionId, summary {pass,partial,fail,unknown} |
| ProposalOutcomeRecorded.v1 | API | Efectividad, Scoring (señal histórica) | proposalId, outcome, montoAdjudicado?, fecha, source |
| HighPotentialCompraDetected.v1 | Monitor Worker | Notification dispatcher | compraAgilId, version, score, descomposición, umbral |
| NotificationDispatched.v1 | Notification dispatcher | Audit | notificationId, userId, canal, tipo, compraAgilId?, digestPeriod? |
| RateLimitConfigChanged.v1 | API (admin) | Sync Worker (hot reload) | configVersion, previo, nuevo, actor, motivo |
| AuditEventCreated.v1 | Todos | Audit store | auditEvent completo |

Ejemplo normativo completo: [01-example-compra-agil-detected.md](01-example-compra-agil-detected.md).
