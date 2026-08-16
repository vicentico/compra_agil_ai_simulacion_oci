# ADR-011 — Observabilidad: OpenTelemetry + Prometheus/Grafana/Loki

**Estado:** Accepted · **Fecha:** 2026-08-16

## Context
NFR-003 (trazabilidad distribuida), NFR-005 (medir antes de optimizar); OCI destino: APM/Monitoring/Logging.

## Decision
Instrumentación **OpenTelemetry en los cuatro servicios .NET** (traces, métricas, logs) → OTel Collector → Prometheus (métricas), Loki (logs JSON estructurados), backend de traces (Tempo o Jaeger, elegir en FASE 2), Grafana como frontal único. Propagación W3C traceparent + `correlationId` de negocio también en headers de RabbitMQ.

## Rationale
OTel es el único estándar que hace la migración a OCI APM un cambio de exporter, no de instrumentación. Collector como punto único desacopla servicios de backends. correlationId de negocio complementa traceId: sobrevive a re-encolados y reintentos que rompen la cadena de spans.

## Consequences
- (+) Migración = cambiar exporters; dashboards sobre nombres de métricas estables.
- (−) 4 contenedores extra de observabilidad (aceptado: es parte del objetivo demostrativo; perfil compose `obs`).

## Rejected Alternatives
Serilog+archivos (sin correlación distribuida); ELK (más pesado que Loki para POC); APM comercial (costo, no local).

## Future Reconsideration
Elección Tempo vs Jaeger en FASE 2; SLOs tras baseline FASE 18.
