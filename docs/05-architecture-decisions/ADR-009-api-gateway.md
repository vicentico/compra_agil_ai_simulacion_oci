# ADR-009 — Traefik como gateway

**Estado:** Accepted · **Fecha:** 2026-08-16

## Context
Se necesita edge local que simule OCI API Gateway/LB/WAF: routing, TLS, rate limiting, headers. Candidatos del MP: Traefik/Kong/YARP.

## Decision
**Traefik** con configuración por labels de Docker Compose.

## Rationale
Integración nativa con Docker (descubrimiento por labels = cero config duplicada), TLS y rate limiting incluidos, footprint pequeño. Kong aporta plugins/portal que el POC no usa (y suma base de datos propia en modo clásico). YARP es código C# a mantener: un servicio más que programar sin necesidad — el gateway no debe contener lógica de negocio.

## Consequences
- (+) Setup mínimo, simulación fiel de edge gestionado.
- (−) Sin políticas avanzadas de API management (aceptado; documentado como límite del símil en 11-oci-mapping).

## Rejected Alternatives
Kong (peso/features sin uso), YARP (mantenimiento de código innecesario).

## Future Reconsideration
Si se necesitara transformación de payloads o auth en el edge, reevaluar Kong; hoy la auth vive en Keycloak+API.
