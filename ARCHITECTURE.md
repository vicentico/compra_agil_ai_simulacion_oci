# ARCHITECTURE.md — Síntesis

Documento índice. El detalle vive en [docs/04-architecture/](docs/04-architecture/).

## Estilo arquitectónico

**Modular monolith + workers desacoplados por eventos**, evolucionable a microservicios mediante strangler pattern (ver [ADR-001](docs/05-architecture-decisions/ADR-001-architecture-style.md) y [ADR-012](docs/05-architecture-decisions/ADR-012-microservices-boundaries.md)).

Piezas desplegables iniciales:

1. **Gateway** (Traefik) — enrutamiento, TLS, rate limiting.
2. **Platform API** (.NET 10, modular monolith) — módulos Procurement, Document, Analysis, RAG, Proposal, Compliance, Audit, CompanyProfile detrás de una sola API con límites lógicos estrictos.
3. **Sync Worker** — sincronización incremental con ChileCompra.
4. **Document Worker** — pipeline documental (descarga → almacenamiento → clasificación → extracción → OCR → chunking → embeddings → indexación).
5. **AI Worker** — análisis IA, extracción de requisitos, generación de propuestas.
6. **Frontend Angular 20**.

Infraestructura: MongoDB (operacional), PostgreSQL (auditoría/reporting), MinIO (objetos), Qdrant (vectores), Redis (cache/locks), RabbitMQ (eventos), Keycloak (identidad), stack OTel/Prometheus/Grafana/Loki.

## Principios

Event-driven, idempotente, auditable, observable, seguro por diseño, dominio independiente de infraestructura, IA como decision support con evidencia obligatoria, sin sobreingeniería (deployment boundary ≠ logical boundary).

## Diagramas

Contexto, contenedores, componentes, despliegue, flujos de datos y eventos, seguridad, observabilidad, IA, RAG y mapeo OCI: ver [docs/04-architecture/](docs/04-architecture/).
