# 02 — Container Diagram (C4 nivel 2)

```mermaid
C4Container
    title PPIP — Contenedores
    Person(user, "Usuario")
    System_Boundary(ppip, "PPIP - Docker Compose") {
        Container(spa, "Frontend", "Angular 20", "Dashboard, compras, documentos, análisis, RAG, propuestas, compliance, trazabilidad")
        Container(gw, "Gateway", "Traefik", "Routing, TLS, rate limiting")
        Container(api, "Platform API", ".NET 10", "Modular monolith: Procurement, Document, Knowledge, Proposal, Compliance, Audit")
        Container(sync, "Sync Worker", ".NET 10", "Sincronización incremental ChileCompra")
        Container(docw, "Document Worker", ".NET 10", "Pipeline documental + OCR")
        Container(aiw, "AI Worker", ".NET 10", "Análisis, requisitos, generación de propuestas")
        ContainerDb(mongo, "MongoDB", "Document DB", "Operacional + raw payloads")
        ContainerDb(pg, "PostgreSQL", "RDBMS", "Auditoría / reporting")
        ContainerDb(minio, "MinIO", "Object Storage", "Documentos binarios")
        ContainerDb(qdrant, "Qdrant", "Vector DB", "Embeddings + metadata")
        ContainerDb(redis, "Redis", "Cache", "Cache, locks, dedupe")
        Container(mq, "RabbitMQ", "Broker", "Eventos de dominio")
        Container(kc, "Keycloak", "IAM", "AuthN/AuthZ, RBAC")
        Container(ollama, "Ollama", "LLM local", "Modelos locales")
    }
    System_Ext(cc, "ChileCompra API")
    Rel(user, spa, "Usa", "HTTPS")
    Rel(spa, gw, "REST/JSON", "HTTPS")
    Rel(gw, api, "Proxy")
    Rel(spa, kc, "OIDC login")
    Rel(api, kc, "Token validation")
    Rel(api, mongo, "Lee/escribe")
    Rel(api, qdrant, "Consulta vectorial")
    Rel(api, minio, "URLs firmadas / streaming")
    Rel(api, redis, "Cache/locks")
    Rel(api, mq, "Publica comandos/eventos")
    Rel(sync, cc, "GET /v2/compra-agil", "HTTPS+ticket")
    Rel(sync, mongo, "Raw + normalizado")
    Rel(sync, mq, "CompraAgilDetected/Updated")
    Rel(docw, mq, "Consume/publica etapas")
    Rel(docw, minio, "Almacena binarios")
    Rel(docw, mongo, "Metadata, chunks")
    Rel(docw, qdrant, "Upsert vectores")
    Rel(aiw, mq, "Consume/publica")
    Rel(aiw, ollama, "Inferencia local")
    Rel(aiw, mongo, "Análisis, requisitos, propuestas")
    Rel(api, pg, "Auditoría/reporting")
```

## Responsabilidades y límites

| Contenedor | Expone | No hace |
|---|---|---|
| Platform API | REST /api/* por bounded context | No ejecuta trabajos pesados (delega por eventos) |
| Sync Worker | health endpoints | No sirve tráfico de usuario |
| Document Worker | health endpoints | No llama al LLM de análisis (solo OCR/embeddings) |
| AI Worker | health endpoints | No accede a ChileCompra |
| Gateway | 80/443 | Sin lógica de negocio |

Escalado: workers escalan horizontalmente (competing consumers + idempotencia); la API escala en réplicas detrás de Traefik.
