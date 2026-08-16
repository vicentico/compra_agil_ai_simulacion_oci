# 01 — Contrato de ejemplo: POST /api/rag/{compraId}/query

Ejemplo normativo del estilo de especificación que tendrá cada endpoint (OpenAPI YAML se genera en FASE 9).

## Request

```http
POST /api/rag/1234-56-COT26/query
Authorization: Bearer <jwt>
X-Correlation-Id: 7f3a...
Content-Type: application/json

{ "question": "¿Cuál es el plazo máximo de entrega?", "topK": 8, "filters": { "documentId": null, "section": null } }
```

Validación: `question` 3..1000 chars; `topK` 1..20 (default 8). El filtro `compraAgilId` **lo inyecta el servidor desde la ruta** — no es sobrescribible por el cliente (ADR-008).

## Response 200

```json
{
  "answer": "El plazo máximo de entrega es de 10 días hábiles desde la orden de compra.",
  "answerType": "FACT",
  "evidence": [
    {
      "documentId": "doc_9f2c",
      "documentVersion": 1,
      "documentName": "Bases_CompraAgil.pdf",
      "page": 7,
      "chunkId": "chk_a41b",
      "sourceText": "El proveedor deberá entregar los productos en un plazo no superior a 10 días hábiles...",
      "score": 0.87,
      "confidence": 0.91
    }
  ],
  "unanswered": false,
  "execution": { "model": "llama3.1:8b", "promptVersion": "rag-answer-v1.0", "tokensIn": 2314, "tokensOut": 118, "latencyMs": 2140 },
  "correlationId": "7f3a..."
}
```

Sin evidencia suficiente → `{"answer": "Información no encontrada en las fuentes analizadas.", "answerType": "UNKNOWN", "evidence": [], "unanswered": true, ...}`.

## Errores
400 validación · 401/403 auth · 404 compra inexistente · 409 compra sin índice construido (`type: .../index-not-ready`, incluye estado del pipeline) · 429 rate limit por usuario · 503 dependencia caída (`detail` indica cuál: qdrant|llm).
