# ADR-008 — Estrategia RAG: por compra, con evidencia obligatoria

**Estado:** Accepted · **Fecha:** 2026-08-16

## Context
MP §14-15: nunca RAG global en contexto de una compra; toda respuesta con evidencia; control de alucinaciones.

## Options
1. **RAG filtrado por compraAgilId con pipeline por etapas (clasificación → expansión → filtro → vector → keyword opcional → rerank → assembly → LLM → evidencia).**
2. RAG simple (vector top-k → LLM).
3. Fine-tuning / long-context sin retrieval.

## Decision
Opción 1, con dos reglas duras: (a) filtro `compraAgilId` obligatorio e inyectado por el servidor (jamás decidido por el LLM ni por el cliente), (b) afirmaciones sin chunk de respaldo → «Información no encontrada en las fuentes analizadas».

## Rationale
El filtro server-side elimina fuga entre compras y reduce el espacio de recuperación (precisión). Pipeline por etapas hace cada paso medible (/evaluation) y desactivable (reranking es SHOULD). Fine-tuning no aplica: los datos cambian por compra y la trazabilidad exige citar fuentes, no memorizarlas.

## Consequences
- (+) Evidencia navegable, evaluación por etapa, aislamiento entre compras.
- (−) Latencia mayor que RAG simple (aceptado; medible antes de optimizar).

## Rejected Alternatives
RAG simple (sin control de calidad ni evidencia estructurada); fine-tuning (arriba).

## Future Reconsideration
Umbrales de score, k, reranker concreto y búsqueda híbrida se fijan con datos en FASE 9-10.
