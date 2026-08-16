# 05 — Requisitos funcionales

Formato: cada FR tiene ID, descripción, prioridad (M/S/C), origen, criterio de aceptación (CA), dependencias y estado. Estado en FASE 0: `Specified` (ninguno implementado).

## Sincronización (Procurement)

| ID | Descripción | Pri | Origen | Criterio de aceptación | Dep |
|---|---|---|---|---|---|
| FR-001 | Sincronizar Compras Ágiles desde la API ChileCompra v2 de forma periódica y bajo demanda | M | Master Prompt §1.1, §6 | Un ciclo de sync consulta la API paginada, procesa todas las páginas y registra SyncExecution con métricas | — |
| FR-002 | Detectar procesos nuevos y publicarlos como `CompraAgilDetected` | M | §1.2 | Compra inexistente localmente crea documento normalizado + raw + evento | FR-001 |
| FR-003 | Detectar modificaciones de procesos existentes y publicarlas como `CompraAgilUpdated` | M | §1.3 | Cambio de hash del payload genera nueva versión y evento; sin cambio no genera escritura | FR-001 |
| FR-004 | Mantener copia local normalizada y auditable derivada del raw payload | M | §1.4, §8 | Todo campo normalizado es derivable del raw almacenado; raw nunca se elimina | FR-001 |
| FR-005 | Mantener SyncCheckpoint (source, lastSuccessfulSync, ventana de cambios, contadores, errores, duración, correlationId) y sincronización incremental vía ttl_cambio_ms / cambio_desde / cambio_hasta | M | §6 | Reinicio del worker retoma desde checkpoint sin duplicar ni perder registros | FR-001 |

## Consulta (Procurement)

| ID | Descripción | Pri | Origen | CA | Dep |
|---|---|---|---|---|---|
| FR-006 | Listar Compras Ágiles con búsqueda, filtros, paginación y ordenamiento | M | §1.19, §30 | API y UI devuelven resultados paginados filtrables por estado, fecha, organismo, monto | FR-004 |
| FR-007 | Consultar detalle de una Compra Ágil con documentos, análisis y estado de pipeline | M | §1.19 | GET /api/compra-agil/{id} entrega detalle + referencias | FR-004 |
| FR-008 | Exponer dashboard con métricas operacionales (nuevas, abiertas, por cerrar, modificadas, analizadas, propuestas, compliance promedio) | M | §31 | Dashboard Angular consume endpoints agregados | FR-006 |

## Document Intelligence

| ID | Descripción | Pri | Origen | CA | Dep |
|---|---|---|---|---|---|
| FR-010 | Descargar documentos adjuntos de cada compra con validación de tipo, tamaño y URL (allowlist) | M | §1.5, §24 | Documento descargado, validado y con estado; URL fuera de allowlist se rechaza y audita | FR-002 |
| FR-011 | Almacenar documentos originales en MinIO con hash SHA-256 y estructura por compra | M | §1.6, §10 | Objeto en `chilecompra/{codigo}/original/`; hash persistido; MongoDB guarda solo metadata + referencia | FR-010 |
| FR-012 | Clasificar PDFs: textual, escaneado, mixto, con tablas, con imágenes, complejo | M | §11 | Clasificación persistida por documento con método y confianza | FR-011 |
| FR-013 | Extraer texto de PDFs textuales conservando página y sección | M | §1.7, §11 | Texto por página con metadata de extracción | FR-012 |
| FR-014 | Detectar necesidad de OCR (densidad de texto) y aplicar OCR mediante IOcrService | M | §1.8-1.9, §12 | PDF escaneado produce texto OCR con confianza por página; proveedor intercambiable sin tocar dominio | FR-012 |
| FR-015 | Extraer y analizar tablas e imágenes cuando existan | S | §1.10, §11 | Tablas representadas estructuradamente; imágenes almacenadas en `/images/` | FR-013 |
| FR-016 | Fragmentar documentos en chunks semánticos (títulos, secciones, párrafos, tablas, requisitos, listas, anexos) con metadata completa | M | §1.11, §13 | DocumentChunk con compraAgilId, documentId, página, sección, chunkType, hash, tokenCount | FR-013, FR-014 |
| FR-017 | Generar embeddings por chunk mediante proveedor abstraído | M | §1.12 | Embedding persistido/indexado con referencia al chunk y versión de modelo | FR-016 |
| FR-018 | Indexar chunks en Qdrant con metadata suficiente para filtering (compraAgilId, documentId, versión, página, sección, chunkType, source) | M | §1.13, §14 | Búsqueda vectorial filtrada por compraAgilId retorna solo chunks de esa compra | FR-017 |

