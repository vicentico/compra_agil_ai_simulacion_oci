# UC-014 — Gestionar cuotas de la API ChileCompra (SuperAdmin)

| Campo | Valor |
|---|---|
| Actor | SuperAdmin (ACT-12) |
| Objetivo | Evitar bloqueos del proveedor: ajustar en caliente el ritmo de peticiones hacia la API Compra Ágil v2 sin redeploy |
| Requisitos | FR-065, FR-066, NFR-020, NFR-021, NFR-007 |
| Precondiciones | Usuario con rol superadmin; Sync Worker operativo |

## Flujo principal

1. El SuperAdmin abre el panel exclusivo de cuotas (`GET /api/admin/rate-limits`): configuración vigente (requests/minuto, requests/hora, concurrencia máxima, ventanas de pausa, tamaño de página) y métricas en vivo (uso actual, 429 recientes, estado del circuit breaker).
2. Modifica los parámetros (`PUT /api/admin/rate-limits`) indicando motivo del cambio.
3. La configuración se persiste en MongoDB y se propaga vía Redis + `RateLimitConfigChanged.v1`; el Sync Worker la aplica **en el siguiente request, sin reinicio** (hot reload, NFR-021).
4. El cambio queda auditado con valores previo/nuevo, actor y motivo.
5. El throttling dinámico convive con las salvaguardas no negociables: respeto de 429/Retry-After y circuit breaker siguen activos aunque la cuota configurada sea alta (defensa en profundidad).

## Flujos alternativos y errores

- **A1 — Valores fuera de rango seguro:** validación rechaza (422) límites absurdos (ej. 0 o por sobre el máximo documentado del proveedor); rangos definidos en configuración.
- **A2 — Usuario admin o inferior intenta acceder:** 403; intento auditado (T10).
- **A3 — Redis caído:** el worker mantiene la última configuración conocida (cacheada localmente) y alerta; el cambio se aplica al recuperarse.
- **A4 — 429 persistentes pese a la cuota:** el circuit breaker abre y se notifica al SuperAdmin con recomendación de reducir límites.

## Postcondiciones
Cuotas efectivas alineadas con las restricciones del proveedor; historial completo de cambios de configuración.

## Eventos / Datos / APIs
`RateLimitConfigChanged.v1`, `AuditEventCreated.v1`. RateLimitConfig, SyncExecution (métricas). `GET/PUT /api/admin/rate-limits`.
