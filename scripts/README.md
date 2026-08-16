# /scripts

Scripts operativos.

- `smoke-test.sh` — smoke test de FASE 1 (`ComposeHealthSmokeTest` en la matriz de trazabilidad): espera a que todos los contenedores de los perfiles `core`+`app` estén `healthy` y falla explícitamente si no. Se invoca vía `make smoke`.
- `seed/` — placeholder del perfil `demo` (FR-052). No implementado todavía: el dominio del que depende se construye desde FASE 4. Ver `docs/16-operations/01-operations.md`.
