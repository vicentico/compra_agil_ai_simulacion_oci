# UC-008 — Evaluar compliance

| Campo | Valor |
|---|---|
| Actor | Compliance Engine (ACT-10); Usuario revisa |
| Objetivo | Evaluar el cumplimiento de la propuesta contra los requisitos extraídos, de forma explicable y re-ejecutable |
| Requisitos | FR-036..FR-038, NFR-011 |
| Precondiciones | Requirements extraídos (UC-004) y Proposal existente (UC-006/007) |

## Flujo principal

1. Disparo automático tras generación (`ProposalGenerated`) o manual (`POST /api/proposals/{id}/compliance`).
2. **Fase determinística primero:** reglas verificables sin LLM (documento exigido presente/ausente, campo numérico dentro de rango, plazo declarado ≤ plazo exigido, certificación presente en CompanyProfile).
3. **Fase asistida:** requisitos no decidibles determinísticamente se evalúan con LLM comparando requisito + evidencia vs contenido de la propuesta; salida validada por schema.
4. Cada `ComplianceResult` registra: requirementId, status (PASS/PARTIAL/FAIL/UNKNOWN), explicación, evidencia, confianza y **qué motor lo decidió** (rule/llm).
5. Persiste `ComplianceEvaluation` versionada ligada a la versión exacta de propuesta y requisitos; publica `ComplianceEvaluated.v1`; audita.
6. La UI muestra la matriz de cumplimiento; el usuario puede corregir un resultado (override humano, auditado con justificación).

## Flujos alternativos y errores

- **A1 — LLM caído:** resultados determinísticos se entregan; los asistidos quedan UNKNOWN `pending_llm`, re-ejecutables.
- **A2 — Propuesta editada después de evaluar:** la evaluación queda marcada `stale`; se sugiere re-ejecución (FR-038).
- **A3 — Requisito sin correlato en la propuesta:** FAIL con explicación «sección no encontrada», no UNKNOWN.

## Postcondiciones
Matriz versionada, explicable y trazable; el LLM nunca fue autoridad única (NFR / MP §18).

## Eventos / Datos / APIs
`ComplianceEvaluated.v1`, `AuditEventCreated.v1`. ComplianceEvaluation, ComplianceResult. `POST /api/proposals/{id}/compliance`, `GET /api/proposals/{id}/compliance`.
