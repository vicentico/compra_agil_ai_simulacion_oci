# infrastructure/docker/config/keycloak — realm `ppip` (FASE 3)

`ppip-realm.json` se importa automáticamente al arrancar Keycloak (`--import-realm`, no reimporta si el realm ya existe). Define:

- **5 roles compuestos** (`viewer < analyst < editor < admin < superadmin`, ADR-010 + Amendment): cada rol superior incluye los inferiores vía `composites` — Keycloak expande la composición en `realm_access.roles` del token, sin lógica de jerarquía en el código .NET.
- **`ppip-spa`**: cliente público, Authorization Code + PKCE, para el frontend Angular (integración real en FASE 16).
- **`ppip-test-client`**: cliente confidencial con Direct Access Grants (password), usado **exclusivamente** por `tests/Ppip.PlatformApi.Tests` para obtener tokens de los 5 usuarios de prueba. No se usa en ningún flujo de producción/demo.
- **5 usuarios de prueba** (`{rol}.test`, contraseña `PpipTest123!`).

**El secreto de `ppip-test-client` y las contraseñas de los usuarios de prueba están en texto plano en este archivo, versionado en git — es intencional, no un descuido.** Este Keycloak corre en modo `start-dev` con almacenamiento efímero (`dev-file`, ver `infrastructure/docker/README.md`) y nunca se expone fuera de `localhost`; no protege ningún dato real. Si el proyecto llega a un despliegue no-local, este archivo completo se reemplaza (realm de producción con secretos reales fuera de Git, per `docs/12-security/01-security-controls.md`).

Ambos clientes agregan `ppip-platform-api` como audience custom del token (`oidc-audience-mapper`), porque Platform API valida `aud` sin estar registrada como cliente Keycloak (no lo necesita para validar JWT vía JWKS).
