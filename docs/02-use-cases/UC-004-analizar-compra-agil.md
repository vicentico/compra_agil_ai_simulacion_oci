# UC-004 — Analizar Compra Ágil

| Campo | Valor |
|---|---|
| Actor | AI Service (ACT-07); Usuario revisa |
| Objetivo | Producir análisis estructurado y requisitos extraídos, con evidencia, para apoyar la decisión de participar |
| Requisitos | FR-024..FR-027, NFR-010, NFR-011, NFR-016, NFR-018 |
| Precondiciones | Documentos de la compra indexados (UC-003); proveedor LLM disponible; prompts versionados |

## Flujo principal

1. Evento `EmbeddingCreated`/final de indexación (o solicitud manual) dispara el análisis con correlationId heredado.
2. El AI Service recupera contexto: datos normalizados + chunks relevantes (retrieval interno filtrado por compraAgilId).
3. Ejecuta prompt `analysis/analyze-compra-vX.Y` con structured output.
4. Valida la respuesta contra JSON Schema `AnalysisResult`; inválida → reintento acotado; agotado → `analysis_failed` auditado.
5. Cada afirmación queda clasificada FACT / INFERENCE / RECOMMENDATION / UNKNOWN; datos ausentes → «Información no encontrada en las fuentes analizadas».
6. Ejecuta prompt `requirements/extract-requirements-vX.Y`; valida `RequirementsResult`; persiste cada `Requirement` con evidencia (documento, página, chunk, texto, confianza).
7. Persiste `AIAnalysis` + `AIExecution` (modelo, versión, prompt, tokens, costo, duración) + AuditEvent; publica `AIAnalysisCompleted.v1` y `RequirementsExtracted.v1`.
8. El usuario revisa el análisis, la matriz de requisitos y la evidencia; puede corregir/descartar requisitos (human-in-the-loop, queda auditado).

## Flujos alternativos y errores

- **A1 — LLM caído/timeout:** retry con backoff; fallback a proveedor alternativo si está configurado; si no, estado `pending_analysis` reintentable.
- **A2 — Documentos con OCR de baja confianza:** análisis procede con flag de calidad; opportunityScore refleja incertidumbre.
- **A3 — Re-análisis por documento nuevo/actualizado:** genera nueva versión de AIAnalysis; nunca sobrescribe la anterior.
- **A4 — Compra sin documentos:** análisis solo sobre datos estructurados de la API; todo lo no disponible → UNKNOWN.

## Postcondiciones
Análisis versionado, requisitos con evidencia, costo registrado, trazabilidad completa a prompt/modelo exactos.

## Eventos producidos
`AIAnalysisCompleted.v1`, `RequirementsExtracted.v1`, `AuditEventCreated.v1`

## Datos / APIs
AIAnalysis, AIExecution, Requirement, RequirementEvidence, PromptVersion, ModelVersion. `GET /api/compra-agil/{id}/analysis`, `GET /api/compra-agil/{id}/requirements`, `POST /api/analysis/{compraId}/run`.
