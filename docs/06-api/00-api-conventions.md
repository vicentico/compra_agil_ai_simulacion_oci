# 00 — Convenciones de API

Toda API se especifica en OpenAPI **antes** de implementarse (spec por módulo en esta carpeta, generadas en la fase correspondiente; este documento fija las convenciones transversales).

## Generales

- REST/JSON sobre HTTPS vía gateway; prefijo `/api`; recursos en kebab-case; inglés en payloads, español permitido en contenido de negocio.
- **Auth**: Bearer JWT (Keycloak); 401 sin token válido, 403 sin rol. Roles mínimos por endpoint documentados en cada spec.
- **Errores**: RFC 7807 `application/problem+json` con `type`, `title`, `status`, `detail`, `instance`, `correlationId` y `errors[]` de validación.
- **Correlación**: se acepta `X-Correlation-Id` entrante (se genera si falta) y siempre se devuelve en la respuesta; propagado a eventos y logs.
- **Paginación**: `page` (1-based) + `pageSize` (default 20, máx 100); respuesta `{ items, page, pageSize, totalItems, totalPages }`.
- **Filtering/sorting**: query params tipados por recurso (`estado`, `organismo`, `fechaCierreDesde/Hasta`, `montoMin/Max`); `sort=campo:asc|desc`.
- **Idempotencia**: mutaciones no idempotentes por naturaleza aceptan header `Idempotency-Key`; repetición devuelve el resultado original con `Idempotency-Replayed: true`.
- **Versionado**: por ruta si se vuelve necesario (`/api/v2/...`); en POC, contratos aditivos.
- **Status codes**: 200/201/202 (202 para operaciones asíncronas con `Location` de seguimiento), 204, 400 validación, 404, 409 conflicto de versión/concurrencia, 422 regla de negocio, 429, 500.

## Catálogo de endpoints por bounded context

### Sync (Procurement)
| Método | Ruta | Descripción | Rol |
|---|---|---|---|
| POST | /api/sync/compra-agil | Dispara ciclo de sync (202 + syncExecutionId) | admin |
| GET | /api/sync/status | Checkpoint + última ejecución | analyst |
| GET | /api/sync/executions | Historial paginado | analyst |

### Compra Ágil (Procurement)
| GET | /api/compra-agil | Listado paginado/filtrado | viewer |
| GET | /api/compra-agil/{id} | Detalle + estado pipeline | viewer |
| GET | /api/compra-agil/{id}/documents | Documentos y versiones | viewer |
| GET | /api/compra-agil/{id}/analysis | Análisis IA vigente (+`?version=`) | viewer |
| GET | /api/compra-agil/{id}/requirements | Requisitos + evidencia | viewer |
| GET | /api/compra-agil/{id}/trace | Cadena de trazabilidad | analyst |
| GET | /api/dashboard/metrics | Agregados del dashboard | viewer |
| GET | /api/opportunities | Compras coincidentes con rubros confirmados, ordenadas por score de ganabilidad (descomposición incluida) | viewer |
| GET | /api/effectiveness/metrics | Telemetría de negocio: win-rate, montos, costo IA por propuesta | viewer |
| GET | /api/notifications | Centro de notificaciones del usuario (paginado, estado leído) | viewer |
| PUT | /api/notifications/preferences | Preferencias: umbral, frecuencia digest, canales, silenciados | viewer |

### Documents
| GET | /api/documents/{id} | Metadata + estado etapas | viewer |
| GET | /api/documents/{id}/content | URL firmada / streaming del binario | viewer |
| GET | /api/documents/{id}/pages/{n} | Texto + método + confianza de página | viewer |
| POST | /api/documents/{id}/reprocess | Reintenta etapa fallida (202) | analyst |
| GET | /api/documents/reviews | Tareas de revisión de extracción pendientes | analyst |
| POST | /api/documents/{id}/review | Resuelve revisión: texto validado/corregido o carga manual (multipart) | analyst |

### Analysis / RAG (Knowledge)
| POST | /api/analysis/{compraId}/run | (Re)ejecuta análisis (202, Idempotency-Key) | analyst |
| POST | /api/rag/{compraId}/query | Pregunta RAG → respuesta + evidence[] | viewer |

### Proposals
| POST | /api/proposals | Crea propuesta (compraId, templateId) | editor |
| POST | /api/proposals/{id}/generate | Genera secciones (202, Idempotency-Key) | editor |
| GET | /api/proposals/{id} | Versión vigente con secciones | viewer |
| GET | /api/proposals/{id}/versions | Historial de versiones | viewer |
| PUT | /api/proposals/{id}/sections/{sectionId} | Edita sección (If-Match versión → 409 si conflicto) | editor |
| POST | /api/proposals/{id}/sections/{sectionId}/regenerate | Sugerencia IA (202) | editor |
| POST | /api/proposals/{id}/compliance | Ejecuta compliance (202) | editor |
| POST | /api/proposals/{id}/outcome | Registra/corrige el resultado de la propuesta (versionado) | editor |
| GET | /api/proposals/{id}/compliance | Matriz vigente (+historial) | viewer |
| GET | /api/proposals/{id}/export?format=docx | Exporta la versión vigente a .docx editable | viewer |

### Administración (SuperAdmin)
| Método | Ruta | Descripción | Rol |
|---|---|---|---|
| GET | /api/admin/rate-limits | Configuración de cuotas vigente + métricas en vivo (uso, 429, circuit breaker) | superadmin |
| PUT | /api/admin/rate-limits | Modifica cuotas en caliente (motivo obligatorio; validación de rangos; auditado) | superadmin |

### Company Profile / Audit
| GET/PUT | /api/company-profile | Perfil de empresa | admin (PUT), viewer (GET) |
| POST | /api/company-profile/infer-rubros | Infiere rubros por LLM desde la descripción del negocio | editor |
| PUT | /api/company-profile/rubros | Confirma/edita/descarta rubros (auditoría humana) | editor |
| GET | /api/audit | Búsqueda por entityId/correlationId/actor/fechas | analyst |

Ejemplo de contrato detallado: [01-example-rag-query.md](01-example-rag-query.md).
