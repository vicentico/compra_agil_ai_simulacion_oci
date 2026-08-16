# ADR-001 — Estilo arquitectónico: modular monolith + workers, event-driven

**Estado:** Accepted · **Fecha:** 2026-08-16

## Context
El master prompt exige una plataforma distribuida, orientada a eventos, observable y migrable a OCI, pero también prohíbe la sobreingeniería (MP2 §38) y exige distinguir logical boundary de deployment boundary (MP2 §10). Equipo pequeño, POC demostrable en local.

## Problem
¿Cuántas piezas desplegables y qué estilo: microservicios por bounded context, monolito clásico, o punto intermedio?

## Options
1. Microservicios físicos por bounded context (7+ servicios .NET).
2. Monolito único (API + jobs internos en el mismo proceso).
3. **Modular monolith (Platform API) + workers desacoplados (Sync/Document/AI) comunicados por eventos.**

## Decision
Opción 3. Límites lógicos DDD estrictos dentro de la API (módulos con architecture tests); trabajos pesados y asíncronos en workers independientes; RabbitMQ + outbox como columna vertebral.

## Rationale
Los workers tienen razones reales de separación: ciclo de vida propio, escalado independiente, aislamiento de fallos (OCR/LLM no debe tumbar la API), perfiles de recursos distintos. Los módulos de consulta/gestión no las tienen aún: separarlos multiplicaría contratos, despliegues y complejidad de observabilidad sin beneficio medible. El estilo demuestra madurez arquitectónica (EDA, idempotencia, trazabilidad) sin inflar contenedores.

## Consequences
- (+) Un despliegue simple, demo confiable, refactors de dominio baratos.
- (+) La EDA real ya existe: extraer un módulo después es routing, no reescritura.
- (−) Disciplina requerida para no acoplar módulos (mitigado con architecture tests y outbox).
- (−) Un bug de memoria en un módulo afecta a toda la API (mitigado: trabajos pesados están fuera).

## Rejected Alternatives
Microservicios día uno (costo sin beneficio en POC; RSK-05). Monolito total (mezcla ciclos de vida de sync/OCR/LLM con tráfico de usuario; viola aislamiento de fallos).

## Future Reconsideration
Revisar tras FASE 18 con métricas; criterios de extracción en ADR-012.
