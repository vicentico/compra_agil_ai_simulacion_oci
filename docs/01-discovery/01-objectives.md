# 01 — Objetivos

## Objetivo general

Construir un Proof of Concept ejecutable localmente mediante Docker que simule una arquitectura empresarial sobre OCI, usando como fuente real la API pública de Compra Ágil de ChileCompra / Mercado Público, y que demuestre de extremo a extremo: integración con API externa, arquitectura orientada a eventos, document intelligence con OCR, RAG con evidencia, IA auditable con human-in-the-loop, compliance, trazabilidad, observabilidad, seguridad y una ruta realista de migración a OCI.

## Objetivos específicos

| ID | Objetivo | Medible por |
|---|---|---|
| OBJ-01 | Sincronizar Compras Ágiles de forma incremental, idempotente y auditable | UC-001, FR-001..FR-005 |
| OBJ-02 | Procesar documentos adjuntos (descarga, almacenamiento, extracción, OCR, chunking, embeddings, indexación) | UC-003, FR-010..FR-018 |
| OBJ-03 | Ofrecer RAG por Compra Ágil con evidencia citada en cada respuesta | UC-005, FR-020..FR-023 |
| OBJ-04 | Analizar automáticamente cada proceso y extraer requisitos estructurados con evidencia y confianza | UC-004, FR-024..FR-027 |
| OBJ-05 | Generar propuestas comerciales/técnicas editables, versionadas y basadas en plantilla + perfil real de empresa | UC-006/UC-007, FR-030..FR-035 |
| OBJ-06 | Evaluar compliance de la propuesta contra los requisitos, con motor independiente del LLM | UC-008, FR-036..FR-038 |
| OBJ-07 | Garantizar trazabilidad end-to-end: API → raw → documento → OCR → chunk → embedding → IA → requisito → propuesta → compliance | UC-009, FR-040..FR-042 |
| OBJ-08 | Ser demostrable offline mediante Demo Mode y seed data | FR-050..FR-052 |
| OBJ-09 | Documentar mapeo componente local → servicio OCI con estrategia de migración | docs/17-oci-migration |

## No-objetivos (en esta etapa)

Licitaciones públicas (LP/LE), órdenes de compra, convenios marco, análisis de competencia, forecasting, pricing intelligence, multi-tenancy comercial, operación productiva real. Ver [02-scope.md](02-scope.md).

## Pregunta que el proyecto debe poder responder

> «¿Cómo construiría una plataforma empresarial de Procurement Intelligence sobre OCI, partiendo de un entorno Docker local?»

El repositorio (docs + arquitectura + código + tests + trazabilidad) constituye conjuntamente la respuesta.
