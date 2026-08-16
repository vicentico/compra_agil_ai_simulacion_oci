# AI.md — Síntesis

Detalle en [docs/11-ai/](docs/11-ai/).

La IA es **decision support, no decisor autónomo**. Reglas: no inventar (requisitos, precios, fechas, capacidades, certificaciones); separar FACT / INFERENCE / RECOMMENDATION / UNKNOWN; citar evidencia siempre; declarar incertidumbre («Información no encontrada en las fuentes analizadas»); structured output validado por JSON Schema antes de persistir; prompts versionados en `/prompts` (nunca dispersos en código); tracking de costo por Compra Ágil/documento/usuario/operación/modelo; proveedor LLM abstraído (Ollama/OpenAI/Gemini) — ver [ADR-007](docs/05-architecture-decisions/ADR-007-llm-abstraction.md).
