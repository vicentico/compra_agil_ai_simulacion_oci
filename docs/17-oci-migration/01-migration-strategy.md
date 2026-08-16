# 01 — Estrategia de migración a OCI

Tabla componente→servicio con límites del símil: [../04-architecture/11-oci-mapping.md](../04-architecture/11-oci-mapping.md).

## Principios

1. **El dominio no se toca**: migrar = escribir/activar adaptadores de infraestructura (puertos: IObjectStorage, IEventBus, IVectorIndex, ILlmProvider, IOcrService, IAuditStore) y cambiar configuración.
2. **Por componente, no big-bang**: cada pieza migra independientemente; el sistema tolera operación híbrida (ej. Object Storage en OCI, resto local) gracias a configuración por entorno (Development / Test / Production-like / OCI).
3. **Datos primero regenerables después**: migran los sources (raw, binarios, operacional, audit); los derivados (vectores, extractos) se regeneran en destino — menos datos que mover, integridad por hash.
4. **Observabilidad continua**: exporters duales (local + OCI APM/Monitoring) durante la transición.

## Orden propuesto de migración (FASE 19, simulada/documentada)

1. Registry e imágenes → OCIR (retag+push).
2. Object Storage: MinIO → OCI Object Storage vía S3-compat (endpoint+credenciales); verificación por hash.
3. Datos: MongoDB → Autonomous JSON (driver compatible; validar índices/agregaciones); PostgreSQL → OCI PostgreSQL.
4. Mensajería: RabbitMQ → OCI Queue tras IEventBus (mapeo topología documentado en ADR-003).
5. Identidad: realm Keycloak → OCI Identity Domain (OIDC estándar: cambia issuer, mapping roles→groups).
6. Cómputo: Compose → Container Instances → OKE (manifests/Helm derivados del compose).
7. IA/OCR: providers OCI Generative AI y Document Understanding como nuevos adapters; re-evaluación con /evaluation antes del switch.
8. Vector: decisión en su momento (Qdrant sobre OKE vs servicio gestionado); índice regenerable hace el cambio barato.
9. Edge: Traefik → API Gateway + LB + WAF; rutas como IaC.
10. Secretos: → OCI Vault.

## Entregable de FASE 19

Documento de arquitectura OCI objetivo + IaC de referencia (Terraform esqueleto) + análisis de costos aproximado + gap analysis por componente. La migración física real está fuera del alcance del POC; el criterio de éxito es que un revisor OCI valide que la ruta es realista.
