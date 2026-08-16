# ADR-010 — Autenticación: Keycloak OIDC + JWT + RBAC

**Estado:** Accepted · **Fecha:** 2026-08-16

## Context
MP §24: JWT, RBAC, Keycloak, migrable a OCI IAM. SPA Angular + APIs .NET + workers sin usuario.

## Decision
Keycloak como IdP local: realm `ppip`; SPA con Authorization Code + PKCE; APIs validan JWT (issuer/audience/exp/firma) y aplican RBAC por roles `viewer` < `analyst` < `editor` < `admin`; workers usan client credentials para APIs internas; tokens de corta vida + refresh.

## Rationale
OIDC estándar garantiza portabilidad a OCI Identity Domains (mismo protocolo, cambia el issuer). Roles gruesos bastan para el POC; permisos finos serían sobreingeniería. PKCE es el flujo correcto para SPA (sin client secret en browser).

## Consequences
- (+) Identidad realista, migrable, sin código de auth propio.
- (−) Keycloak es pesado en arranque local (aceptado; perfil compose `core`).

## Rejected Alternatives
Auth propia (riesgo y esfuerzo injustificados); API keys estáticas (sin usuarios ni RBAC); Auth0/otros SaaS (rompe requisito local-first).

## Future Reconsideration
Matriz de permisos fina si aparecen más roles reales; mapeo a OCI IAM en FASE 19.
