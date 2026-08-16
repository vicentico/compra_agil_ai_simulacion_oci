# UC-011 — Explorar oportunidades y generar propuesta on-demand

| Campo | Valor |
|---|---|
| Actor | Usuario (ACT-01) |
| Objetivo | Acelerar la venta: ver de un vistazo las Compras Ágiles que calzan con el perfil y, bajo demanda, obtener una propuesta editable lista para refinar |
| Requisitos | FR-057, FR-031, FR-058, FR-006 |
| Precondiciones | Rubros confirmados (UC-010); compras sincronizadas (UC-001) |

## Flujo principal

1. El usuario abre el **panel de oportunidades** (`GET /api/opportunities`): Compras Ágiles cuyo rubro/categoría coincide con los rubros confirmados del perfil.
2. Cada oportunidad expone los datos críticos para decidir: nombre, organismo, monto disponible, fecha de cierre, estado, score de coincidencia y **explicación del match** (qué rubro coincidió y por qué).
3. El panel ordena por defecto según el **score de ganabilidad** (FR-061), con su descomposición por factor visible (calce de rubros, monto vs capacidad, plazo, señales históricas si existen); el usuario puede reordenar por cierre próximo, monto o score simple. Navega al detalle (UC-002) o al análisis IA (UC-004) si está disponible.
4. Sobre una oportunidad elegida, solicita **generar propuesta** (flujo UC-006, disparado desde el panel; si la compra aún no tiene análisis, el sistema encadena análisis → requisitos → generación e informa el progreso).
5. Con la propuesta generada, el usuario puede refinarla en el editor (UC-007) y/o **exportarla a .docx** (`GET /api/proposals/{id}/export?format=docx`).
6. El .docx exportado es fiel a la versión vigente (secciones, datos del CompanyProfile, matriz de cumplimiento resumida) y se almacena también en MinIO `generated/` con su hash, quedando trazable a la versión de propuesta exacta.

## Flujos alternativos y errores

- **A1 — Sin rubros confirmados:** el panel invita a completar el onboarding (UC-010) en lugar de mostrar resultados vacíos sin explicación.
- **A2 — Sin coincidencias:** estado vacío con última sincronización visible y sugerencia de ampliar rubros.
- **A3 — Matching ambiguo (rubro genérico):** score bajo y explicación visible; el usuario decide — el sistema no oculta ni infla coincidencias.
- **A4 — Export falla:** la propuesta sigue disponible en la UI; export reintentable; error auditado.

## Postcondiciones
Solo lectura sobre compras; la generación/edición sigue los flujos y garantías de UC-006/007; export versionado y auditado.

## Eventos / Datos / APIs
`ProposalGenerated.v1` (vía UC-006), `AuditEventCreated.v1`. CompraAgil, CompanyProfile.rubros, Proposal. `GET /api/opportunities`, `POST /api/proposals`, `GET /api/proposals/{id}/export?format=docx`.
