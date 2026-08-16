#!/usr/bin/env python3
"""
Placeholder del job de seeding (FR-052 — datos ficticios: Compra Ágil,
documentos, empresa, requisitos, embeddings mock, propuesta, eventos,
auditoría, todos marcados isDemoData=true).

NO implementado todavía: el dominio del que depende (CompraAgil, Document,
CompanyProfile, Proposal...) se construye a partir de FASE 4
(docs/ROADMAP.md). Este script existe para que el perfil `demo` de
docker-compose.yml sea válido desde FASE 1, y falla explícitamente en vez
de simular un éxito falso.
"""
import sys

MENSAJE = (
    "[seed] Pendiente de implementacion. El seeding real requiere el dominio "
    "Procurement/Document/Proposal (FASE 4+). Ver docs/16-operations/01-operations.md "
    "y docs/ROADMAP.md. Este placeholder existe solo para que `docker compose "
    "--profile demo` tenga un contrato valido desde FASE 1."
)

if __name__ == "__main__":
    print(MENSAJE, file=sys.stderr)
    sys.exit(1)
