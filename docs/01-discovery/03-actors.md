# 03 — Actores

| ID | Actor | Tipo | Descripción / responsabilidad |
|---|---|---|---|
| ACT-01 | Usuario (analista/comercial) | Human Actor | Explora compras, revisa análisis y evidencia, edita y aprueba propuestas. Decisor final (human-in-the-loop). |
| ACT-02 | Administrador | Human Actor | Configura sistema, credenciales, plantillas, perfil de empresa, usuarios/roles. |
| ACT-03 | Sistema ChileCompra | External System | API pública Compra Ágil v2; source of truth de información de contratación. No controlado por nosotros. |
| ACT-04 | Sync Worker | Worker | Sincronización incremental; detecta nuevos/modificados; publica eventos; mantiene checkpoint. |
| ACT-05 | Document Worker | Worker | Ejecuta pipeline documental etapa por etapa, reintentable e idempotente. |
| ACT-06 | OCR Service | Internal Service | Abstracción IOcrService (Local/Cloud/Mock). Convierte imágenes/escaneos en texto con confianza. |
| ACT-07 | AI Service | Internal Service / AI Agent | Ejecuta análisis, extracción de requisitos y generación de secciones vía proveedor LLM abstraído. |
| ACT-08 | RAG Service | Internal Service | Retrieval + ensamblado de contexto + respuesta con evidencia. |
| ACT-09 | Proposal Service | Internal Service | Genera y versiona propuestas desde plantilla + perfil + requisitos + RAG. |
| ACT-10 | Compliance Engine | Internal Service | Evalúa cumplimiento; reglas determinísticas primero, LLM como apoyo, nunca autoridad única. |
| ACT-11 | Keycloak | External System (local) | Identidad, autenticación, RBAC. |
| ACT-12 | SuperAdmin | Human Actor | Rol por sobre admin; único autorizado a gestionar cuotas/throttling hacia la API externa (FR-066). |
| ACT-13 | Monitor/Notification Worker | Worker | Detecta oportunidades de alto potencial tras cada sync y despacha notificaciones curadas (in-app + email digest). |

Distinciones: los **workers** reaccionan a eventos/agenda y no exponen API de usuario; los **internal services** son módulos invocables; los **AI agents** producen contenido que siempre pasa por validación de esquema y revisión humana.