## RAG (Knowledge)

| ID | Descripción | Pri | Origen | CA | Dep |
|---|---|---|---|---|---|
| FR-020 | Responder preguntas sobre una Compra Ágil mediante RAG restringido a esa compra | M | §1.14, §14-15 | Pipeline query→retrieval→LLM responde solo con contexto de la compra activa | FR-018 |
| FR-021 | Toda respuesta RAG incluye evidencia: documentId, página, chunkId, texto fuente, confianza | M | §15 | Respuesta sin evidencia documental se marca UNKNOWN, nunca se presenta como hecho | FR-020 |
| FR-022 | Soportar expansión de query, filtrado por metadata y reranking | S | §15 | Etapas configurables y medibles individualmente | FR-020 |
| FR-023 | Permitir al usuario navegar de la evidencia al documento/página original | M | §1.24, §21 | Click en evidencia abre visor en la página citada | FR-021 |

## Análisis IA

| ID | Descripción | Pri | Origen | CA | Dep |
|---|---|---|---|---|---|
| FR-024 | Generar análisis estructurado por compra (resumen, objetivo, productos, cantidades, requisitos técnicos/comerciales, condiciones, garantías, documentos exigidos, criterios, presupuesto, plazos, riesgos, preguntas, opportunityScore, complianceComplexity, recomendación) | M | §1.15, §16 | AnalysisResult validado contra JSON Schema y persistido con versión de prompt/modelo | FR-018 |
| FR-025 | Separar cada afirmación en FACT / INFERENCE / RECOMMENDATION / UNKNOWN | M | §16, §36 | Campo obligatorio en el schema; dato ausente → «Información no encontrada en las fuentes analizadas» | FR-024 |
| FR-026 | Extraer requisitos estructurados (Requirement) con tipo, obligatoriedad, categoría, documento fuente, página, evidencia y confianza | M | §1.16-1.17, §17 | Cada requisito traza a evidencia documental verificable | FR-024 |
| FR-027 | Construir matriz de cumplimiento a partir de los requisitos | M | §1.18 | Matriz consultable por API y visible en UI | FR-026 |

## Propuestas

| ID | Descripción | Pri | Origen | CA | Dep |
|---|---|---|---|---|---|
| FR-030 | Mantener CompanyProfile con información real de la empresa (razón social, RUT, productos, certificaciones, experiencia, cobertura, garantías, contactos, políticas) | M | §19 | CRUD auditado; la IA nunca inventa capacidades no presentes en el perfil | — |
| FR-031 | Generar propuesta desde plantilla + CompraAgil + Requirements + CompanyProfile + RAGContext | M | §1.21, §20 | ProposalDraft con todas las secciones definidas en §20 | FR-026, FR-030 |
| FR-032 | Propuesta editable por sección con contenido manual y sugerencias IA | M | §1.22, §21 | Edición persiste nueva versión de sección | FR-031 |
| FR-033 | Regenerar secciones individuales mediante IA mostrando evidencia | M | §1.23-1.24 | Regeneración crea versión nueva; evidencia visible; aceptar/rechazar sugerencia | FR-032 |
| FR-034 | Versionar propuestas: nunca sobrescribir; comparar y restaurar versiones | M | §1.26, §21 | Historial completo navegable; restauración crea versión nueva | FR-032 |
| FR-035 | Bloqueo optimista/pesimista para edición concurrente humano-IA | M | §28 | Editar mientras la IA genera no corrompe ni pierde versiones | FR-032 |

## Compliance

