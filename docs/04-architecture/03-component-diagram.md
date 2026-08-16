# 03 — Component Diagram (C4 nivel 3 — Platform API y Workers)

## Platform API (modular monolith)

```mermaid
flowchart TB
    subgraph API[Platform API .NET 10]
        subgraph ProcM[Modulo Procurement]
            PC[CompraAgil Endpoints] --> PA[Application: queries/commands] --> PD[Domain] 
            PA --> PRepo[IMongoCompraRepo]
        end
        subgraph DocM[Modulo Document]
            DC[Document Endpoints] --> DA[Application] --> DD[Domain]
            DA --> DRepo[IDocumentRepo] & DStore[IObjectStorage]
        end
        subgraph KnowM[Modulo Knowledge/RAG]
            KC2[RAG + Analysis Endpoints] --> KA[Application] --> KD[Domain]
            KA --> KVec[IVectorSearch] & KLlm[ILlmProvider] & KEmb[IEmbeddingProvider]
        end
        subgraph PropM[Modulo Proposal]
            PPC[Proposal Endpoints] --> PPA[Application] --> PPD[Domain]
            PPA --> PPRepo[IProposalRepo] & PPTmpl[ITemplateProvider]
        end
        subgraph CompM[Modulo Compliance]
            CCn[Compliance Endpoints] --> CAp[Application] --> CDo[Domain: RuleEngine]
            CAp --> CLlm[ILlmProvider]
        end
        subgraph AudM[Modulo Audit]
            AC[Audit/Trace Endpoints] --> AA[Application] --> ARepo[IAuditStore]
        end
        BB[Building Blocks: EventBus, Outbox, CorrelationContext, SchemaValidation, AuthZ]
    end
    ProcM & DocM & KnowM & PropM & CompM -.publican via Outbox.-> BB
```

Reglas: los módulos se comunican solo por eventos (outbox) o contratos públicos de aplicación; dominio sin referencias a infraestructura (validado por architecture tests, NFR-013).

## Sync Worker

Componentes: `Scheduler` → `SyncOrchestrator` → `ChileCompraClient` (resilience: retry, circuit breaker, rate limit, timeout) → `PayloadValidator` → `Normalizer` → `ChangeDetector` (hash) → `CheckpointStore` → `EventPublisher (outbox)`.

## Document Worker

Consumidores por etapa (uno por evento): `DownloadHandler` → `StoreHandler` → `ClassifyHandler` → `ParseHandler` → `OcrHandler (IOcrService)` → `ChunkHandler (ChunkingService)` → `EmbedHandler (IEmbeddingProvider)` → `IndexHandler (IVectorIndex)`. Cada handler: dedupe por idempotency key, precondición verificada, publicación del evento siguiente.

## AI Worker

`AnalysisHandler`, `RequirementsHandler`, `ProposalSectionHandler`, `ComplianceAssistHandler` — todos sobre `ILlmProvider` + `PromptRegistry` (versionado) + `SchemaValidator` + `CostTracker`.
