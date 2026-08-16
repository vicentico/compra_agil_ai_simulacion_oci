# 02 — Modelo de dominio

Notación: **Agregado** (raíz), Entity, VO = Value Object. Ids como VOs tipados (CompraAgilId, DocumentId...). Detalle de persistencia en [../08-data/](../08-data/) — este documento es agnóstico de infraestructura.

## Procurement

**CompraAgil** (Agregado)
- Identidad: `CompraAgilId` (código oficial ChileCompra)
- VOs: Money (monto+moneda), DateRange (publicación/cierre), EstadoCompra, InstitutionRef
- Entities internas: ProductRequirement (ítems solicitados: producto, cantidad, unidad)
- Referencias: Institution (por id), Documents (por id — otro contexto)
- Invariantes: código único; toda versión normalizada deriva de un RawPayload identificado; transiciones de estado válidas (publicada → cerrada → adjudicada/desierta)
- Versionamiento: nueva versión por cada cambio detectado; timestamps + source + audit metadata

**Institution** (Entity/Agregado pequeño): organismo comprador; identidad por código oficial.
**Supplier** (Entity, FUTURE salvo para CompanyProfile propio).
**SyncExecution** (Agregado): ejecución de sync con contadores, errores, duración, correlationId.
**SyncCheckpoint** (Entity singleton por source): lastSuccessfulSync, ventana, contadores.
**RawCompraAgilPayload** (VO inmutable persistido): payload, sourceUrl, retrievedAt, httpStatus, responseHash, apiVersion, correlationId.

## Document Intelligence

**Document** (Agregado)
- Identidad: `DocumentId`
- VOs: Sha256Hash, StorageRef (bucket/key MinIO), DocumentClass (textual|scanned|mixed|tables|images|complex), ProcessingState (máquina de estados por etapa)
- Entities: DocumentVersion (hash + storageRef + detectedAt), DocumentPage (número, texto, método de extracción, ocrConfidence, boundingBoxes?)
- Invariantes: binarios solo en object storage; hash obligatorio; una versión nunca se modifica; estado avanza solo por etapas válidas

**DocumentChunk** (Entity, hija lógica de Document): chunkId, compraAgilId, documentId, versión, página, section, subsection, chunkType (title|paragraph|table|requirement|list|annex), text, hash, tokenCount, embeddingId?

## Knowledge / RAG

**Embedding** (Entity): embeddingId, chunkId, modelVersion, dimension, vectorRef (Qdrant pointId). El vector vive en Qdrant; el dominio guarda la referencia.
**AIAnalysis** (Agregado): versión, resultado estructurado (con clasificación FACT/INFERENCE/RECOMMENDATION/UNKNOWN por afirmación), opportunityScore, complianceComplexity, refs a AIExecution.
**AIExecution** (Entity): modelo, modelVersion, promptId, promptVersion, tokensIn/Out, costo, duración, inputHash, outputHash, correlationId.
**Requirement** (Agregado): descripción, type, mandatory, category (technical|commercial|legal|administrative|delivery|warranty|documentation|financial|environmental|social|other), estado de revisión humana.
**RequirementEvidence** (VO): documentId, documentVersion, page, chunkId, sourceText, confidence.
**PromptVersion / ModelVersion** (Entities de registro, append-only).

## Proposal Management

**CompanyProfile** (Agregado): legalName, rut, description, products, services, certifications, experience, deliveryCapabilities, geographicCoverage, guarantees, contacts, legalDocuments, commercialPolicies. Invariante: única fuente válida de capacidades declarables.
**Proposal** (Agregado)
- Entities: ProposalVersion (append-only), ProposalSection (tipo de sección, contenido, autor humano|ia, fuentes/evidencia, estado draft|generated|edited|approved|requires_input)
- VOs: SectionType (portada, presentación, resumen, técnica, productos, cantidades, cumplimiento, plazos, entrega, garantía, económica, documentos, declaraciones, anexos), Authorship
- Invariantes: nunca sobrescribir versiones; toda sección generada por IA conserva evidencia y prompt/modelo; edición concurrente controlada por versión (optimistic concurrency)

## Compliance

**ComplianceEvaluation** (Agregado): ligada a (proposalVersionId, conjunto de requirementIds); estado global; entidad hija **ComplianceResult** (requirementId, status PASS|PARTIAL|FAIL|UNKNOWN, explanation, evidence, confidence, engine rule|llm|human_override).
- Invariante: resultado determinístico prevalece sobre LLM en conflicto; override humano requiere justificación auditada.

## Audit / Traceability

**AuditEvent** (Agregado append-only): eventId, timestamp, actor, actorType (human|worker|service|ai), service, operation, entityType, entityId, previousVersion, newVersion, correlationId, causationId, inputHash, outputHash, model?, promptVersion?. Inmutable; sin updates ni deletes.

## Domain events (resumen)

Ver catálogo completo en [../07-events/](../07-events/): CompraAgilDetected, CompraAgilUpdated, DocumentDetected, DocumentDownloaded, DocumentExtracted, OcrCompleted, DocumentChunked, EmbeddingCreated, AIAnalysisCompleted, RequirementsExtracted, ProposalGenerated, ProposalUpdated, ComplianceEvaluated, AuditEventCreated.

## Policies y servicios de dominio

- **SyncPolicy**: decide creación/actualización/no-op por comparación de hash.
- **OcrPolicy**: decide OCR por densidad de texto y umbral configurable.
- **ChunkingService**: segmentación semántica por estructura.
- **EvidencePolicy**: ninguna afirmación derivada de documentos sin evidencia → UNKNOWN.
- **CompliancePolicy**: orden determinístico → LLM → humano.
- **VersioningPolicy**: append-only universal para documentos, análisis, prompts, modelos y propuestas.

## Specifications

Ejemplos a implementar como specification objects reutilizables: `CompraAbiertaSpec`, `CompraPorCerrarSpec(days)`, `RequiereOcrSpec(minTextDensity)`, `PropuestaEvaluableSpec`, `EvaluacionStaleSpec`.
