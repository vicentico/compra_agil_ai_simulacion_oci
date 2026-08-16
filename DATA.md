# DATA.md — Síntesis

Detalle en [docs/08-data/](docs/08-data/).

| Dato | Source of Truth |
|---|---|
| Información pública de contratación | ChileCompra (externo) |
| Copia operacional normalizada + raw payloads | MongoDB |
| Documentos binarios | MinIO |
| Índice vectorial | Qdrant (derivado, reconstruible) |
| Auditoría / reporting relacional | PostgreSQL |
| Cache / locks / dedupe | Redis (efímero) |
| Análisis IA | Derivado, versionado, nunca fuente primaria |

Nunca se pierde el payload original (`RawCompraAgilPayload`). Todo dato derivado es regenerable desde su fuente y trazable (lineage completo en [docs/08-data/03-data-lineage.md](docs/08-data/03-data-lineage.md)).
