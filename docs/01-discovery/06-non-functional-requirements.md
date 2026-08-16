# 06 — Requisitos no funcionales

| ID | Categoría | Descripción | Criterio de aceptación |
|---|---|---|---|
| NFR-001 | Data Integrity | La sincronización debe ser idempotente | Re-ejecutar un sync sobre los mismos datos no crea duplicados ni versiones espurias (test de idempotencia) |
| NFR-002 | Reliability | El procesamiento documental debe poder reintentarse por etapa sin duplicar resultados | Reintento de cualquier etapa del pipeline produce el mismo estado final (dedupe por hash/idempotency key) |
| NFR-003 | Observability | Toda ejecución distribuida debe ser trazable mediante correlationId y traceId propagados | Un flujo completo (sync→propuesta) es reconstruible en Grafana/Loki con un solo correlationId |
| NFR-004 | Reliability | Tolerancia a fallos de dependencias externas (ChileCompra caído, 429, LLM caído, etc.) | Escenarios de docs/14-reliability tienen detección, retry, fallback y recuperación definidos y probados |
| NFR-005 | Performance | Medir antes de optimizar: latencias de sync, OCR, embedding, RAG, generación y APIs instrumentadas desde el día uno | Métricas expuestas en Prometheus; sin objetivos numéricos hasta tener baseline (MP2 §44) |
| NFR-006 | Availability | Degradación elegante: si la API externa o el LLM no están disponibles, el resto del sistema sigue operable con datos locales | UI funcional en modo lectura/demo ante caída de dependencias |
| NFR-007 | Security | Autenticación JWT + RBAC vía Keycloak en toda API de usuario | Endpoint sin token válido responde 401; rol insuficiente responde 403 |
| NFR-008 | Security | Secretos fuera del código y del repositorio | Ningún secreto real en Git; escaneo en CI; Docker secrets / .env no versionado |
| NFR-009 | Security | Documentos externos tratados como untrusted input (validación tipo/tamaño, sin ejecución de contenido, abstracción antimalware, defensa prompt injection) | Threat model docs/12-security con mitigaciones implementadas por fase |
| NFR-010 | AI Reliability | Output de LLM siempre validado contra JSON Schema antes de persistir; fallo de validación = reintento/rechazo auditado, nunca persistencia parcial | Tests de contrato de IA |
| NFR-011 | AI Reliability | Toda afirmación IA derivada de documentos incluye evidencia; sin evidencia → UNKNOWN | Evaluación de citation accuracy en /evaluation |
| NFR-012 | Auditability | Toda mutación de entidades núcleo y toda ejecución IA genera AuditEvent inmutable | Cobertura de auditoría verificada por tests de integración |
| NFR-013 | Maintainability | Dominio independiente de infraestructura (sin dependencias a MongoDB/RabbitMQ/HTTP/LLM/Angular/Docker/OCI) | Architecture tests (.NET) validan las dependencias entre capas |
| NFR-014 | Scalability | Workers escalables horizontalmente; consumo de eventos competitivo sin duplicación de efectos | Dos instancias del mismo worker no duplican resultados |
| NFR-015 | Privacy | Datos ficticios de demo claramente separados de datos reales; sin datos personales innecesarios | Flag isDemoData; revisión de campos personales |
| NFR-016 | Cost | Tracking de costo IA (tokens/modelo) por compra, documento, usuario y operación; cache de resultados para evitar regeneraciones | Dashboard de costo IA; cache hit medible |
| NFR-017 | Data Integrity | El raw payload es inmutable y nunca se pierde; todo dato derivado es regenerable | Reproceso completo desde raw produce estado equivalente |
| NFR-018 | Maintainability | Prompts versionados como artefactos (/prompts), nunca embebidos dispersos en código ni modificados silenciosamente | PromptVersion referenciada en cada AIExecution |
| NFR-019 | Reliability | Eventos versionados, idempotentes, serializables, compatibles hacia atrás; consumidores toleran duplicados y desorden | Contract tests de eventos; consumer-driven |
| NFR-020 | Performance | Backpressure y rate limiting hacia ChileCompra (respetar 429/Retry-After) y hacia proveedores LLM | Circuit breaker + límites configurables por entorno |
