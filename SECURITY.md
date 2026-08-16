# SECURITY.md — Síntesis

Detalle y threat model en [docs/12-security/](docs/12-security/).

Pilares: Keycloak + JWT + RBAC; secretos fuera del código (Docker secrets → OCI Vault); documentos externos tratados como **untrusted input** (validación de tipo/tamaño, abstracción de malware scanning, jamás ejecutar contenido); protección contra prompt injection (contenido documental = datos, no instrucciones); SSRF controlado por allowlist de dominios de descarga; rate limiting en gateway; auditoría completa de operaciones.
