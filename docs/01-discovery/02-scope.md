# 02 — Alcance (MoSCoW)

## MUST HAVE

- Sincronización incremental e idempotente de Compras Ágiles (API v2: `GET /v2/compra-agil`, `GET /v2/compra-agil/{codigo}`) con checkpoint, retry, circuit breaker y manejo de 401/403/404/429/500/503 + Retry-After.
- Persistencia de raw payload + modelo normalizado (MongoDB) y detección de nuevos/modificados/sin cambios.
- Descarga y almacenamiento de documentos adjuntos en MinIO con hash SHA-256 y versionado.
- Pipeline documental por etapas reintentables: clasificación PDF (textual/escaneado/mixto/tablas/imágenes), extracción de texto, OCR condicional (abstracción IOcrService), chunking semántico, embeddings, indexación en Qdrant.
- RAG por Compra Ágil con filtrado obligatorio por `compraAgilId` y evidencia en cada respuesta.
- Análisis IA estructurado (resumen ejecutivo, requisitos, condiciones, fechas, montos, riesgos, opportunityScore) con separación FACT/INFERENCE/RECOMMENDATION/UNKNOWN y validación por JSON Schema.
- Extracción de requisitos con categoría, obligatoriedad, evidencia y confianza.
- CompanyProfile con datos reales de la empresa; propuesta generada desde plantilla + perfil + requisitos + RAG.
- Propuesta editable y versionada (nunca sobrescribir versiones), regeneración de secciones por IA con evidencia.
- Compliance engine independiente del LLM (reglas determinísticas primero) con resultado PASS/PARTIAL/FAIL/UNKNOWN por requisito.
- AuditEvent y trazabilidad navegable end-to-end con correlationId/causationId.
- Observabilidad completa: OpenTelemetry, Prometheus, Grafana, Loki, health checks.
- Seguridad: Keycloak/JWT/RBAC, secretos fuera del código, documentos como untrusted input, protección de prompt injection y SSRF.
- Docker Compose completo + seed data + Demo Mode.
- Frontend Angular 20 con dashboard, detalle de compra, documentos, análisis, requisitos, RAG, editor de propuesta, matriz de compliance y trazabilidad.

## SHOULD HAVE

- PostgreSQL para auditoría/reporting relacional (puede iniciar en MongoDB y migrar).
- Reranking en el pipeline RAG; keyword search híbrida.
- AI cost tracking por compra/documento/usuario/operación/modelo.
- Dataset de evaluación de IA/RAG con métricas (factuality, citation accuracy, precision/recall, hallucination rate).
- Comparación visual de versiones de propuesta.

## COULD HAVE

- Extracción de tablas avanzada y bounding boxes.
- Alertas configurables (compras por cerrar, alto potencial).
- Export de propuesta a PDF/DOCX.
- Kong/YARP como alternativa de gateway.

## FUTURE (explícitamente fuera de FASE 0-19)

- Licitaciones, órdenes de compra, convenios marco.
- Historial de organismos, análisis de competencia, forecasting, scoring comercial, pricing/market intelligence.
- Kafka/Redpanda, Kubernetes/OKE (la arquitectura los prevé; no se implementan hasta tener razón medida).
- Multi-tenancy, facturación, SLA productivo.

Toda funcionalidad implementada debe trazar a un requisito de este alcance; nada se construye «porque parece interesante».
