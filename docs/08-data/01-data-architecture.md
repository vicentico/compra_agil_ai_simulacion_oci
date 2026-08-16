# 01 — Data Architecture

## Almacenes y ownership

| Almacén | Contexto owner | Contenido | Retención |
|---|---|---|---|
| MongoDB `procurement` | Procurement | compras_agiles, raw_payloads, instituciones, sync_executions, sync_checkpoints | raw: indefinida (inmutable); operacional: indefinida en POC |
| MongoDB `documents` | Document Intelligence | documents, document_versions, document_pages, document_chunks | Ligada al documento; versiones nunca se borran |
| MongoDB `knowledge` | Knowledge/RAG | ai_analyses, ai_executions, requirements, prompt_versions, model_versions, embeddings(refs) | Versionada, append-only |
| MongoDB `proposals` | Proposal Mgmt | company_profile, templates, proposals, proposal_versions | Append-only |
| MongoDB `compliance` | Compliance | compliance_evaluations | Versionada |
| MongoDB `audit` | Audit | audit_events (append-only) | Indefinida; ETL a PostgreSQL en FASE 15 |
| PostgreSQL | Audit/Reporting | Réplica consultable de auditoría + vistas de reporting | Desde FASE 15 (ADR-002) |
| MinIO `chilecompra` | Document Intelligence | `{codigo}/original|pages|images|ocr|extracted|generated/` | Original: indefinida; derivados: regenerables |
| Qdrant `chunks_v1` | Knowledge | vector + payload {compraAgilId, documentId, documentVersion, page, section, chunkType, source, hash, isDemoData} | Derivado, reconstruible |
| Redis | Building blocks | cache (TTL por tipo), locks (`lock:sync:{source}` TTL 10m), dedupe eventId (TTL 7d), rate limit counters | Efímero por TTL |

Nota: "MongoDB `x`" = bases lógicas separadas en la misma instancia local; se convierten en instancias/servicios separados solo si un contexto se extrae (ADR-012).

## Índices mínimos (se afinan por fase)

- compras_agiles: `{codigo:1}` unique; `{estado:1, fechaCierre:1}`; `{organismoCodigo:1}`; text index en nombre/descripción.
- raw_payloads: `{responseHash:1}`; `{compraAgilId:1, retrievedAt:-1}`.
- document_versions: `{documentId:1, sha256:1}` unique (idempotencia de descarga).
- document_chunks: `{compraAgilId:1, documentId:1, page:1}`; `{hash:1}`.
- ai_executions: `{correlationId:1}`; `{promptVersion:1, modelVersion:1}`.
- proposals/proposal_versions: `{proposalId:1, versionNumber:-1}`; unique `{proposalId:1, versionNumber:1}`.
- audit_events: `{entityType:1, entityId:1, timestamp:-1}`; `{correlationId:1}`.
- Colección Qdrant: dimensión fijada por modelo de embedding elegido (OQ-03); payload indexado por compraAgilId (keyword) para filtrado eficiente.

## Relaciones (referencias por id, no embedding entre agregados)

CompraAgil 1→N Document 1→N DocumentVersion 1→N DocumentPage/DocumentChunk 1→1 Embedding(ref) · CompraAgil 1→N AIAnalysis 1→N Requirement 1→N RequirementEvidence · CompraAgil 1→N Proposal 1→N ProposalVersion 1→N ProposalSection · ProposalVersion 1→N ComplianceEvaluation 1→N ComplianceResult → Requirement.
