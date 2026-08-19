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
**RateLimitConfig** (Agregado de configuración, Procurement): requestsPorMinuto, requestsPorHora, concurrenciaMax, ventanasDePausa, tamañoPagina; versionado con actor y motivo; hot-reload vía Redis (FR-065/066).
**WinnabilityScore** (VO, calculado): valor 0-100 + descomposición por factor {factor, peso, aporte, explicación}; determinístico dado el mismo input y la misma configuración de pesos (FR-061).
**Notification / NotificationPreference** (Entities, Procurement-monitoreo): notificación curada (tipo, compraAgilId, score, resumen, canales, estado de entrega) y preferencias por usuario (umbral, frecuencia digest, canales, silenciados) — FR-063/064.
**SyncCheckpoint** (Entity singleton por source): lastSuccessfulSync, ventana, contadores.
**RawCompraAgilPayload** (VO inmutable persistido): payload, sourceUrl, retrievedAt, httpStatus, responseHash, apiVersion, correlationId.

## Document Intelligence

**Document** (Agregado)
- Identidad: `DocumentId`
- VOs: Sha256Hash, StorageRef (bucket/key MinIO), DocumentClass (textual|scanned|mixed|tables|images|complex), ProcessingState (máquina de estados por etapa)
- Entities: DocumentVersion (hash + storageRef + detectedAt), DocumentPage (número, texto, método de extracción, ocrConfidence, boundingBoxes?)
- Invariantes: binarios solo en object storage; hash obligatorio; una versión nunca se modifica; estado avanza solo por etapas válidas

**ExtractionReviewTask** (Entity, hija de Document): reviewTaskId, documentVersionId, motivo (low_confidence | partial_parse | manual_request), estado (pending | resolved_validated | resolved_manual_upload | dismissed), resolvedBy, resolvedAt. Soporta el flujo HITL FR-053/054.
**DocumentChunk** (Entity, hija lógica de Document): chunkId, compraAgilId, documentId, versión, página, section, subsection, chunkType (title|paragraph|table|requirement|list|annex), text, hash, tokenCount, embeddingId?

## Knowledge / RAG

**Embedding** (Entity): embeddingId, chunkId, modelVersion, dimension, vectorRef (Qdrant pointId). El vector vive en Qdrant; el dominio guarda la referencia.
**AIAnalysis** (Agregado): versión, resultado estructurado (con clasificación FACT/INFERENCE/RECOMMENDATION/UNKNOWN por afirmación), opportunityScore, complianceComplexity, refs a AIExecution.
**AIExecution** (Entity): modelo, modelVersion, promptId, promptVersion, tokensIn/Out, costo, duración, inputHash, outputHash, correlationId.
**Requirement** (Agregado): descripción, type, mandatory, category (technical|commercial|legal|administrative|delivery|warranty|documentation|financial|environmental|social|other), estado de revisión humana.
**RequirementEvidence** (VO): documentId, documentVersion, page, chunkId, sourceText, confidence.
**PromptVersion / ModelVersion** (Entities de registro, append-only).

## Proposal Management

**CompanyProfile** (Agregado): legalName, rut, description, businessDescription (texto libre del onboarding), rubros[], products, services, certifications, experience, deliveryCapabilities, geographicCoverage, guarantees, contacts, legalDocuments, commercialPolicies. Invariante: única fuente válida de capacidades declarables.
**Rubro** (VO dentro de CompanyProfile): code, name, confidence, source (`inferred` | `confirmed` | `manual`), promptVersion?, modelVersion?. Invariante: solo rubros con source `confirmed`/`manual` alimentan el matching (FR-056).
**Proposal** (Agregado)
- Entities: ProposalVersion (append-only), ProposalSection (tipo de sección, contenido, autor humano|ia, fuentes/evidencia, estado draft|generated|edited|approved|requires_input)
- VOs: SectionType (portada, presentación, resumen, técnica, productos, cantidades, cumplimiento, plazos, entrega, garantía, económica, documentos, declaraciones, anexos), Authorship
- Invariantes: nunca sobrescribir versiones; toda sección generada por IA conserva evidencia y prompt/modelo; edición concurrente controlada por versión (optimistic concurrency)

**ProposalOutcome** (Entity, hija de Proposal): estado (presentada | adjudicada | no_adjudicada | desierta | descartada), montoAdjudicado?, fecha, notas, source (manual | api_suggested_confirmed), versionado con historial (FR-059).

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
- **ExtractionReviewPolicy**: decide cuándo una extracción es deficiente (umbral de confianza/densidad configurable) y crea la tarea de revisión (FR-053).
- **MatchingPolicy**: cruza categorías/rubros de la Compra Ágil con los rubros confirmados del perfil y produce score + explicación (FR-057; taxonomía por resolver en OQ-09).
- **ScoringPolicy**: calcula el WinnabilityScore con reglas ponderadas configurables; heurístico y explicable, sin ML en el POC (FR-061; recalibración automática = FUTURE FR-062).
- **NotificationCurationPolicy**: decide qué notificar (umbral, dedupe por compra/versión) y cómo agrupar el digest (FR-064; anti-fatiga RSK-15).
- **VersioningPolicy**: append-only universal para documentos, análisis, prompts, modelos y propuestas.

## Specifications

Ejemplos a implementar como specification objects reutilizables: `CompraAbiertaSpec`, `CompraPorCerrarSpec(days)`, `RequiereOcrSpec(minTextDensity)`, `PropuestaEvaluableSpec`, `EvaluacionStaleSpec`.

## Estado de implementación (actualizado FASE 8, 2026-08-19)

**Procurement** (FASE 4-6): dominio completo — `src/modules/procurement/Ppip.Procurement.Domain` (`CompraAgil`, `Institution`, `SyncExecution`, `SyncCheckpoint`, `RawCompraAgilPayload`, VOs `CompraAgilId`/`Money`/`DateRange`/`EstadoCompra`/`InstitutionRef`, entidad `ProductRequirement`, `SyncPolicy`, puertos en `Domain/Ports`) — 49 tests xUnit, sin infraestructura (NFR-013). Capa de aplicación (`SyncOrchestrator`) e infraestructura (Mongo/Redis/RabbitMQ) reales desde FASE 6.

**Document Intelligence** (FASE 7-8: UC-003 pasos 1-9, descarga+storage+clasificación+extracción+OCR+chunking): `src/modules/documents/Ppip.DocumentIntelligence.Domain` — `Document` (Identidad `DocumentId`, `DocumentStage`: descarga/storage, FASE 7), `DocumentVersion` (VOs `Sha256Hash`/`StorageRef`; gana en FASE 8 `DocumentProcessingStage`, `DocumentClass`, lista de `DocumentPage` — máquina de estados independiente de `DocumentStage` porque versiona "qué se hizo con el binario", no "cómo se obtuvo el binario"), `DocumentPage` (texto, método de extracción, densidad, confianza OCR), `DocumentChunk` (entidad propia, no anidada — colección `document_chunks`), `Policies/UrlAllowlistPolicy`, `Policies/PdfMagicBytes`, `Policies/ClassificationPolicy` — 52 tests xUnit, sin infraestructura (NFR-013, puertos en `Domain/Ports`). **Todavía no implementado:** `ExtractionReviewTask` (HITL, FR-053/054, FASE 8 propuesta de plataforma — llega cuando se implemente ese flujo, no antes).

Los demás contextos (Knowledge/RAG, Proposal Management, Compliance, Audit) todavía no tienen dominio implementado — se construyen en sus fases (9+, 12+, 14, 15).
