# 01 — Gobernanza de IA

## Principios (MP §36, MP2 §46-47)

Decision support, no decisor autónomo. No inventar requisitos, precios, fechas, capacidades, certificaciones ni documentación. Separación obligatoria FACT / INFERENCE / RECOMMENDATION / UNKNOWN. Evidencia citada para todo lo derivado de documentos; incertidumbre declarada («Información no encontrada en las fuentes analizadas»). Structured output validado por JSON Schema **antes** de persistir (NFR-010). Human-in-the-loop: requisitos, análisis, propuestas y compliance son revisables/corregibles y la corrección queda auditada.

## Registro por ejecución (AIExecution)

modelo + modelVersion, promptId + promptVersion, temperatura, tokens in/out, costo estimado, duración, inputHash, outputHash, correlationId, resultado de validación de schema. Nada de esto es opcional.

## Prompts (/prompts, MP2 §22-23)

Estructura: `system/`, `analysis/`, `requirements/`, `rag/`, `proposal/`, `compliance/`. Cada prompt es un archivo con frontmatter obligatorio:

```yaml
---
promptId: proposal-generator
version: 1.0
purpose: Generar seccion de propuesta desde plantilla + perfil + requisitos + contexto RAG
inputSchema: refs a variables esperadas
outputSchema: schemas/ProposalSectionResult.v1.json
model: configurable (default ollama/llama3.1:8b)
temperature: 0.2
createdDate: 2026-08-16
author: <autor>
changeReason: version inicial
evaluationResult: pendiente (dataset /evaluation)
---
```

Reglas: nunca modificar silenciosamente un prompt usado históricamente — todo cambio = nueva versión + changeReason + re-evaluación; el PromptRegistry carga por (promptId, version) y las ejecuciones antiguas siguen trazando a su versión exacta.

## Output contracts (JSON Schemas, se materializan en `schemas/` en FASE 10)

- **AnalysisResult.v1**: executiveSummary, purchaseObjective, products[], quantities[], technicalRequirements[], mandatoryRequirements[], optionalRequirements[], commercialConditions, deliveryConditions, warranty, requiredDocuments[], evaluationCriteria[], budget, deadlines[], risks[], questions[], opportunityScore (0-100 + rationale), complianceComplexity, recommendation — cada ítem con `assertionType: FACT|INFERENCE|RECOMMENDATION|UNKNOWN` y `evidence[]` cuando FACT.
- **RequirementsResult.v1**: requirements[] {description, type, mandatory, category, sourceDocument, page, evidence, confidence}.
- **ProposalSectionResult.v1**: sectionType, content, sources[] (perfil|evidencia documental), requiresInput[] (campos que el humano debe completar), assertions[] tipadas.
- **ComplianceResult.v1**: requirementId, status PASS|PARTIAL|FAIL|UNKNOWN, explanation, evidence, confidence.

Pipeline de validación: LLM output → schema validation (reintento acotado con feedback del error) → business validation (ej: evidencia citada existe; capability citada existe en CompanyProfile) → persistencia. Fallo definitivo = estado failed auditado, nunca persistencia parcial.

## Costos (NFR-016)

Cache por inputHash (mismo input + prompt + modelo = resultado cacheado); clasificación con modelos pequeños; generación con grandes solo donde aporta; presupuestos configurables por operación; tracking agregable por compra/documento/usuario/operación/modelo con dashboard en Grafana.
