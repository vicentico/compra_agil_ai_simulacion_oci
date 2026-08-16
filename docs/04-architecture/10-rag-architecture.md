# 10 — RAG Architecture

Especificación completa en [../10-rag/](../10-rag/).

```mermaid
flowchart LR
    Q[Query usuario] --> QC[Query Classification]
    QC --> QE[Query Expansion]
    QE --> MF[Metadata Filtering compraAgilId=X obligatorio]
    MF --> VS[Vector Search Qdrant]
    MF --> KS[Keyword Search opcional]
    VS & KS --> RR[Reranking]
    RR --> CA[Context Assembly con citas]
    CA --> LLM[LLM con instruccion anti-injection]
    LLM --> ANS[Respuesta]
    ANS --> EV[Evidencia: documentId, page, chunkId, sourceText, confidence]
    EV -->|score < umbral| UNK[Informacion no encontrada]
```

Principios: RAG **por compra** (filtro obligatorio), evidencia siempre, sin fallback al conocimiento paramétrico del modelo para hechos del proceso, retrieval medible etapa por etapa (precision/recall en /evaluation).
