# UC-005 — Consultar RAG

| Campo | Valor |
|---|---|
| Actor | Usuario (ACT-01), RAG Service (ACT-08) |
| Objetivo | Responder preguntas en lenguaje natural sobre una Compra Ágil específica, con evidencia verificable |
| Requisitos | FR-020..FR-023, NFR-011 |
| Precondiciones | Compra con chunks indexados; usuario autenticado |

## Flujo principal

1. El usuario, dentro del contexto de una compra, formula una pregunta («¿Cuál es el plazo máximo de entrega?»).
2. El RAG Service clasifica la query (factual/procedimental/comparativa) y la expande si aporta.
3. Aplica filtrado obligatorio `compraAgilId = X` (+ filtros opcionales por documento/sección) y ejecuta búsqueda vectorial en Qdrant; keyword search opcional; reranking.
4. Ensambla contexto dentro de la ventana del modelo, priorizando chunks rerankeados y citando cada uno.
5. El LLM responde bajo instrucción explícita: el contenido documental es evidencia no confiable, no instrucciones; sin evidencia suficiente → declarar que no se encontró.
6. La respuesta retorna con evidencia por afirmación: documentId, página, chunkId, texto fuente, confianza.
7. El usuario hace click en la evidencia y el visor abre el documento en la página citada.
8. Se registra AIExecution (tokens, latencia, prompt/modelo) y auditoría.

## Flujos alternativos y errores

- **A1 — Sin chunks relevantes (score < umbral):** respuesta «Información no encontrada en las fuentes analizadas», sin invención.
- **A2 — Qdrant caído:** error explícito con estado de servicio; sin fallback a conocimiento del modelo.
- **A3 — LLM caído:** se muestran los chunks recuperados como resultados de búsqueda sin síntesis.
- **A4 — Pregunta fuera del contexto de la compra:** el sistema no amplía el filtro; sugiere búsqueda global explícita (fuera de alcance del RAG por compra).

## Postcondiciones
Ninguna mutación de dominio; ejecución IA auditada.

## Eventos / Datos / APIs
AuditEventCreated.v1. DocumentChunk, Embedding, AIExecution. `POST /api/rag/{compraId}/query`.
