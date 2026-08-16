# UC-012 — Registrar resultado de propuesta y consultar efectividad

| Campo | Valor |
|---|---|
| Actor | Usuario (ACT-01) |
| Objetivo | Cerrar el ciclo comercial: registrar el resultado de cada propuesta y evidenciar la efectividad/ROI de la plataforma |
| Requisitos | FR-059, FR-060, NFR-016, NFR-012 |
| Precondiciones | Propuesta generada (UC-006); usuario autenticado (editor) |

## Flujo principal

1. Al conocer el desenlace de una postulación, el usuario registra el outcome (`POST /api/proposals/{id}/outcome`): presentada, adjudicada, no_adjudicada, desierta o descartada, con monto adjudicado, fecha y notas opcionales.
2. El sistema persiste el outcome versionado (corregible con historial), publica `ProposalOutcomeRecorded.v1` y audita con actor humano.
3. El dashboard de efectividad (`GET /api/effectiveness/metrics`) agrega: win-rate por período, montos adjudicados, propuestas generadas/presentadas, tiempo medio de generación, costo IA por propuesta (cruza NFR-016) — telemetría de negocio presentable a clientes.
4. Cuando existan outcomes suficientes, las señales históricas (organismo, categoría) alimentan el score de ganabilidad (FR-061) como factor adicional explicable.

## Flujos alternativos y errores

- **A1 — Corrección de un outcome:** nueva versión del outcome con motivo; el dashboard se recalcula; historial completo auditado.
- **A2 — Verificación automática (si OQ-10 se resuelve positivo):** el Monitor Worker detecta la adjudicación en la API y sugiere el outcome; el usuario confirma (human-in-the-loop, nunca escritura silenciosa).
- **A3 — Sin outcomes registrados:** el dashboard muestra métricas de actividad (propuestas generadas, tiempo, costo) y explicita que el win-rate requiere registrar resultados.

## Postcondiciones
Historial transaccional de resultados; métricas de efectividad reproducibles y trazables a sus outcomes.

## Eventos / Datos / APIs
`ProposalOutcomeRecorded.v1`, `AuditEventCreated.v1`. ProposalOutcome, Proposal, AIExecution (costos). `POST /api/proposals/{id}/outcome`, `GET /api/effectiveness/metrics`.
