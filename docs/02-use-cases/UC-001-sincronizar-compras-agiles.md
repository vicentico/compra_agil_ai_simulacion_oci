# UC-001 — Sincronizar Compras Ágiles

| Campo | Valor |
|---|---|
| Actor | Sync Worker (ACT-04); Administrador para sync manual |
| Objetivo | Mantener la copia local de Compras Ágiles actualizada de forma incremental, idempotente y auditable |
| Requisitos | FR-001..FR-005, NFR-001, NFR-003, NFR-020 |
| Precondiciones | Ticket ChileCompra configurado (fuera del código); MongoDB y RabbitMQ disponibles; SyncCheckpoint existente o inicial |

## Flujo principal

1. El scheduler (o el administrador vía `POST /api/sync/compra-agil`) dispara un ciclo de sincronización con un `correlationId` nuevo.
2. El worker lee el `SyncCheckpoint` y calcula la ventana incremental (`cambio_desde`/`cambio_hasta` o `ttl_cambio_ms`).
3. Consulta `GET /v2/compra-agil` paginado, respetando rate limits.
4. Por cada página: valida el schema de respuesta, persiste el `RawCompraAgilPayload` (payload, URL, timestamp, status, hash, versión API, correlationId).
5. Normaliza cada registro y compara hash contra la copia local.
6. Registro inexistente → crea `CompraAgil`, publica `CompraAgilDetected.v1`.
7. Registro con hash distinto → crea nueva versión, publica `CompraAgilUpdated.v1` con diff resumido.
8. Registro sin cambios → no escribe, incrementa contador `unchanged`.
9. Al finalizar: actualiza `SyncCheckpoint` (lastSuccessfulSync, ventana, contadores, duración) y persiste `SyncExecution` + AuditEvent.

## Flujos alternativos y errores

- **A1 — 429/Retry-After:** el worker espera lo indicado, reduce tasa; si persiste, aborta el ciclo dejando checkpoint intacto (el siguiente ciclo retoma). |
- **A2 — 401/403:** marca credencial inválida, alerta, no reintenta hasta intervención (evita bloqueo de cuenta).
- **A3 — 5xx/timeout:** retry con backoff exponencial + circuit breaker; ciclo parcial registra páginas procesadas.
- **A4 — Respuesta malformada:** guarda raw igualmente, marca registro como `quarantined`, continúa con el resto.
- **A5 — Ejecución concurrente:** lock distribuido en Redis; segundo ciclo termina inmediatamente como `skipped`.

## Postcondiciones

Copia local consistente con la ventana sincronizada; eventos publicados exactamente una vez por cambio real; checkpoint avanzado solo tras éxito.

## Eventos producidos
`CompraAgilDetected.v1`, `CompraAgilUpdated.v1`, `AuditEventCreated.v1`

## Datos involucrados
RawCompraAgilPayload, CompraAgil, SyncCheckpoint, SyncExecution, AuditEvent

## APIs involucradas
Externa: `GET /v2/compra-agil`, `GET /v2/compra-agil/{codigo}`. Interna: `POST /api/sync/compra-agil`, `GET /api/sync/status`.