| ID | Descripción | Pri | Origen | CA | Dep |
|---|---|---|---|---|---|
| FR-036 | Evaluar automáticamente la propuesta contra los requisitos (PASS/PARTIAL/FAIL/UNKNOWN por requisito, con explicación, evidencia y confianza) | M | §1.25, §18 | ComplianceEvaluation persistida y re-ejecutable | FR-026, FR-031 |
| FR-037 | Reglas determinísticas se ejecutan antes que el LLM; el LLM nunca es autoridad única | M | §18 | Resultado indica qué motor lo produjo | FR-036 |
| FR-038 | Re-ejecutar compliance tras cada edición relevante de la propuesta | M | §21 | Nueva versión de propuesta permite nueva evaluación versionada | FR-036 |

## Trazabilidad y auditoría

| ID | Descripción | Pri | Origen | CA | Dep |
|---|---|---|---|---|---|
| FR-040 | Registrar AuditEvent por toda operación relevante (actor, servicio, operación, entidad, versiones, correlationId, causationId, hashes, modelo, promptVersion) | M | §22 | Toda mutación de entidades núcleo genera AuditEvent | — |
| FR-041 | Navegar la cadena completa: CompraAgil → API → raw → documento → OCR → chunk → embedding → ejecución IA → requisito → propuesta → compliance → versión final | M | §1.20, §22 | UI de trazabilidad muestra la cadena para cualquier entidad | FR-040 |
| FR-042 | Versionar documentos, análisis, prompts, modelos y propuestas | M | §1.26, §23 | Toda ejecución IA referencia promptVersion y modelVersion exactos | FR-040 |

## Operación y demo

| ID | Descripción | Pri | Origen | CA | Dep |
|---|---|---|---|---|---|
| FR-050 | Arranque completo con `docker compose up -d` + health checks | M | MP2 §41, §27-28 | Todos los servicios healthy; endpoints /health /ready /live | — |
| FR-051 | Demo Mode: cargar datos de ejemplo y ejecutar pipeline completo sin API externa | M | MP2 §42 | Flujo completo demostrable offline | FR-050 |
| FR-052 | Seed data: compra ficticia, documentos, empresa, requisitos, embeddings mock, propuesta, eventos, auditoría — claramente marcados como ficticios | M | MP2 §43 | `scripts/seed` puebla el sistema; flag `isDemoData` en toda entidad sembrada | FR-051 |

## Onboarding, matching y respaldo humano (Propuesta de Plataforma, 2026-08-16)

| ID | Descripción | Pri | Origen | CA | Dep |
|---|---|---|---|---|---|
| FR-053 | Detectar extracción deficiente (confianza OCR/densidad de texto bajo umbral, parseo parcial) y crear una tarea de revisión humana en estado `pending_review` sin bloquear el resto del pipeline | S | Propuesta de Plataforma §1 | Documento bajo umbral genera tarea visible en UI y evento `DocumentReviewRequested.v1`; los demás documentos continúan | FR-013, FR-014 |
| FR-054 | Permitir al usuario resolver la revisión: validar/corregir el texto extraído o cargar el documento manualmente; el pipeline se reanuda desde chunking con el contenido validado | S | Propuesta §1 | Resolución publica `DocumentReviewCompleted.v1`; carga manual crea DocumentVersion con source=manual_upload; todo auditado con actor humano | FR-053 |
| FR-055 | Inferir rubros del negocio mediante LLM a partir de la descripción en lenguaje natural del usuario, con structured output validado por JSON Schema y persistencia estructurada en el CompanyProfile (MongoDB) | S | Propuesta §2 | Descripción → lista de rubros {code, name, confidence, source=inferred, promptVersion}; validación de schema previa a persistir (NFR-010) | FR-030 |
| FR-056 | Disponer interfaz de auditoría de rubros: el usuario confirma, edita o descarta cada rubro inferido; solo rubros `confirmed` alimentan el matching | S | Propuesta §2 | Cambios de rubros auditados (AuditEvent); matching ignora rubros no confirmados | FR-055 |
| FR-057 | Presentar dashboard de oportunidades: Compras Ágiles coincidentes con los rubros confirmados del perfil, con datos críticos (organismo, monto, fecha de cierre, estado, score de coincidencia) y acceso directo al detalle | S | Propuesta §3 | `GET /api/opportunities` retorna compras matched ordenables por cierre/score; matching explicable (qué rubro coincidió) | FR-006, FR-056 |
| FR-058 | Exportar la propuesta a documento .docx editable, generable on-demand desde el panel de oportunidades o desde el editor | S | Propuesta §3 | `GET /api/proposals/{id}/export?format=docx` descarga .docx fiel a la versión vigente, con secciones y datos del perfil; objeto almacenado en MinIO `generated/` | FR-031, FR-034 |

