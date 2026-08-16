# ADR-012 — Criterios de extracción de microservicios

**Estado:** Accepted · **Fecha:** 2026-08-16

## Context
ADR-001 fija modular monolith + workers. MP2 §38-39 exige criterios explícitos para separar y diseño strangler-ready.

## Decision
Un módulo se extrae a servicio físico solo si cumple **al menos dos** criterios con evidencia medida: despliegue independiente necesario (frecuencia de cambio conflictiva), escalado independiente (métricas de saturación), ownership de datos que exige aislamiento, requerimiento de resiliencia/seguridad diferenciado, o carga que degrada al resto. La extracción sigue strangler: (1) el módulo ya solo se comunica por eventos/contratos, (2) se mueve el módulo a un host propio reutilizando dominio y aplicación intactos, (3) Traefik re-enruta, (4) se retira del monolith.

Candidatos previsibles (no compromisos): Knowledge/RAG (perfil de recursos), Document Intelligence (CPU-bound), Compliance (aislamiento normativo).

## Rationale
La cantidad de contenedores no demuestra madurez; la capacidad de separar sin reescribir sí. Los prerequisitos (outbox, contratos, architecture tests, dominio limpio) se construyen desde FASE 4, haciendo la extracción una decisión reversible y barata.

## Consequences
- (+) Evolución guiada por métricas, no por moda.
- (−) Exige disciplina continua de límites (architecture tests en CI).

## Rejected Alternatives
Separación preventiva; monolito sin límites internos.

## Future Reconsideration
Revisión formal del boundary map al cierre de cada fase mayor (FASE 10, 15, 18).
