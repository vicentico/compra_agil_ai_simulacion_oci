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
~~Elección Tempo vs Jaeger en FASE 2~~ — resuelta, ver Amendment. SLOs tras baseline FASE 18 (pendiente).

## Amendment (2026-08-16) — Backend de traces: Grafana Tempo

**Decisión:** Grafana Tempo (no Jaeger).

**Razones:**
1. **Un solo vendor para todo el frontal de observabilidad** (Prometheus + Loki + Tempo + Grafana, el llamado "stack LGTM"): la correlación trace↔logs↔métricas se provisiona de forma nativa en Grafana (`tracesToLogsV2`, `derivedFields`, `serviceMap`) sin adaptadores adicionales — ver `infrastructure/docker/config/grafana/provisioning/datasources/datasources.yaml`.
2. **Ingesta OTLP nativa** y **TraceQL** como lenguaje de consulta, consistente con el resto del stack (PromQL/LogQL).
3. **Almacenamiento simple para el POC**: backend `local` en disco, sin depender de Cassandra/Elasticsearch (que sí requieren varios backends de producción de Jaeger).

**Jaeger rechazado por:** duplicar superficie de UI (Grafana ya es "frontal único" por decisión original de este ADR); su integración con Grafana es funcional pero menos profunda que la de Tempo (mismo vendor).

**Consecuencia:** `infrastructure/docker/config/tempo/tempo.yaml`, contenedor `tempo` en el perfil `obs` de `docker-compose.yml`. Sin impacto en el dominio ni en la instrumentación de los servicios (ADR-011 original: solo cambia el exporter OTLP de destino, gestionado por el Collector).
