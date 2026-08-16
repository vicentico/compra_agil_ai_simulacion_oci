# 03 — Data Lineage

```mermaid
flowchart TD
    A[ChileCompra API] -->|Ingestion: sync worker, correlationId| B[Raw payload inmutable]
    B -->|Normalization: normalizer vN| C[CompraAgil normalizada]
    C -->|Document detection| D[Document + sourceUrl]
    D -->|Download + SHA-256| E[Binario MinIO original/]
    E -->|Classification + Parsing| F[Texto por pagina + metodo]
    F -->|OCR condicional + confianza| G[Texto OCR]
    F & G -->|Unificacion| H[Representacion unificada]
    H -->|Chunking semantico vN| I[DocumentChunks + hash]
    I -->|Embedding modelo vN| J[Vectores Qdrant]
    I & C -->|Prompt vX + modelo vY| K[AIAnalysis / Requirements + evidencia]
    K & L[CompanyProfile] & M[Template vN] & J -->|Generation prompt vZ| N[ProposalSection]
    N & K -->|Rules + LLM| O[ComplianceResult]
    B & C & D & E & F & G & I & J & K & N & O -->|cada transformacion| P[(AuditEvent: input/output hash, actor, version, correlationId)]
```

## Registro por transformación

Cada flecha del grafo persiste en su AuditEvent: qué entró (inputHash), qué salió (outputHash), quién (actor/servicio/versión), con qué (promptVersion/modelVersion/algoritmo vN), cuándo, y bajo qué correlationId. Esto hace posible UC-009: dada una afirmación en una propuesta, llegar en N saltos hasta la línea del PDF y la llamada HTTP original.

## Invalidación en cascada

Documento nuevo/versión nueva → invalida (marca stale, no borra): chunks → vectores → análisis → secciones generadas basadas en esa evidencia → evaluaciones de compliance. La UI muestra qué está stale; el reproceso es explícito (humano o job), nunca silencioso.
