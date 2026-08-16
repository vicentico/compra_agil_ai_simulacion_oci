# Architecture Review Package — Gate FASE 0 → FASE 1

Según MASTER PROMPT 2 §37: la implementación solo comienza tras verificar que estas piezas son coherentes entre sí.

## Checklist de contenido

| Pieza | Documento | Estado |
|---|---|---|
| System Context | [04-architecture/01-context-diagram.md](04-architecture/01-context-diagram.md) | ✅ |
| Container Diagram | [04-architecture/02-container-diagram.md](04-architecture/02-container-diagram.md) | ✅ |
| Component Diagram | [04-architecture/03-component-diagram.md](04-architecture/03-component-diagram.md) | ✅ |
| Deployment Diagram | [04-architecture/04-deployment-diagram.md](04-architecture/04-deployment-diagram.md) | ✅ |
| Data Model | [03-domain/02-domain-model.md](03-domain/02-domain-model.md) + [08-data/01](08-data/01-data-architecture.md) | ✅ |
| Event Model | [07-events/00-event-conventions.md](07-events/00-event-conventions.md) + [04-architecture/06](04-architecture/06-event-flow.md) | ✅ |
| API Model | [06-api/00-api-conventions.md](06-api/00-api-conventions.md) | ✅ |
| Security Model | [12-security/](12-security/01-security-controls.md) + [threat model](12-security/02-threat-model.md) | ✅ |
| RAG Architecture | [10-rag/01](10-rag/01-rag-specification.md) + [04-architecture/10](04-architecture/10-rag-architecture.md) | ✅ |
| AI Architecture | [11-ai/01](11-ai/01-ai-governance.md) + [04-architecture/09](04-architecture/09-ai-architecture.md) | ✅ |
| OCI Mapping | [04-architecture/11-oci-mapping.md](04-architecture/11-oci-mapping.md) + [17-oci-migration/01](17-oci-migration/01-migration-strategy.md) | ✅ |
| Risk Register | [01-discovery/08-risks.md](01-discovery/08-risks.md) | ✅ |
| ADRs | [05-architecture-decisions/](05-architecture-decisions/README.md) (12) | ✅ |
| Traceability | [18-traceability/requirements-traceability.md](18-traceability/requirements-traceability.md) | ✅ |

## Verificaciones de coherencia realizadas

1. **Requisitos ↔ casos de uso ↔ componentes**: todo FR mapea a un UC y a un componente del container/component diagram (matriz de trazabilidad completa; sin requisitos huérfanos ni componentes sin requisito).
2. **Eventos ↔ flujos**: el catálogo de eventos (07) coincide con el event flow (04/06), los casos de uso y los productores/consumidores del component diagram.
3. **APIs ↔ bounded contexts**: cada grupo de endpoints pertenece a exactamente un contexto; sin endpoints que crucen contextos.
4. **Datos ↔ ownership**: cada colección/bucket/colección vectorial tiene un contexto owner único (08-data); sources of truth sin conflicto.
5. **ADRs ↔ arquitectura**: cada tecnología del container diagram tiene ADR; ningún ADR contradice otro (verificado: ADR-002 difiere PostgreSQL y ADR-005 evita pgvector coherentemente).
6. **Seguridad ↔ amenazas**: cada amenaza del threat model tiene mitigación asignada a una fase del roadmap.
7. **Fallos ↔ operación**: los 16 escenarios de fallo tienen detección/retry/fallback/recuperación/UX/auditoría definidos, coherentes con la topología de colas (retry/DLQ).

## Riesgos aceptados en este gate

- OQ-01/OQ-02 (contrato exacto API ChileCompra y descarga de adjuntos) permanecen abiertos por diseño: se cierran con el spike de FASE 5 contra la API real; el anti-corruption layer acota el impacto.
- OQ-03 (modelo de embeddings) se decide en FASE 9; el diseño de Qdrant ya contempla recreación de colección.
- ASM-08 (términos de uso) debe validarse antes de sincronizar datos reales.

## Decisión

**Estado: LISTO PARA REVISIÓN HUMANA.** Con la aprobación del revisor (usuario), se habilita FASE 1. Cualquier observación se incorpora vía cambio de docs + ADR si altera una decisión.
