# RAG.md — Síntesis

Detalle en [docs/10-rag/](docs/10-rag/).

RAG **por Compra Ágil** (filtrado obligatorio por `compraAgilId`, nunca RAG global en contexto de un proceso específico). Pipeline: query → clasificación → expansión → filtrado por metadata → búsqueda vectorial (Qdrant) → keyword search opcional → reranking → ensamblado de contexto → LLM → respuesta **con evidencia obligatoria** (documentId, página, chunkId, texto fuente, confianza). Chunking semántico (títulos, secciones, tablas, requisitos), no división arbitraria.
