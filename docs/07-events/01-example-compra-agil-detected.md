# 01 — Contrato de ejemplo: CompraAgilDetected.v1

## Identificación
- **eventType**: `CompraAgilDetected` · **version**: 1 · **routing key**: `procurement.compra-agil-detected.v1`
- **Productor**: Sync Worker · **Consumidores**: Document Worker (dispara pipeline), Platform API (notificaciones/dashboard)

## Payload

```json
{
  "eventId": "018f3c1e-...",
  "eventType": "CompraAgilDetected",
  "version": 1,
  "timestamp": "2026-08-16T09:30:12Z",
  "correlationId": "sync-2026-08-16-0930-abc",
  "causationId": "cmd-sync-cycle-449",
  "producer": "sync-worker@0.1.0",
  "isDemoData": false,
  "payload": {
    "compraAgilId": "1234-56-COT26",
    "codigo": "1234-56-COT26",
    "nombre": "Adquisición de insumos de laboratorio",
    "organismoCodigo": "6945",
    "fechaCierre": "2026-08-22T15:00:00Z",
    "montoDisponible": { "amount": 4500000, "currency": "CLP" },
    "rawPayloadId": "raw_663d",
    "documentRefs": [
      { "documentId": "doc_9f2c", "sourceUrl": "https://.../bases.pdf", "declaredName": "Bases_CompraAgil.pdf" }
    ]
  }
}
```

## JSON Schema (extracto normativo)
`payload.compraAgilId` (string, required) · `payload.rawPayloadId` (string, required) · `payload.documentRefs` (array, puede ser vacía) · campos monetarios siempre `{amount, currency}` · fechas ISO-8601 UTC.

## Semántica
Se emite **exactamente una vez por compra nueva real** (garantizado por unique index sobre codigo + outbox). Reprocesar el mismo raw no re-emite. Consumidor idempotente: si el documento ya existe con el mismo hash, la etapa corta sin efecto.

## Evolución
Aditivo permitido (nuevos campos opcionales). Cambio de tipo/semántica → `CompraAgilDetected.v2` con dual-publish ≥ 1 fase de transición.
