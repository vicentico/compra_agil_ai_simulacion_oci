# Architecture Decision Records

Formato: Context · Problem · Options · Decision · Rationale · Consequences · Rejected Alternatives · Future Reconsideration. Estados: Proposed / Accepted / Superseded. Los trade-offs nunca se ocultan.

| ADR | Título | Estado |
|---|---|---|
| [ADR-001](ADR-001-architecture-style.md) | Modular monolith + workers, event-driven | Accepted |
| [ADR-002](ADR-002-mongodb.md) | MongoDB operacional; PostgreSQL diferido a reporting/auditoría | Accepted |
| [ADR-003](ADR-003-rabbitmq.md) | RabbitMQ como bus de eventos inicial | Accepted |
| [ADR-004](ADR-004-minio.md) | MinIO como object storage | Accepted |
| [ADR-005](ADR-005-qdrant.md) | Qdrant como vector database | Accepted |
| [ADR-006](ADR-006-ocr-strategy.md) | Abstracción IOcrService con OCR local por defecto | Accepted |
| [ADR-007](ADR-007-llm-abstraction.md) | Abstracción ILlmProvider multi-proveedor | Accepted |
| [ADR-008](ADR-008-rag-strategy.md) | RAG por Compra Ágil con evidencia obligatoria | Accepted |
| [ADR-009](ADR-009-api-gateway.md) | Traefik como gateway | Accepted |
| [ADR-010](ADR-010-authentication.md) | Keycloak OIDC + JWT + RBAC | Accepted |
| [ADR-011](ADR-011-observability.md) | OpenTelemetry + Prometheus/Grafana/Loki | Accepted |
| [ADR-012](ADR-012-microservices-boundaries.md) | Criterios de extracción de microservicios (strangler) | Accepted |
