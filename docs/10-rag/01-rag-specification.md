# 01 — Especificación RAG

Arquitectura visual: [../04-architecture/10-rag-architecture.md](../04-architecture/10-rag-architecture.md). Decisión: [ADR-008](../05-architecture-decisions/ADR-008-rag-strategy.md).

## Qué se recupera, por qué, cómo se puntúa, cómo se presenta

- **Qué**: DocumentChunks de la compra activa (filtro `compraAgilId` server-side obligatorio), opcionalmente acotados por documento/sección.
- **Por qué**: responder exclusivamente con evidencia del proceso; el conocimiento paramétrico del LLM no es fuente válida para hechos del proceso.
- **Cómo se puntúa**: score vectorial (cosine) → fusión opcional con keyword score (RRF) → reranking (SHOULD) → umbral mínimo configurable; bajo umbral → UNKNOWN.
- **Cómo se presenta**: respuesta + evidence[] (documentId, versión, página, chunkId, sourceText, score, confidence) con navegación al visor.

## Componentes

| Etapa | Especificación | Configurable |
|---|---|---|
| Embedding model | A decidir en OQ-03 (candidatos: nomic-embed-text local; text-embedding-3-small API). La dimensión fija la colección Qdrant; cambio de modelo = colección nueva + re-embedding (migración documentada) | modelo, dimensión |
| Chunk strategy | Semántica (docs/09); chunks tipo requirement priorizados en queries de requisitos | tamaño objetivo, overlap |
| Metadata | compraAgilId, documentId, documentVersion, page, section, chunkType, source, hash, isDemoData | — |
| Retrieval | top-k vectorial filtrado; k default 8 (1..20) | k, umbral score |
| Keyword search | Text index MongoDB sobre chunks; fusión RRF (SHOULD) | on/off |
| Reranking | Cross-encoder local o LLM-as-reranker pequeño (SHOULD, decidir con datos F9) | on/off, modelo |
| Context assembly | Presupuesto de tokens por modelo; chunks ordenados por score con encabezado de cita [D:doc p:X]; truncado nunca a mitad de chunk | presupuesto |
| Citation | El prompt exige citar [n] por afirmación; respuesta se post-procesa para mapear citas → evidence[]; afirmación sin cita → degradada a INFERENCE o eliminada | — |
| Hallucination control | Instrucción anti-injection + "solo contexto provisto"; validación de que sourceText citado existe en el chunk; umbral de score; UNKNOWN explícito | umbrales |

## Evaluación (con /evaluation, FASE 9-10)

Dataset: preguntas doradas por compra seed con respuestas y chunks esperados. Métricas: retrieval precision@k / recall@k, citation accuracy (¿la evidencia citada sustenta la afirmación?), factuality, hallucination rate, latencia por etapa. Regla: ninguna mejora de prompt/retrieval se adopta sin correr el dataset.

## Estado de implementación (FASE 9, 2026-08-19)

**Implementado end-to-end** (`src/modules/knowledge/Ppip.Knowledge.*`, `src/services/Ppip.PlatformApi` — primer endpoint de negocio real de PlatformApi). Diferencias reales vs. lo especificado aquí:

