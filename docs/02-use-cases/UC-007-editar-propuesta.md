# UC-007 — Editar propuesta

| Campo | Valor |
|---|---|
| Actor | Usuario (ACT-01) |
| Objetivo | Refinar la propuesta manteniendo versionado completo, con asistencia IA opcional por sección |
| Requisitos | FR-032..FR-035, NFR-012 |
| Precondiciones | Propuesta generada (UC-006) |

## Flujo principal

1. El usuario abre el editor: secciones con contenido, fuentes y estado.
2. Edita una sección manualmente → nueva versión de sección (append-only) con autor humano.
3. O solicita regeneración IA de una sección → sugerencia con evidencia; el usuario **acepta** (nueva versión, autor IA + aprobador humano) o **rechaza** (queda registrado, versión vigente intacta).
4. Puede visualizar la fuente de cualquier afirmación (evidencia → visor de documento).
5. Puede comparar dos versiones de una sección o de la propuesta completa (diff).
6. Puede restaurar una versión anterior → se crea una versión nueva equivalente (nunca se borra historia).
7. Puede re-ejecutar compliance (UC-008) tras cambios relevantes.

## Flujos alternativos y errores

- **A1 — Edición concurrente humano vs IA en la misma sección:** bloqueo optimista por versión; el segundo escritor recibe conflicto y ve el diff para fusionar (RSK-09, escenario de reliability).
- **A2 — Dos usuarios editan la misma sección:** mismo mecanismo de conflicto por versión.
- **A3 — Regeneración con LLM caído:** la edición manual sigue disponible; sugerencia queda en cola reintentable.

## Postcondiciones
Historial completo de versiones con autoría (humano/IA), sin sobrescrituras; auditoría por cada cambio.

## Eventos / Datos / APIs
`ProposalUpdated.v1`, `AuditEventCreated.v1`. Proposal, ProposalVersion, ProposalSection. `PUT /api/proposals/{id}/sections/{sectionId}`, `POST /api/proposals/{id}/sections/{sectionId}/regenerate`, `GET /api/proposals/{id}/versions`.
