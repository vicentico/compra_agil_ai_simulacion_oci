# 06 — Event Flow

## Cadena de eventos del pipeline completo

```mermaid
sequenceDiagram
    participant SW as Sync Worker
    participant MQ as RabbitMQ
    participant DW as Document Worker
    participant AIW as AI Worker
    participant API as Platform API
    SW->>MQ: CompraAgilDetected.v1 (correlationId C1)
    MQ->>DW: DocumentDetected.v1 (causation: CompraAgilDetected)
    DW->>MQ: DocumentDownloaded.v1
    DW->>MQ: DocumentExtracted.v1
    DW->>MQ: OcrCompleted.v1 (si aplica)
    DW->>MQ: DocumentChunked.v1
    DW->>MQ: EmbeddingCreated.v1
    MQ->>AIW: EmbeddingCreated.v1 (ultimo del documento)
    AIW->>MQ: AIAnalysisCompleted.v1
    AIW->>MQ: RequirementsExtracted.v1
    Note over API: Usuario genera propuesta (comando)
    API->>MQ: ProposalGenerateRequested.v1
    MQ->>AIW: ProposalGenerateRequested.v1
    AIW->>MQ: ProposalGenerated.v1
    MQ->>API: ComplianceRequested (auto)
    API->>MQ: ComplianceEvaluated.v1
    Note over MQ: Todos los eventos generan AuditEventCreated.v1
```

## Topología RabbitMQ

- Exchange topic `ppip.events` — routing key = `contexto.evento.version` (ej: `procurement.compra-agil-detected.v1`).
- Una cola por consumidor lógico (`document-worker.download`, `ai-worker.analysis`...), competing consumers para escalar.
- **DLQ por cola** (`*.dlq`) con TTL + reintentos escalonados (retry queues 30s/5m/1h) antes de dead-letter definitivo.
- **Outbox pattern** en cada productor: evento persistido transaccionalmente con el estado, publicado por dispatcher (at-least-once). Consumidores idempotentes por `eventId` + idempotency key de negocio.

## Garantías

At-least-once delivery; orden no garantizado entre colas → cada handler verifica precondiciones y re-encola si su insumo aún no existe (delay). Duplicados absorbidos por dedupe (Redis + unique keys en MongoDB). Compatibilidad: solo cambios aditivos en `.v1`; cambio breaking → `.v2` + consumidor dual durante transición (NFR-019).
