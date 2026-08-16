# UC-002 — Consultar Compra Ágil

| Campo | Valor |
|---|---|
| Actor | Usuario (ACT-01) |
| Objetivo | Explorar y consultar Compras Ágiles sincronizadas con su estado de procesamiento |
| Requisitos | FR-006..FR-008 |
| Precondiciones | Usuario autenticado (JWT); al menos un ciclo de sync o seed data |

## Flujo principal

1. El usuario abre el dashboard: métricas agregadas (nuevas, abiertas, por cerrar, modificadas, analizadas, propuestas, compliance promedio).
2. Navega al listado con búsqueda, filtros (estado, organismo, fechas, monto), paginación y ordenamiento.
3. Selecciona una compra: `GET /api/compra-agil/{id}` retorna detalle normalizado + documentos + estado del pipeline + análisis disponible.
4. Puede navegar a documentos, análisis, requisitos, RAG, propuestas o trazabilidad.

## Flujos alternativos y errores

- **A1 — Sin resultados:** estado vacío con indicación del último sync.
- **A2 — Compra sin documentos procesados:** el detalle muestra el estado por etapa del pipeline (pendiente/en curso/fallido) sin bloquear la consulta.
- **A3 — 401/403:** redirección a login / mensaje de permisos.

## Postcondiciones
Solo lectura; consulta auditada de forma agregada (no por vista individual, para evitar ruido).

## Eventos producidos
Ninguno de dominio.

## Datos / APIs
CompraAgil, Document, AIAnalysis (lectura). `GET /api/compra-agil`, `GET /api/compra-agil/{id}`, `GET /api/compra-agil/{id}/documents`.
