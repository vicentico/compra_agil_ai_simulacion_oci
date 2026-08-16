# Matriz de trazabilidad de requisitos

Cadena: Requisito → Caso de uso → Componente arquitectónico → API/Evento → Código → Test → Documentación. En FASE 0 las columnas Código/Test referencian los nombres planificados; se actualizan al implementar cada fase (parte del Definition of Done).

| Req | UC | Componente | API / Evento | Código (plan) | Test (plan) | Doc |
|---|---|---|---|---|---|---|
| FR-001..005 | UC-001 | Sync Worker | POST /api/sync/compra-agil · CompraAgilDetected/Updated.v1 | ChileCompraSyncWorker, ChileCompraClient, SyncOrchestrator, CheckpointStore | SyncWorkerTests, ChileCompraClientContractTests, SyncIdempotencyTests | 02-use-cases/UC-001, 04-architecture/06, ADR-003 |
| FR-006..008 | UC-002 | Platform API módulo Procurement; Angular dashboard | GET /api/compra-agil*, /api/dashboard/metrics | CompraAgilEndpoints, CompraAgilQueries | CompraAgilApiTests | UC-002, 06-api/00 |
| FR-010..011 | UC-003 | Document Worker (Download/Store) | DocumentDetected/Downloaded.v1 | DownloadHandler, StoreHandler, MinioObjectStorage | DocumentDownloadTests, SsrfAllowlistTests | UC-003, 09-doc-intel/01, ADR-004 |
| FR-012..015 | UC-003 | Document Worker (Classify/Parse/OCR) | DocumentExtracted.v1, OcrCompleted.v1 | ClassifyHandler, ParseHandler, OcrHandler, LocalOcrService | PdfClassificationTests, OcrHandlerTests | 09-doc-intel/01, ADR-006 |
| FR-016..018 | UC-003 | Document Worker (Chunk/Embed/Index) | DocumentChunked.v1, EmbeddingCreated.v1 | ChunkingService, EmbedHandler, QdrantVectorIndex | ChunkingTests, IndexingIdempotencyTests | 09-doc-intel/01, ADR-005 |
| FR-020..023 | UC-005 | Knowledge module (RAG) | POST /api/rag/{id}/query | RagPipeline, ContextAssembler, EvidenceMapper | RagPipelineTests, RagEvaluation (dataset) | 10-rag/01, ADR-008, 06-api/01 |
| FR-024..025 | UC-004 | AI Worker (Analysis) | AIAnalysisCompleted.v1 · GET /api/compra-agil/{id}/analysis | AnalysisHandler, PromptRegistry, SchemaValidator | AnalysisSchemaTests, AiEvaluation | 11-ai/01, ADR-007 |
| FR-026..027 | UC-004 | AI Worker (Requirements) | RequirementsExtracted.v1 · GET .../requirements | RequirementsHandler | RequirementExtractionEvaluation | 11-ai/01, UC-004 |
| FR-030 | UC-006 | Proposal module | GET/PUT /api/company-profile | CompanyProfileEndpoints | CompanyProfileApiTests | UC-006 |
| FR-031 | UC-006 | AI Worker (ProposalSection) + Proposal module | POST /api/proposals(/{id}/generate) · ProposalGenerated.v1 | ProposalGenerator, TemplateProvider | ProposalGenerationTests, ProposalIdempotencyTests | UC-006, 11-ai/01 |
| FR-032..035 | UC-007 | Proposal module + Angular editor | PUT .../sections/{id}, POST .../regenerate · ProposalUpdated.v1 | ProposalVersioning, SectionEndpoints | ProposalVersioningTests, ConcurrencyConflictTests | UC-007, 14-reliability F16 |
| FR-036..038 | UC-008 | Compliance module | POST/GET /api/proposals/{id}/compliance · ComplianceEvaluated.v1 | ComplianceRuleEngine, ComplianceAssistHandler | RuleEngineTests, ComplianceEvaluation dataset | UC-008 |
| FR-040..042 | UC-009 | Audit module | GET /api/audit, GET .../trace · AuditEventCreated.v1 | AuditStore, TraceGraphBuilder | AuditCoverageTests | UC-009, 08-data/03 |
| FR-053..054 | UC-003 (A6) | Document Worker + Angular (tareas de revisión) | GET /api/documents/reviews, POST /api/documents/{id}/review · DocumentReviewRequested/Completed.v1 | ExtractionReviewPolicy, ReviewEndpoints, ManualUploadHandler | ExtractionReviewTests, ManualUploadPipelineTests | UC-003, 09-doc-intel/01 |
| FR-055..056 | UC-010 | AI Worker (infer-rubros) + Proposal module | POST /api/company-profile/infer-rubros, PUT .../rubros | RubroInferenceHandler, RubrosResult schema | RubroInferenceSchemaTests, RubroAuditTests | UC-010, 11-ai/01 |
| FR-057 | UC-011 | Procurement module (MatchingPolicy) + Angular opportunities | GET /api/opportunities | MatchingPolicy, OpportunityQueries | MatchingPolicyTests | UC-011, OQ-09 |
| FR-058 | UC-006/011 | Proposal module (DocxExporter) | GET /api/proposals/{id}/export?format=docx | DocxExporter (OpenXML) | DocxExportTests | UC-006, UC-011 |
| FR-059..060 | UC-012 | Proposal module (outcomes) + Angular efectividad | POST /api/proposals/{id}/outcome, GET /api/effectiveness/metrics · ProposalOutcomeRecorded.v1 | ProposalOutcomeEndpoints, EffectivenessQueries | OutcomeVersioningTests, EffectivenessMetricsTests | UC-012 |
| FR-061 | UC-011 | Procurement (ScoringPolicy) | GET /api/opportunities (score + descomposición) | ScoringPolicy, ScoreConfig | ScoringPolicyTests (determinismo + descomposición) | UC-011, RSK-14 |
| FR-063..064 | UC-013 | Monitor/Notification Worker + Angular notificaciones | GET /api/notifications, PUT .../preferences · HighPotentialCompraDetected.v1, NotificationDispatched.v1 | MonitorHandler, NotificationDispatcher, EmailSender(SMTP/MailHog) | MonitorIdempotencyTests, DigestCurationTests | UC-013, RSK-15 |
| FR-065..066 | UC-014 | Sync Worker (throttle) + Admin panel | GET/PUT /api/admin/rate-limits · RateLimitConfigChanged.v1 | DynamicRateLimiter, RateLimitConfigStore | HotReloadRateLimitTests, SuperAdminAuthZTests | UC-014, ADR-010 amendment |
| FR-050..052 | — | Infra + scripts/seed | — | `infrastructure/docker/docker-compose.yml`, `Ppip.PlatformApi`/`Ppip.SyncWorker`/`Ppip.DocumentWorker`/`Ppip.AiWorker`, `src/apps/frontend`, `scripts/seed/seed.py` (placeholder) | `scripts/smoke-test.sh` (ComposeHealthSmokeTest) ejecutado — pendiente SeedIdempotencyTest (FASE 4+) | 16-operations/01, ROADMAP.md nota FASE 1 |
| NFR-001/002 | transversal | Building blocks (outbox, dedupe) | — | OutboxDispatcher, IdempotencyStore | IdempotencyTests | 14-reliability/01 |
| NFR-003 | transversal | OTel building block | — | CorrelationContext, OtelSetup | TracePropagationTests | 13-observability/01, ADR-011 |
| NFR-007..009 | transversal | Keycloak + validaciones | — | AuthPolicies, FileValidator, UrlAllowlist | AuthZMatrixTests, SecurityTests | 12-security, ADR-010 |
| NFR-010/011 | transversal | SchemaValidator + EvidencePolicy | — | AiOutputValidator | AiContractTests | 11-ai/01 |
| NFR-013 | transversal | Architecture tests | — | ArchitectureRules | ArchitectureTests | ADR-001, 03-domain/01 |
| NFR-019 | transversal | Event contracts | schemas/ | EventEnvelope, SchemaRegistry | EventContractTests | 07-events/00 |

Regla de mantenimiento: ningún PR de feature se mergea sin actualizar su fila (Definition of Done, MP2 §34).
