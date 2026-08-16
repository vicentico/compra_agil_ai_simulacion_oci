# UC-003 — Procesar documentos

| Campo | Valor |
|---|---|
| Actor | Document Worker (ACT-05), OCR Service (ACT-06) |
| Objetivo | Convertir cada documento adjunto en representación unificada, chunks semánticos y vectores indexados |
| Requisitos | FR-010..FR-018, NFR-002, NFR-009 |
| Precondiciones | Evento `CompraAgilDetected/Updated` con documentos; MinIO, Qdrant, RabbitMQ disponibles |

## Flujo principal (pipeline por eventos)

1. `DocumentDetected` → el worker valida URL (allowlist SSRF), tipo y tamaño máximo.
2. `DocumentDownloadRequested` → descarga con retry/backoff; calcula SHA-256.
3. `DocumentDownloaded` → si el hash ya existe para esa compra, corta como duplicado (idempotencia); si es versión nueva del mismo documento, crea `DocumentVersion`.
4. `DocumentStored` → objeto en `chilecompra/{codigo}/original/`; MongoDB guarda metadata + referencia (nunca el binario).
5. `DocumentClassified` → inspección: textual / escaneado / mixto / tablas / imágenes / complejo, según densidad de texto y análisis de páginas.
6. `DocumentParsed` → extracción de texto por página (textual); tablas → representación estructurada; imágenes → `/images/`.
7. `OCRRequired` → páginas de baja densidad van a `IOcrService`; `OCRCompleted` persiste texto + confianza por página en `/ocr/`.
8. `TextExtracted` → representación unificada del documento (páginas, secciones, método de extracción, confianza).
9. `Chunked` → chunking semántico; cada `DocumentChunk` con compraAgilId, documentId, página, sección, chunkType, hash, tokenCount.
10. `Embedded` → embeddings por chunk (proveedor abstraído, versión de modelo registrada).
11. `Indexed` → upsert en Qdrant con metadata de filtrado; publica disponibilidad para análisis (`AIAnalyzed` se dispara en UC-004).

## Flujos alternativos y errores

- **A1 — Descarga falla (404/timeout):** reintentos con backoff; agotados → estado `download_failed`, visible en UI, reintentable manualmente.
- **A2 — PDF corrupto:** clasificación falla → `parse_failed`; el resto de documentos de la compra continúa.
- **A3 — OCR falla o confianza < umbral:** texto marcado `low_confidence`; chunks igualmente indexados con flag; UI advierte calidad.
- **A4 — Evento duplicado o fuera de orden:** dedupe por (documentId, etapa, hash); cada etapa verifica precondiciones y re-encola si su insumo no está listo.
- **A5 — Qdrant caído:** etapa `Indexed` reintenta; el resto del pipeline no se bloquea; job de reconciliación re-indexa pendientes.

## Postcondiciones
Documento navegable por página, chunks indexados y filtrables por compra, cada etapa auditada con correlationId heredado del sync.

## Eventos producidos
`DocumentDetected.v1`, `DocumentDownloaded.v1`, `DocumentExtracted.v1`, `OcrCompleted.v1`, `DocumentChunked.v1`, `EmbeddingCreated.v1`, `AuditEventCreated.v1`

## Datos / APIs
Document, DocumentVersion, DocumentPage, DocumentChunk, Embedding. `GET /api/compra-agil/{id}/documents`, `GET /api/documents/{id}`.
