# UC-006 — Generar propuesta

| Campo | Valor |
|---|---|
| Actor | Usuario (ACT-01), Proposal Service (ACT-09) |
| Objetivo | Generar una propuesta comercial/técnica inicial completa a partir de plantilla, requisitos, perfil de empresa y RAG |
| Requisitos | FR-030..FR-031, NFR-010, NFR-011 |
| Precondiciones | Análisis y requisitos disponibles (UC-004); CompanyProfile completo; plantilla configurada |

## Flujo principal

1. El usuario pulsa «Generar propuesta» en una compra analizada (`POST /api/proposals` + `POST /api/proposals/{id}/generate`).
2. El Proposal Service arma el contexto: CompraAgil + Requirements + CompanyProfile + Template + RAGContext (chunks relevantes por sección).
3. Genera sección por sección (portada, presentación de empresa, resumen ejecutivo, propuesta técnica, productos, cantidades, cumplimiento de requisitos, plazos, entrega, garantía, propuesta económica, documentos requeridos, declaraciones, anexos) con prompts `proposal/*` versionados y structured output validado.
4. Regla dura: capacidades, certificaciones y experiencia provienen exclusivamente del CompanyProfile; datos del proceso provienen de requisitos/RAG con evidencia; precios quedan como campos a completar por el usuario salvo que existan en el perfil.
5. Persiste `Proposal` v1 con `ProposalSection`s, cada una con sus fuentes; publica `ProposalGenerated.v1`; audita con prompt/modelo/tokens.
6. El usuario recibe la propuesta editable (continúa en UC-007).
7. En cualquier momento puede **exportar la versión vigente a .docx editable** (FR-058, `GET /api/proposals/{id}/export?format=docx`); el archivo se almacena además en MinIO `generated/` con hash, trazable a la versión exacta de propuesta.

## Flujos alternativos y errores

- **A1 — CompanyProfile incompleto:** generación procede marcando secciones afectadas como `requires_input`; nunca inventa capacidades.
- **A2 — Fallo LLM en una sección:** las demás secciones se generan; la fallida queda `generation_failed`, regenerable individualmente.
- **A3 — Requisito sin evidencia clara:** la sección de cumplimiento lo marca UNKNOWN y lo destaca para revisión humana.
- **A4 — Generación duplicada (doble click / retry):** idempotency key por (compraId, templateVersion, trigger) evita propuestas duplicadas.

## Postcondiciones
Proposal v1 completa y auditada; ninguna afirmación sin fuente (perfil o evidencia documental).

## Eventos / Datos / APIs
`ProposalGenerated.v1`, `AuditEventCreated.v1`. Proposal, ProposalVersion, ProposalSection, CompanyProfile. `POST /api/proposals`, `POST /api/proposals/{id}/generate`, `GET /api/proposals/{id}`.
