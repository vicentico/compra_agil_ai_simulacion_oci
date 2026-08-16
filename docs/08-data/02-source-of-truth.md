# 02 — Source of Truth

| Dato | System of Record | Clase | Regla |
|---|---|---|---|
| Información pública de contratación | **ChileCompra** | Source externo | Nuestra copia jamás "corrige" al origen; discrepancia = re-sync |
| Raw payloads | **MongoDB (raw_payloads)** | Source local inmutable | Nunca se edita ni borra; base de todo reproceso (NFR-017) |
| CompraAgil normalizada | MongoDB | Derived | 100% derivable del raw; bug de normalización → re-derivar, no editar a mano |
| Binarios de documentos | **MinIO original/** | Source local (copia) | Hash SHA-256 verifica integridad; versiones append-only |
| Texto extraído / OCR / páginas | MinIO + MongoDB | Derived | Regenerable desde binario |
| Chunks | MongoDB | Derived | Regenerable desde texto unificado |
| Vectores | Qdrant | Indexed | **Nunca fuente primaria**; reconstruible; reconciliación por hash |
| Cache | Redis | Cached | Descartable siempre |
| AIAnalysis / Requirements | MongoDB | AI Generated | Derivado no determinístico → se versiona con prompt/modelo exactos; nunca autoridad sobre el documento fuente |
| CompanyProfile | **MongoDB (proposals)** | Source local | Única fuente de capacidades de la empresa |
| Proposal | **MongoDB (proposals)** | Source local (artefacto humano+IA) | Append-only; no regenerable |
| AuditEvent | **MongoDB audit (→PostgreSQL)** | Source local | Inmutable |

Principio operativo: ante cualquier duda, se reconstruye del nivel superior de la cadena. Un embedding jamás responde por sí mismo: siempre existe el chunk, la página, el documento y el raw detrás.
