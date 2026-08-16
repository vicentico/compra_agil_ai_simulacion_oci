# syntax=docker/dockerfile:1
# Placeholder del perfil `demo` (FR-051/052). El seeding real depende del
# dominio Procurement/Proposal que se implementa desde FASE 4 — ver
# docs/16-operations/01-operations.md. Esta imagen documenta la intención
# y falla de forma clara y explícita si se invoca antes de tiempo.
FROM python:3.12-alpine
WORKDIR /seed
COPY seed.py .
ENTRYPOINT ["python", "seed.py"]
