# UC-009 — Consultar trazabilidad

| Campo | Valor |
|---|---|
| Actor | Usuario (ACT-01), Administrador (ACT-02) |
| Objetivo | Reconstruir y navegar la cadena completa de derivación de cualquier dato del sistema |
| Requisitos | FR-040..FR-042, NFR-003, NFR-012 |
| Precondiciones | AuditEvents registrados por los flujos anteriores |

## Flujo principal

1. Desde cualquier entidad (compra, documento, requisito, sección de propuesta, resultado de compliance) el usuario abre «Trazabilidad» (`GET /api/compra-agil/{id}/trace`, `GET /api/audit?entityId=...`).
2. El sistema reconstruye la cadena: CompraAgil → llamada API (raw payload, timestamp, hash) → Documento (versión, hash) → OCR (método, confianza) → Chunk → Embedding (modelo) → AIExecution (prompt/modelo/tokens) → Requirement → Proposal (versión/sección/autor) → Compliance (motor/resultado) → versión final.
3. Cada nodo muestra: actor, timestamp, correlationId/causationId, versiones previa/nueva, hashes de entrada/salida.
4. Para afirmaciones IA responde: ¿por qué?, ¿con qué documento?, ¿qué página?, ¿qué evidencia?, ¿qué modelo?, ¿qué prompt?, ¿qué versión del documento?, ¿qué confianza? (explicabilidad, MP2 §47).
5. Filtros por correlationId permiten reconstruir una ejecución distribuida completa, cruzable con traces en Grafana.

## Flujos alternativos

- **A1 — Cadena incompleta (etapa fallida):** el grafo muestra el punto de corte y el estado del fallo.
- **A2 — Entidad de demo:** claramente marcada `isDemoData`.

## Postcondiciones
Solo lectura.

## Datos / APIs
AuditEvent, todas las entidades núcleo (lectura). `GET /api/compra-agil/{id}/trace`, `GET /api/audit`.
