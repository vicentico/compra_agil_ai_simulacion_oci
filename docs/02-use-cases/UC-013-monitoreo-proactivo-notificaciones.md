# UC-013 — Monitoreo proactivo y notificaciones curadas

| Campo | Valor |
|---|---|
| Actor | Monitor/Notification Worker (ACT-13); Usuario recibe |
| Objetivo | Que el usuario no tenga que buscar: el sistema vigila continuamente y avisa solo de oportunidades de alto potencial |
| Requisitos | FR-063, FR-064, FR-061, NFR-021 |
| Precondiciones | Sync operativo (UC-001); rubros confirmados y score configurado (UC-010, FR-061); preferencias de notificación del usuario |

## Flujo principal

1. Al finalizar cada ciclo de sync, el worker evalúa las compras nuevas/actualizadas con el score de ganabilidad (FR-061).
2. Las que superan el umbral configurable publican `HighPotentialCompraDetected.v1` (una vez por compra/versión — idempotente).
3. El dispatcher de notificaciones aplica las **preferencias del usuario** (umbral propio, frecuencia, silenciados) y el **curado**: agrupa en digest por período, destaca datos críticos (cierre, monto, organismo, score y por qué coincide), nunca envía el listado completo.
4. Entrega por dos canales: centro de notificaciones in-app (`GET /api/notifications`) y email digest (SMTP configurable; MailHog en entorno local).
5. Cada despacho publica `NotificationDispatched.v1` y se audita; desde la notificación el usuario navega directo a la oportunidad (UC-011).
6. Si OQ-10 se resuelve positivo, el mismo worker rastrea adjudicaciones/órdenes de compra y sugiere outcomes (UC-012 A2).

## Flujos alternativos y errores

- **A1 — SMTP caído:** la notificación in-app se entrega igual; el email se reintenta con backoff; sin duplicados (dedupe por notificationId).
- **A2 — Usuario en opt-out:** solo in-app, o nada si silenció todo; el sistema respeta la preferencia sin excepciones.
- **A3 — Ráfaga de coincidencias (sync grande):** el curado agrupa en un solo digest; nunca N correos (RSK-15).
- **A4 — Score no configurado:** el monitoreo usa el matching simple de rubros (FR-057) como criterio de respaldo.

## Postcondiciones
Usuario informado sin fatiga; todo despacho trazable (qué se notificó, por qué, con qué score y umbral).

## Eventos / Datos / APIs
`HighPotentialCompraDetected.v1`, `NotificationDispatched.v1`, `AuditEventCreated.v1`. Notification, NotificationPreference, CompraAgil, WinnabilityScore. `GET /api/notifications`, `PUT /api/notifications/preferences`.
