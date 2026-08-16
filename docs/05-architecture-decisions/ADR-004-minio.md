# ADR-004 — MinIO como object storage

**Estado:** Accepted · **Fecha:** 2026-08-16

## Context
PDFs y derivados (páginas, imágenes, OCR, extraídos, generados) no deben vivir en MongoDB (MP §10). Destino OCI: Object Storage (API S3-compatible disponible).

## Options
1. **MinIO** (S3-compatible, self-hosted). 2. Filesystem local + volumen. 3. GridFS de MongoDB.

## Decision
MinIO con bucket `chilecompra` y prefijos `/{codigo}/original|pages|images|ocr|extracted|generated/`, hash SHA-256 por objeto, metadata en MongoDB con referencia.

## Rationale
API S3 = migración a OCI Object Storage por cambio de endpoint/credenciales; URLs firmadas para el frontend; versionado y políticas nativas. Filesystem no ofrece API ni firma de URLs y acopla al host; GridFS mete binarios en la base operacional, exactamente lo prohibido.

## Consequences
- (+) Migración trivial; separación binario/metadata limpia.
- (−) Un servicio más que operar y respaldar (aceptado: es el símil OCI buscado).

## Rejected Alternatives
Filesystem, GridFS (arriba).

## Future Reconsideration
Solo si OCI S3-compat mostrara límites; entonces adapter nativo OCI SDK tras el puerto IObjectStorage.
