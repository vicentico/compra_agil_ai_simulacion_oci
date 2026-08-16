# ADR-006 — Estrategia OCR: abstracción con local por defecto

**Estado:** Accepted · **Fecha:** 2026-08-16

## Context
PDFs escaneados/mixtos requieren OCR (MP §11-12); destino OCI: Document Understanding. Demo debe funcionar offline.

## Options
1. **Puerto `IOcrService` + LocalOcrService (Tesseract) + MockOcrService (tests/demo) + CloudOcrService futuro.**
2. Solo servicio cloud (OCI/Google/Azure).
3. Modelo vision-LLM para OCR.

## Decision
Opción 1. Selección por configuración; confianza por página persistida y normalizada [0..1]; páginas van a OCR solo si densidad de texto < umbral configurable (OcrPolicy).

## Rationale
Tesseract es suficiente para demostrar el pipeline y funciona offline (requisito Demo Mode); la abstracción permite subir calidad después sin tocar dominio (mismo patrón que ILlmProvider). Cloud-only rompe la demo offline y añade costo. Vision-LLM mezcla responsabilidades y hace el costo impredecible; puede añadirse como implementación más adelante.

## Consequences
- (+) Offline, gratis, intercambiable; mock determinístico para tests.
- (−) Calidad Tesseract limitada en escaneos pobres (mitigado: flag low_confidence visible, RSK-04).

## Rejected Alternatives
Cloud-only, vision-LLM-only (arriba).

## Future Reconsideration
Al migrar: CloudOcrService → OCI Document Understanding; comparar calidad con dataset de /evaluation.
