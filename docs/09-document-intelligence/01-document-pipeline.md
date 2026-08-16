# 01 — Pipeline de Document Intelligence

Diagrama de eventos en [../04-architecture/06-event-flow.md](../04-architecture/06-event-flow.md); casos de fallo por etapa en [../14-reliability/](../14-reliability/).

## Etapas

| # | Etapa | Entrada | Salida | Fallo → estado |
|---|---|---|---|---|
| 1 | **Download** | sourceUrl (allowlist, tipo, tamaño máx) | binario temporal + SHA-256 | download_failed (retry backoff; manual) |
| 2 | **Validation** | binario | content-type + magic bytes verificados; IMalwareScanner | rejected (auditado, no procesable) |
| 3 | **Storage** | binario válido | objeto `original/` + DocumentVersion (hash único) | storage_failed (retry) |
| 4 | **Classification** | PDF | clase: textual/escaneado/mixto/tablas/imágenes/complejo + densidad de texto por página | parse_failed |
| 5 | **Extraction** | PDF textual | texto por página + secciones + método | parse_failed parcial (páginas OK continúan) |
| 6 | **OCR** | páginas densidad < umbral | texto + confianza por página (`ocr/`) | ocr_failed / low_confidence (continúa con flag) |
| 7 | **Image/Table processing** | páginas con tablas/imágenes | tablas estructuradas; imágenes `images/` | degradado: tabla como texto plano |
| 8 | **Unification** | textos + tablas | representación unificada del documento | — |
| 9 | **Chunking** | representación unificada | DocumentChunks semánticos | chunk_failed |
| 10 | **Embedding** | chunks | vectores + modelVersion | embed_failed (retry; batch parcial reanuda) |
| 11 | **Indexing** | vectores | upsert Qdrant + verificación | index_failed (reconciliación) |

Cada etapa: consumidor RabbitMQ propio, idempotente (key = documentVersionId + etapa + hash de entrada), reintentable individualmente, audita entrada/salida, propaga correlationId.

## Clasificación PDF (detalle)

Inspección con librería PDF (.NET): páginas, texto extraíble, imágenes por página. `textDensity = chars extraíbles / área estimada`. Umbrales configurables: densidad ≥ D1 → textual; ≤ D2 → escaneado; intermedio → mixto (OCR solo en páginas pobres). Tablas detectadas por heurística de layout (+ librería específica en FASE 8). Todo umbral vive en configuración, jamás hardcodeado.

## Chunking semántico

Prioridad de cortes: títulos/numeración de secciones → subsecciones → párrafos → ítems de lista → filas agrupadas de tabla → anexos. Tamaño objetivo por tokens (configurable, p.ej. 256-512) con overlap pequeño solo cuando el corte semántico excede el máximo. Cada chunk conserva: compraAgilId, documentId, versión, página(s), section, subsection, chunkType, text, hash, tokenCount. Un requisito detectable (patrones "deberá", "se exige", numerales) se etiqueta chunkType=requirement — mejora retrieval de UC-004/005.

## OCR

Puerto `IOcrService` (ADR-006): `RecognizeAsync(pageImage) → {text, confidence, words[]?}`. Implementaciones: LocalOcrService (Tesseract, spa+eng), MockOcrService (fixtures determinísticos), CloudOcrService (futuro, OCI Document Understanding). Confianza < umbral → flag low_confidence visible en UI y penalización de confidence en evidencia derivada.
