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
