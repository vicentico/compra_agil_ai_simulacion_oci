# DOMAIN.md — Síntesis

Detalle en [docs/03-domain/](docs/03-domain/).

Bounded contexts: **Procurement** (Compras Ágiles y sincronización), **Document Intelligence** (documentos, OCR, chunks), **Knowledge/RAG** (embeddings, retrieval, evidencia), **Proposal Management** (propuestas versionadas y perfil de empresa), **Compliance** (evaluación de cumplimiento), **Audit/Traceability** (eventos de auditoría y lineage), **Identity/Access** (delegado en Keycloak).

Regla central: el dominio no depende de MongoDB, RabbitMQ, HTTP, proveedores de LLM, Angular, Docker ni OCI. Toda integración pasa por puertos (interfaces) implementados en infraestructura.