## Mejoras evolutivas (Propuesta de Mejoras Evolutivas, 2026-08-16)

| ID | Descripción | Pri | Origen | CA | Dep |
|---|---|---|---|---|---|
| FR-059 | Registrar el resultado de cada propuesta (presentada, adjudicada, no_adjudicada, desierta, descartada) con monto, fecha y notas — registro manual del usuario; verificación automática vía API si el spike de FASE 5 confirma que expone adjudicaciones/órdenes de compra (OQ-10) | S | Mejoras Evolutivas §2 | Outcome versionado + `ProposalOutcomeRecorded.v1` + AuditEvent; editable con historial | FR-031 |
| FR-060 | Dashboard de efectividad: win-rate, montos adjudicados, propuestas por período, tiempo de generación y costo IA por propuesta — telemetría de negocio para evidenciar ROI | S | Mejoras §2 | `GET /api/effectiveness/metrics` alimenta panel Angular con métricas agregadas y por período | FR-059, NFR-016 |
| FR-061 | Calcular un score de ganabilidad heurístico y explicable por oportunidad: reglas ponderadas configurables (calce de rubros, monto vs capacidad declarada, plazo de cierre, señales históricas del organismo/outcome cuando existan); el panel de oportunidades ordena por este score y muestra su descomposición | S | Mejoras §1 | Score reproducible dado el mismo input; descomposición visible por factor; pesos configurables por admin | FR-057, FR-030 |
| FR-062 | Recalibrar automáticamente los pesos del score con los resultados históricos (aprendizaje) | **F** (FUTURE) | Mejoras §1 | Se especificará cuando exista volumen de outcomes reales (mitiga cold start, RSK-14) | FR-059, FR-061 |
| FR-063 | Monitoreo proactivo: tras cada ciclo de sync, detectar Compras Ágiles de alto potencial (score ≥ umbral configurable) y publicar `HighPotentialCompraDetected.v1`; rastrear adjudicaciones/órdenes de compra si la API lo permite (OQ-10) | S | Mejoras §3 | Detección determinística sobre el score FR-061; sin duplicados por compra/versión | FR-001, FR-061 |
| FR-064 | Notificar al usuario de forma curada y resumida: centro de notificaciones in-app + email digest (SMTP configurable; MailHog en local); preferencias por usuario (frecuencia, umbral, silenciar); solo alto potencial — nunca el listado completo | S | Mejoras §3 | Digest agrupa por período; preferencia respetada; `NotificationDispatched.v1` auditado; opt-out disponible | FR-063 |
| FR-065 | Throttling dinámico hacia la API ChileCompra: límites de requests por minuto/hora, concurrencia y ventanas de pausa configurables **en caliente** (persistidos en MongoDB, aplicados vía Redis sin redeploy); complementa el circuit breaker y el respeto de 429/Retry-After (NFR-020) | S | Mejoras §4 | Cambio de límites surte efecto en el siguiente request del Sync Worker sin reinicio; `RateLimitConfigChanged.v1` | FR-001, NFR-020 |
| FR-066 | Rol SuperAdmin con panel exclusivo de gestión de cuotas API; todo cambio registra valores previo/nuevo, actor y motivo en AuditEvent | S | Mejoras §4 | Endpoint/panel inaccesible para admin y roles inferiores (403); cambios auditados | FR-065, NFR-007 |

Trazabilidad completa hacia UC/componentes/tests: [../18-traceability/requirements-traceability.md](../18-traceability/requirements-traceability.md).
