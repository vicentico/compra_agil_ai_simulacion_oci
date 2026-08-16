# 09 — Preguntas abiertas

| ID | Pregunta | Bloquea | Resolver antes de | Estado |
|---|---|---|---|---|
| OQ-01 | ¿Formato exacto de respuesta y paginación de la API Compra Ágil v2 (nombres de campos, ventanas de cambio)? | Diseño fino del connector | FASE 5 | Abierta — requiere spike con API key real |
| OQ-02 | ¿Los adjuntos requieren autenticación para descarga? ¿URLs firmadas/temporales? | Pipeline de descarga | FASE 7 | Abierta |
| OQ-03 | ¿Qué modelo de embeddings usar por defecto (local vs API) y con qué dimensión? | Colección Qdrant (dimensión fija) | FASE 9 | Abierta — candidatos: nomic-embed-text (Ollama), text-embedding-3-small |
| OQ-04 | ¿PostgreSQL entra desde FASE 1 o la auditoría inicia en MongoDB y migra después? | Data architecture | FASE 4 | Propuesta: iniciar en MongoDB, PostgreSQL al llegar reporting (ADR-002) |
| OQ-05 | ¿Plantilla de propuesta única o múltiples plantillas por rubro? | Proposal Generator | FASE 13 | Propuesta: una plantilla parametrizable en POC |
| OQ-06 | ¿Términos de uso de Mercado Público permiten almacenamiento y procesamiento IA de documentos? | Legal | FASE 5 | Abierta (ASM-08) |
| OQ-07 | ¿Qué gateway concreto: Traefik, Kong o YARP? | Infraestructura | FASE 1 | Propuesta: Traefik (ADR-009) |
| OQ-08 | ¿Idioma de la UI solo español o i18n desde el inicio? | Frontend | FASE 16 | Propuesta: español, estructura i18n-ready |

Regla: ninguna pregunta se cierra silenciosamente; su resolución se registra aquí y, si cambia una decisión, en un ADR.
