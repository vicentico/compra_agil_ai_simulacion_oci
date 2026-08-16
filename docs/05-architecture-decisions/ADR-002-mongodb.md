# ADR-002 — MongoDB operacional; PostgreSQL diferido

**Estado:** Accepted · **Fecha:** 2026-08-16

## Context
Datos dominantes: payloads JSON de API externa, documentos con estructura variable, versiones, chunks, análisis IA anidados. También se exige auditoría y reporting (MP §7, §22) y OCI mapea MongoDB → Autonomous JSON Database.

## Problem
¿Qué base para el modelo operacional y dónde vive la auditoría/reporting?

## Options
1. Todo PostgreSQL (JSONB).
2. Todo MongoDB.
3. **MongoDB operacional + raw payloads; PostgreSQL se incorpora cuando llegue reporting/auditoría relacional (FASE 15), iniciando la auditoría en MongoDB append-only.**

## Decision
Opción 3.

## Rationale
El modelo es documental por naturaleza (raw payload inmutable, versiones, chunks anidados); MongoDB elimina impedance mismatch y mapea directo a OCI Autonomous JSON. PostgreSQL aporta valor en consultas relacionales de auditoría/reporting, que no existen hasta fases tardías: introducirlo el día uno es un contenedor más que mantener sin usuarios. La colección AuditEvent en MongoDB es append-only y migrable por ETL cuando PostgreSQL entre.

## Consequences
- (+) Un solo motor al inicio; esquema flexible ante cambios de la API externa.
- (−) Sin joins relacionales: agregaciones de reporting más laboriosas hasta FASE 15.
- (−) Doble persistencia eventual (audit Mongo→PG) exige ETL idempotente.

## Rejected Alternatives
Todo PostgreSQL: JSONB viable pero pierde el mapeo natural a OCI JSON DB y complica versionado documental. Todo MongoDB para siempre: reporting relacional y retención de auditoría se benefician de SQL.

## Future Reconsideration
OQ-04; decidir formato definitivo de auditoría al iniciar FASE 15.
