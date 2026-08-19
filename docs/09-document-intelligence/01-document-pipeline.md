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

## Estado de implementación (FASE 7-8, 2026-08-19)

**Etapas 1-9 implementadas** (`src/modules/documents/Ppip.DocumentIntelligence.*`, ver docs/03-domain/02-domain-model.md y docs/ROADMAP.md notas de cierre de FASE 7-8). Diferencias reales vs. lo especificado aquí, encontradas construyendo/probando contra binarios reales:

- **Etapas 10-11 (Embedding/Indexing) no están en esta implementación** — quedan en FASE 9 (RAG), que además cierra OQ-03 (modelo de embeddings).
- **"Cada etapa: consumidor RabbitMQ propio"** todavía no es así — no existe ningún consumidor de eventos real en el sistema (ni aquí ni en Procurement). El pipeline completo (descarga→...→chunking) corre síncrono dentro de un único orquestador por HTTP interno (`/internal/documents/download`, `/internal/documents/{id}/process`), no como etapas desacopladas por evento. `IIdempotencyStore` (definido desde FASE 4) sigue sin adaptador real por la misma razón.
- **Etapa 7 (Image/Table processing) solo parcial:** se detecta *que* una página tiene layout de tabla o imágenes embebidas (heurística de posición de palabras / extracción PNG de PdfPig), pero no se construye una representación estructurada de filas/celdas — FR-015 es SHOULD, se difirió deliberadamente el resto.
- **`IPdfExtractor.EmbeddedImages`** son imágenes rasterizadas ya embebidas en la página (vía PdfPig), no una renderización del contenido completo de la página — cubre el caso típico de un PDF escaneado (la imagen embebida ES la página), pero no rasteriza contenido vectorial/texto.
- **Los umbrales de densidad de clasificación son provisionales** — sin un corpus real de Compras Ágiles para calibrarlos (OQ-02 sigue abierta), son un punto de partida razonable, no una medición.
- **`TesseractOcrService` está implementado pero no se validó contra Tesseract real en la sesión que lo construyó** — el Dockerfile de `Ppip.DocumentWorker` instala `tesseract-ocr`+`tesseract-ocr-spa`+`tesseract-ocr-eng`, pero `Ppip:Ocr:Provider` por defecto sigue siendo `MockOcrService` (validado, sin dependencias nativas).