- **Embedding model (OQ-03, cerrada):** nomic-embed-text, 768 dimensiones, colección Qdrant `chunks_v1` (distancia coseno, índice de payload por `compraAgilId`). `OllamaEmbeddingProvider` implementado pero **no ejecutado contra un modelo Ollama real descargado en esta sesión** — mismo criterio que `TesseractOcrService` (FASE 8). El proveedor por defecto en producción es `MockEmbeddingProvider` (determinístico, SHA-256 del texto expandido y normalizado).
- **Keyword search / fusión RRF:** no implementado — solo búsqueda vectorial. Sigue siendo SHOULD, decidido con datos en FASE 9-10 según lo previsto.
- **Reranking:** no implementado, mismo criterio (SHOULD).
- **Citation:** implementado con una simplificación real respecto al ADR-007 literal — `ILlmProvider` devuelve texto crudo (no JSON estructurado validado por schema); `RagQueryOrchestrator` post-procesa citas `[n]` con una expresión regular que mapea directo al chunk numerado `n` del contexto que el propio orquestador construyó (no hay riesgo de que el LLM invente un chunk, porque `n` siempre referencia una posición real del contexto armado). Una afirmación sin ninguna cita `[n]` degrada a `AnswerType.Inference` con `evidence: []` (no se intenta segmentar la respuesta en afirmaciones individuales — granularidad a nivel de respuesta completa, no por oración).
- **Hallucination control — validación de que el `sourceText` citado existe en el chunk:** trivialmente cierta por construcción (el `sourceText` de cada `EvidenceItem` es el texto real del `DocumentChunk`, nunca algo que el LLM generó), así que no hay un paso de validación posterior separado — es una garantía estructural, no una verificación en runtime.
- **Confidence por evidencia:** reutiliza el score vectorial (coseno) — no hay un modelo de confianza por afirmación independiente del retrieval.
- **UC-005 A2 (Qdrant caído) vs. A3 (LLM caído):** implementados con comportamientos DELIBERADAMENTE distintos — A2 (`RetrievalUnavailableException`, cubre tanto el fallo de `IEmbeddingProvider` al vectorizar la pregunta como el de `IVectorIndex` en la búsqueda) se propaga como error explícito 503 (`detail` menciona "qdrant"); A3 (`LlmUnavailableException`) se captura dentro de `RagQueryOrchestrator` y degrada a una respuesta 200 con los chunks recuperados como resultados de búsqueda, sin síntesis — nunca un 503. El contrato genérico de errores (`docs/06-api/01-example-rag-query.md`) documenta "503 llm" como caso reservado para un modo estricto futuro, no el comportamiento por defecto de este POC.
- **404 vs. 409 (compra sin índice construido):** solo se implementó el 404 (compra inexistente). El 409 documentado ("compra sin índice construido") se simplificó deliberadamente: una compra que existe pero no tiene chunks indexados cae en el mismo camino que UC-005 A1 (sin evidencia relevante → 200 con `unanswered: true`), en vez de una señal 409 distinta con estado del pipeline — requeriría trackear el estado de la etapa 11 a nivel de compra completa, que no existe todavía.
- **Filtros opcionales por documento/sección** (`filters.documentId`/`filters.section` del contrato de ejemplo): no implementados en FASE 9 — el request solo acepta `question`/`topK`. Deferred, no hay campo silenciosamente ignorado en el contrato real expuesto.
- **`AIExecution`** se registra en TODA consulta, incluida UC-005 A1 (sin evidencia, `model: "n/a"`, 0 tokens) — cumple UC-005 paso 8 literalmente ("exista o no evidencia").
- **Sin dataset `/evaluation`** — FASE 9 no incluyó compras seed con preguntas doradas; queda para cuando exista contenido real indexado (bloqueado transitivamente por OQ-02, igual que FASE 7-8).
- **Sin test HTTP end-to-end** del endpoint contra Mongo+Qdrant+Ollama reales combinados en un solo proceso — se validó por partes: `RagQueryOrchestrator`/`EmbeddingIndexer` con fakes (17 tests), adaptadores de Infrastructure contra Mongo/Qdrant reales vía Testcontainers (14 tests), y que el contenedor DI de `Ppip.PlatformApi` con los 3 módulos combinados arranca correctamente contra un Keycloak real (`Ppip.PlatformApi.Tests`, 12 tests, ya existentes de FASE 3).
- **Bug real encontrado construyendo esto:** `Ppip.Procurement.Infrastructure.AddProcurementPersistence()` y `Ppip.DocumentIntelligence.Infrastructure.AddDocumentPersistence()` registraban `IMongoDatabase` directamente en el contenedor DI — inofensivo mientras cada módulo vivía en su propio worker (`SyncWorker`/`DocumentWorker`), pero `Ppip.PlatformApi` es el primer proceso que combina Procurement+DocumentIntelligence+Knowledge, y esa combinación habría hecho que los tres módulos resolvieran silenciosamente la MISMA base de datos (la última registrada). Corregido con un wrapper dedicado por módulo (`ProcurementMongoDatabaseProvider`/`DocumentMongoDatabaseProvider`/`KnowledgeMongoDatabaseProvider`) antes de que este endpoint llegara a ejecutarse — ver `Ppip.PlatformApi.Tests` (smoke del contenedor DI) como regresión.
