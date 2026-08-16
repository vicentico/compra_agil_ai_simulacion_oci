# 11 — OCI Mapping

Estrategia de migración por componente en [../17-oci-migration/](../17-oci-migration/).

| # | Componente local | Servicio OCI | Responsabilidad | Límite del símil local | Estrategia de migración |
|---|---|---|---|---|---|
| 1 | Traefik | API Gateway + Load Balancer + WAF | Edge, routing, TLS, rate limit | Sin WAF gestionado real | Reemplazo de edge; rutas declaradas como IaC |
| 2 | Docker containers | Compute / Container Instances | Ejecución | Sin autoscaling | Imágenes idénticas → OCIR → Container Instances |
| 3 | (futuro) Kubernetes | OKE | Orquestación | N/A en POC | Compose → manifests → Helm → OKE |
| 4 | .NET Workers | OCI Functions / Container Instances | Trabajos asíncronos | Workers long-running vs functions efímeras | Workers quedan en containers; funciones puntuales (ej. webhook) a Functions |
| 5 | RabbitMQ | OCI Queue | Mensajería punto a punto | Semántica distinta (colas vs exchanges) | Abstracción IEventBus; mapping topic→queues documentado |
| 6 | (futuro) Kafka/Redpanda | OCI Streaming | Event streaming | N/A en POC | Solo si el volumen lo justifica (ADR-003) |
| 7 | MinIO | Object Storage | Binarios | API S3-compatible ≈ nativa OCI vía S3 compat | Cambio de endpoint+credenciales; misma estructura de buckets |
| 8 | MongoDB | Autonomous JSON Database | Operacional documental | API Mongo-compatible de OCI con diferencias | Driver compatible; revisar índices y agregaciones |
| 9 | PostgreSQL | OCI PostgreSQL | Auditoría/reporting | Ninguno relevante | Dump/restore o replicación |
| 10 | Qdrant | OCI Search / vector solution | Vectores | Sin servicio vectorial 1:1 | Puerto IVectorIndex; índice regenerable desde chunks (re-embedding si cambia dimensión) |
| 11 | Abstracción IA (Ollama/OpenAI/Gemini) | OCI Generative AI | LLM | Modelos distintos | Nuevo provider tras ILlmProvider; re-evaluación en /evaluation |
| 12 | IOcrService local | OCI Document Understanding | OCR | Calidad/formatos distintos | Nuevo adapter; confianza normalizada |
| 13 | Docker secrets | OCI Vault | Secretos | Sin rotación gestionada | Inyección por variables → Vault SDK/CSI |
| 14 | Keycloak | OCI IAM (Identity Domains) | Identidad | Features OIDC distintas | OIDC estándar; mapear realm→domain, roles→groups |
| 15 | Loki | OCI Logging | Logs | Query language distinto | Salida OTLP; cambiar exporter del collector |
| 16 | Prometheus/Grafana | OCI Monitoring | Métricas/dashboards | Dashboards a recrear | Métricas OTel neutrales; export dual durante transición |
| 17 | OTel Collector | OCI APM | Traces | Ninguno relevante | Cambiar exporter OTLP |
| 18 | RabbitMQ eventos | OCI Events | Eventos de plataforma | Alcance distinto | Eventos de dominio permanecen en Queue/Streaming |
| 19 | Registry local | OCIR | Imágenes | Ninguno | Retag + push |
| 20 | Scripts CI local | OCI DevOps | CI/CD | Pipeline por definir | Pipelines declarativos equivalentes |

Regla transversal: **ninguna migración exige tocar el dominio** — solo adaptadores de infraestructura (validación: architecture tests + puertos de ADR-007/ADR-006).
