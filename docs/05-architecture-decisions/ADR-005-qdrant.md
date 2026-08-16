# ADR-005 — Qdrant como vector database

**Estado:** Accepted · **Fecha:** 2026-08-16

## Context
RAG por compra exige búsqueda vectorial con **filtrado por metadata obligatorio** (compraAgilId, documentId, versión, página, sección, chunkType, source) — MP §14.

## Options
1. **Qdrant**. 2. pgvector (PostgreSQL). 3. Atlas Vector Search / Mongo local. 4. Elasticsearch/OpenSearch kNN.

## Decision
Qdrant, tras puerto `IVectorIndex`, colección por entorno con payload de metadata completo; índice tratado como derivado reconstruible.

## Rationale
Filtrado por payload de primera clase (el requisito duro), rendimiento sólido en local con footprint bajo, API simple, snapshots. pgvector implicaría adoptar PostgreSQL antes de tiempo (contradice ADR-002) y su filtrado combinado es menos directo. Vector search de MongoDB local no está disponible como en Atlas. OpenSearch es una plataforma completa: sobreingeniería.

## Consequences
- (+) Filtros nativos; separación clara índice-derivado vs fuente.
- (−) Un servicio más; riesgo de desincronización (RSK-11 → job de reconciliación por hash).
- (−) Dimensión de colección fija por modelo de embedding: cambio de modelo = re-embedding (documentado en 17-oci-migration y docs/10-rag).

## Rejected Alternatives
pgvector, Mongo vector, OpenSearch (arriba).

## Future Reconsideration
En migración OCI no hay símil 1:1: evaluar Qdrant sobre OKE vs servicio gestionado disponible en su momento; IVectorIndex y reconstruibilidad hacen el cambio barato.
