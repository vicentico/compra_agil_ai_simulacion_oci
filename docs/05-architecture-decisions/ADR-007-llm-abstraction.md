# ADR-007 — Abstracción de proveedor LLM

**Estado:** Accepted · **Fecha:** 2026-08-16

## Context
MP §3 exige permitir Ollama/OpenAI/Gemini/otros sin acoplar el dominio; gobernanza exige registrar modelo/versión/tokens/costo por ejecución.

## Options
1. **Puertos propios `ILlmProvider` / `IEmbeddingProvider` con adapters por proveedor.**
2. SDK unificado de terceros (LangChain/SemanticKernel) como capa central.
3. Acoplarse a un proveedor.

## Decision
Opción 1. Contrato mínimo propio: `CompleteStructuredAsync(promptRef, context, schema, options) → validated JSON + usage`; selección por configuración y por operación (modelo pequeño para clasificar, grande para generar); Semantic Kernel u otros pueden usarse *dentro* de un adapter, nunca como contrato del dominio.

## Rationale
El contrato propio mantiene el dominio limpio (NFR-013), hace triviales el mock y la evaluación A/B entre proveedores, y deja OCI Generative AI como un adapter más. Frameworks de orquestación cambian rápido; adoptarlos como frontera pública nos acoplaría a su churn.

## Consequences
- (+) Multi-proveedor real, testeable, cost tracking centralizado.
- (−) Mantener adapters propios (aceptado: superficie pequeña, structured output only).

## Rejected Alternatives
Framework como frontera; proveedor único (arriba).

## Future Reconsideration
Si el número de operaciones IA crece mucho, reevaluar un runtime de orquestación interno manteniendo el puerto.
