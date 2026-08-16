# 01 — Operaciones, Demo Mode y seeding

## Developer experience (MP2 §41)

`docker compose up -d` levanta todo; perfiles: `core` (datos+broker+identidad), `app` (API+workers+frontend), `obs`, `demo`. Health checks encadenados con `depends_on: service_healthy`. `make`/`just` targets: `up`, `down`, `seed`, `demo`, `logs`, `test`. `.env.example` documenta toda variable; ningún secreto real en el repo.

## Demo Mode (FR-051, MP2 §42)

Objetivo: demostrar el pipeline completo sin API externa ni proveedores cloud. Al activar el perfil `demo`:
1. Seed carga: 3 Compras Ágiles ficticias (una abierta con documentos, una por cerrar, una cerrada), PDFs ficticios (uno textual, uno escaneado para OCR, uno mixto con tabla), CompanyProfile ficticio completo, plantilla de propuesta.
2. El pipeline procesa los documentos reales del seed (OCR real con Tesseract sobre el PDF escaneado; embeddings reales locales o mock según configuración).
3. Guion demostrable: dashboard → detalle de compra → documentos y páginas OCR → pregunta RAG con evidencia → análisis IA → matriz de requisitos → generar propuesta → editar y regenerar sección → compliance → trazabilidad end-to-end.

Todo dato sembrado lleva `isDemoData: true` visible en la UI (NFR-015): nunca confundible con datos reales.

## Seeding (`/scripts/seed`, FR-052)

Contenido: compra ficticia + raw payload simulado, documentos ficticios generados (no reales de terceros), empresa ficticia, requisitos esperados (sirven además como dataset de evaluación), embeddings (mock o reales), propuesta de ejemplo, eventos y auditoría coherentes con el lineage. Seed idempotente: re-ejecutar no duplica.

## Runbooks mínimos (se completan por fase)
Credencial ChileCompra inválida · DLQ con mensajes · reconciliación Qdrant↔Mongo · reproceso de documento · rotación de secretos · backup/restore de volúmenes (Mongo, MinIO, Postgres).
