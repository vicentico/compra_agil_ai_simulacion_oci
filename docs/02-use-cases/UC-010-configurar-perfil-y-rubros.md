# UC-010 — Configurar perfil y rubros (onboarding)

| Campo | Valor |
|---|---|
| Actor | Usuario/Administrador (ACT-01/02), AI Service (ACT-07) |
| Objetivo | Reducir la fricción del onboarding: el usuario describe su negocio en lenguaje natural y el sistema infiere y estructura sus rubros, que el usuario audita y confirma |
| Requisitos | FR-030, FR-055, FR-056, NFR-010, NFR-018 |
| Precondiciones | Usuario autenticado con rol editor/admin; proveedor LLM disponible |

## Flujo principal

1. Durante el onboarding (o desde configuración), el usuario completa el CompanyProfile e ingresa una **descripción libre del negocio** («Vendemos y distribuimos insumos de laboratorio y equipamiento médico en la V Región...»).
2. El usuario solicita la inferencia (`POST /api/company-profile/infer-rubros`).
3. El AI Service ejecuta el prompt versionado `analysis/infer-rubros-vX.Y` con structured output; la salida se valida contra JSON Schema `RubrosResult` antes de persistir (NFR-010).
4. Cada rubro inferido se persiste en el CompanyProfile (MongoDB) como `{code, name, confidence, source: "inferred", promptVersion, modelVersion}`.
5. La UI presenta la lista para **auditoría**: el usuario confirma, edita o descarta cada rubro; puede agregar rubros manuales (`source: "manual"`).
6. Al confirmar, los rubros pasan a `source: "confirmed"`; cada cambio genera AuditEvent con actor humano.
7. Solo los rubros confirmados alimentan el matching de oportunidades (UC-011).

## Flujos alternativos y errores

- **A1 — LLM caído:** el usuario puede cargar rubros manualmente; la inferencia queda disponible para reintentar (degradación elegante, NFR-006).
- **A2 — Salida inválida contra schema:** reintento acotado con feedback del error; agotado → se informa y se ofrece carga manual; nunca se persiste salida no validada.
- **A3 — Descripción demasiado breve/ambigua:** el sistema responde con rubros de baja confianza marcados como tales y sugiere ampliar la descripción; no inventa especificidad (regla MP §36).
- **A4 — Re-inferencia tras editar la descripción:** genera nueva propuesta de rubros sin tocar los confirmados; el usuario decide fusionar.

## Postcondiciones
CompanyProfile con rubros estructurados y estado de confirmación; trazabilidad completa de qué fue inferido (con qué prompt/modelo) y qué fue decidido por el humano.

## Eventos / Datos / APIs
`AuditEventCreated.v1`. CompanyProfile (rubros[]), AIExecution, PromptVersion. `POST /api/company-profile/infer-rubros`, `PUT /api/company-profile/rubros`, `GET /api/company-profile`.
