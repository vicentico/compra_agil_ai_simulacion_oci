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
- **Human-in-the-Loop en extracción documental** (FR-053/054): extracción deficiente genera tarea de revisión; el usuario valida el texto o carga el documento manualmente y el pipeline se reanuda.
- **Perfilado inteligente de rubros vía LLM** (FR-055/056): inferencia de rubros desde la descripción del negocio en lenguaje natural, persistida estructuradamente y auditada/confirmada por el usuario.
- **Dashboard de oportunidades por matching de rubros** (FR-057): compras ágiles coincidentes con el perfil, con datos críticos para decisión.
- **Export de propuesta a .docx editable** (FR-058), generable on-demand desde el panel de oportunidades.
- **Registro histórico de resultados y dashboard de efectividad** (FR-059/060): outcome de cada propuesta (adjudicada/no adjudicada/desierta) registrado manualmente por el usuario, con telemetría de negocio (win-rate, montos, ROI).
- **Score de ganabilidad heurístico y explicable** (FR-061): reglas ponderadas configurables que ordenan el panel de oportunidades; la recalibración automática con ML queda FUTURE (FR-062) hasta acumular datos reales.
- **Monitoreo proactivo y notificaciones curadas** (FR-063/064): detección de compras de alto potencial tras cada sync + centro de notificaciones in-app y email digest.
- **Throttling dinámico hacia ChileCompra con panel SuperAdmin** (FR-065/066): cuotas configurables en caliente, cambios auditados.

## COULD HAVE

- Extracción de tablas avanzada y bounding boxes.
- Alertas configurables (compras por cerrar, alto potencial).
- Export de propuesta a PDF (el export .docx es SHOULD, FR-058).
- Kong/YARP como alternativa de gateway.

## FUTURE (explícitamente fuera de FASE 0-19)

- Licitaciones, órdenes de compra, convenios marco.
- Historial de organismos, análisis de competencia, forecasting, pricing/market intelligence, y **recalibración automática (ML) del score de ganabilidad** (FR-062) — la versión heurística sí es SHOULD (FR-061).
- Kafka/Redpanda, Kubernetes/OKE (la arquitectura los prevé; no se implementan hasta tener razón medida).
- Multi-tenancy, facturación, SLA productivo.

Toda funcionalidad implementada debe trazar a un requisito de este alcance; nada se construye «porque parece interesante».
