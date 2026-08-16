# OCI-MIGRATION.md — Síntesis

Detalle en [docs/17-oci-migration/](docs/17-oci-migration/) y mapeo por servicio en [docs/04-architecture/11-oci-mapping.md](docs/04-architecture/11-oci-mapping.md).

| Local | OCI |
|---|---|
| Traefik | API Gateway / Load Balancer / WAF |
| Docker containers | Compute → OKE |
| .NET Workers | Functions / Container Instances |
| RabbitMQ | Queue / Streaming |
| MinIO | Object Storage |
| MongoDB | Autonomous JSON Database |
| PostgreSQL | OCI PostgreSQL |
| Qdrant | Solución vector/search compatible |
| Abstracción IA | OCI AI Services / proveedor externo |
| Abstracción OCR | OCI Document Understanding |
| Docker secrets | OCI Vault |
| Keycloak | OCI IAM |
| Loki / Prometheus / Grafana | OCI Logging / Monitoring |
| OpenTelemetry | OCI APM |
| Registry local | OCI Registry (OCIR) |
| CI/CD local | OCI DevOps |

Cada componente documenta responsabilidad, límites y estrategia de migración sin reescritura del dominio.
