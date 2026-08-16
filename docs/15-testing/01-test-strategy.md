# 01 — Estrategia de testing

Definition of Done exige tests junto con cada feature (MP2 §33-34). Herramientas .NET: xUnit + Testcontainers + WireMock.NET + Verify; Angular: Jest/Karma + Playwright e2e.

| Tipo | Alcance | Herramientas | Cuándo |
|---|---|---|---|
| **Unit** | Dominio (policies, specifications, máquinas de estado, chunking, rule engine de compliance) sin infraestructura | xUnit | Cada feature |
| **Integration** | Repos contra MongoDB/Qdrant/MinIO/RabbitMQ reales en Testcontainers; outbox; consumidores | Testcontainers | Cada feature con infra |
| **Contract** | (a) ChileCompra client contra fixtures grabados + WireMock (401/403/404/429/500/503/Retry-After); (b) eventos productor↔consumidor contra JSON Schemas | WireMock.NET, schemas | F5+, F4+ |
| **Architecture** | Reglas de dependencia: dominio sin infraestructura; módulos sin referencias cruzadas | NetArchTest/ArchUnitNET | Desde F4, en CI siempre |
| **API** | Endpoints con auth real (Keycloak testcontainer), validación, RFC7807, paginación, RBAC 401/403 | WebApplicationFactory | Cada endpoint |
| **Security** | AuthZ matrix, SSRF allowlist, límites de archivo, secret scanning, headers | xUnit + CI tooling | F3+ |
| **Idempotency** | Re-ejecución de sync/etapas/eventos duplicados = mismo estado | Integration harness | Cada operación crítica |
| **Resilience / Failure recovery** | Escenarios F1-F16 de docs/14 con fallos inyectados (Toxiproxy/containers pausados) | Testcontainers + Toxiproxy | F18 principalmente; smoke antes |
| **AI Evaluation** | Dataset dorado: factuality, citation accuracy, requirement extraction accuracy, hallucination rate, compliance accuracy, proposal completeness | /evaluation harness propio | F9-F14, gate de cambios de prompt |
| **RAG Evaluation** | precision@k, recall@k por etapa; adversarial (prompt injection en documentos seed) | /evaluation | F9+ |
| **E2E** | Flujo demo completo: seed → pipeline → análisis → propuesta → compliance → trazabilidad, vía UI | Playwright | F16-17 |
| **Performance** | Baseline de latencias/throughput (recién aquí se fijan objetivos) | k6 | F18 |

Reglas: los tests de IA usan MockLlm/MockOcr determinísticos salvo en evaluation; ningún test depende de la API real de ChileCompra (fixtures grabados y anonimizados); pipeline CI corre unit+architecture+contract en cada push, integration en PR, evaluation al tocar prompts/retrieval.
