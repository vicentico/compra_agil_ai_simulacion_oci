# OPERATIONS.md — Síntesis

Detalle en [docs/16-operations/](docs/16-operations/).

`docker compose up -d` levanta el sistema completo con health checks (`/health`, `/ready`, `/live`), seed data y **Demo Mode**: pipeline completo demostrable (compra ficticia → documentos → OCR → RAG → análisis → propuesta → compliance → trazabilidad) sin depender de la API externa. Observabilidad: [docs/13-observability/](docs/13-observability/). Fallos y recuperación: [docs/14-reliability/](docs/14-reliability/).
