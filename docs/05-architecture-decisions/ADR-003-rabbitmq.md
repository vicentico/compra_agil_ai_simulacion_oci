# ADR-003 — RabbitMQ como bus de eventos inicial

**Estado:** Accepted · **Fecha:** 2026-08-16

## Context
Pipeline por etapas reintentables, competing consumers, DLQs; volumen esperado bajo (ASM-04). OCI destino: Queue (y Streaming si escala).

## Problem
¿Broker para eventos de dominio: RabbitMQ, Kafka/Redpanda, o cola simple (Redis Streams)?

## Options
1. **RabbitMQ** (topic exchange, DLQ, retry queues, management UI).
2. Kafka/Redpanda (log distribuido, replay, particiones).
3. Redis Streams (mínimo operacional).

## Decision
RabbitMQ, detrás de una abstracción `IEventBus` con outbox pattern.

## Rationale
El patrón dominante es work-queue con routing y reintentos — el punto fuerte de RabbitMQ. Kafka aporta replay y throughput que el POC no necesita, al costo de operación (particiones, offsets, compactación) que violaría MP2 §38. El replay que sí necesitamos (reproceso) se resuelve por diseño: todo derivado es regenerable desde datos persistidos, no desde el log del broker.

## Consequences
- (+) DLQ/retry nativos, UI de gestión, curva corta.
- (−) Sin replay de eventos históricos (mitigado: reproceso desde raw/chunks).
- (−) Orden no garantizado entre colas (mitigado: precondiciones + re-encolado, ver 06-event-flow).

## Rejected Alternatives
Kafka día uno (complejidad sin volumen). Redis Streams (sin DLQ/routing maduros; Redis ya tiene roles de cache/lock — mezcla de responsabilidades).

## Future Reconsideration
Si aparece necesidad real de streaming (auditoría de alto volumen, integraciones), evaluar Redpanda → OCI Streaming; IEventBus limita el blast radius.
