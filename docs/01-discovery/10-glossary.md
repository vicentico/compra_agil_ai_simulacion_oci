# 10 — Glosario

| Término | Definición |
|---|---|
| **Compra Ágil (CA)** | Modalidad de compra pública chilena para adquisiciones de bajo monto (actualmente ≤ 100 UTM) donde organismos publican requerimientos y proveedores cotizan en plazos breves vía Mercado Público. |
| **ChileCompra / Mercado Público** | Institución y plataforma transaccional del Estado de Chile para compras públicas; expone APIs públicas con ticket/API key. |
| **Ticket / API key** | Credencial personal para consumir las APIs de ChileCompra. Nunca se almacena en código fuente. |
| **Raw payload** | Respuesta original e inmutable de la API externa, almacenada con URL, timestamp, status, hash y correlationId. Base de toda derivación. |
| **Modelo normalizado** | Representación interna derivada del raw payload, apta para consulta y procesamiento. |
| **SyncCheckpoint** | Estado persistente de la sincronización incremental (última ventana procesada, contadores, errores). |
| **Pipeline documental** | Cadena de etapas reintentables: descarga → almacenamiento → clasificación → extracción → OCR → chunking → embeddings → indexación → análisis IA. |
| **OCR** | Reconocimiento óptico de caracteres para PDFs escaneados/imágenes; produce texto con nivel de confianza. |
| **Chunk / chunking semántico** | Fragmento de documento delimitado por estructura (sección, párrafo, tabla, requisito) con metadata; unidad de indexación vectorial. |
| **Embedding** | Vector numérico que representa semánticamente un chunk; se indexa en Qdrant. |
| **RAG** | Retrieval-Augmented Generation: responder con un LLM usando exclusivamente contexto recuperado y citando evidencia. |
| **Evidencia** | Referencia verificable que sustenta una afirmación: documentId, página, chunkId, texto fuente, confianza. |
| **FACT / INFERENCE / RECOMMENDATION / UNKNOWN** | Clasificación obligatoria de cada afirmación IA según su respaldo documental. |
| **Requirement** | Requisito estructurado extraído de las bases (tipo, categoría, obligatoriedad, evidencia, confianza). |
| **Matriz de cumplimiento** | Vista requisito × estado de cumplimiento de la propuesta (PASS/PARTIAL/FAIL/UNKNOWN). |
| **Compliance Engine** | Motor de evaluación de cumplimiento; reglas determinísticas primero, LLM solo como apoyo. |
| **CompanyProfile** | Perfil real de la empresa proveedora; única fuente de capacidades declarables en propuestas. |
| **Proposal / ProposalVersion / ProposalSection** | Propuesta comercial/técnica versionada por secciones; append-only. |
| **AuditEvent** | Registro inmutable de una operación con actor, entidad, versiones, hashes, correlación y versión de prompt/modelo. |
| **correlationId / causationId** | Identificadores de correlación de un flujo completo y de causalidad entre eventos. |
| **Idempotencia** | Propiedad de una operación de producir el mismo resultado al ejecutarse múltiples veces. |
| **Bounded Context** | Límite lógico de un submodelo de dominio con lenguaje propio (DDD). |
| **Modular monolith** | Un despliegue con módulos internos estrictamente delimitados; boundary lógico sin boundary físico. |
| **Strangler pattern** | Extracción progresiva de módulos hacia servicios independientes sin reescribir el dominio. |
| **Source of Truth / System of Record** | Sistema autoritativo para un dato; el resto son derivados o caches. |
| **Demo Mode** | Modo de operación con datos ficticios que demuestra el pipeline completo sin API externa. |
| **OCI** | Oracle Cloud Infrastructure; destino conceptual de migración del sistema. |
| **UTM** | Unidad Tributaria Mensual, unidad de cuenta chilena usada para umbrales de compra. |
| **Rubro** | Categoría de negocio/actividad económica del proveedor; inferida por LLM desde la descripción del negocio y confirmada por el usuario; base del matching de oportunidades. |
| **Matching de oportunidades** | Cruce entre Compras Ágiles sincronizadas y los rubros confirmados del CompanyProfile, con score y explicación de coincidencia. |
| **Tarea de revisión (HITL)** | Tarea generada cuando la extracción de un documento es deficiente; el usuario valida el texto o carga el documento manualmente y el pipeline se reanuda. |
| **Win-rate** | Tasa de adjudicación: propuestas adjudicadas / propuestas presentadas en un período. Métrica central del dashboard de efectividad. |
| **Score de ganabilidad** | Puntaje heurístico y explicable (reglas ponderadas configurables) que estima el atractivo de una oportunidad para el perfil; ordena el panel, nunca oculta oportunidades. |
| **Outcome de propuesta** | Resultado registrado de una propuesta: presentada, adjudicada, no adjudicada, desierta o descartada. |
| **Throttling** | Control del ritmo de peticiones salientes hacia la API externa (req/min, req/hora, concurrencia) para no exceder las cuotas del proveedor. |
| **Digest** | Notificación resumida y agrupada por período que destaca solo oportunidades de alto potencial. |
