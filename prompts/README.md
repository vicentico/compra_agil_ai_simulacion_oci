# /prompts

Prompts versionados del sistema (MP2 §22-23). Estructura: `system/`, `analysis/`, `requirements/`, `rag/`, `proposal/`, `compliance/`.

Reglas: cada prompt lleva frontmatter (promptId, version, purpose, inputSchema, outputSchema, model, temperature, createdDate, author, changeReason, evaluationResult — formato en [docs/11-ai/01-ai-governance.md](../docs/11-ai/01-ai-governance.md)); nunca se modifica una versión usada históricamente — se crea una nueva; todo cambio se re-evalúa con /evaluation antes de adoptarse. Se poblarán desde FASE 9-10.
