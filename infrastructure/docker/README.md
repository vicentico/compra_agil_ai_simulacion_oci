# infrastructure/docker — FASE 1: Docker infrastructure

Implementa `docs/04-architecture/04-deployment-diagram.md`. Levanta la infraestructura base (perfil `core`) y los esqueletos de aplicación (perfil `app`) definidos en el Container Diagram.

## Uso rápido

```bash
cp infrastructure/docker/.env.example infrastructure/docker/.env
# editar .env con credenciales locales (nunca reales/productivas)
make up          # perfiles core + app
make ps          # estado de salud de cada contenedor
make smoke        # scripts/smoke-test.sh: falla si algo no está healthy
make logs SERVICE=platform-api
make down
```

Rutas vía Traefik (resolución automática de `*.localhost`, RFC 6761 — no requiere editar `/etc/hosts`):

| URL | Servicio |
|---|---|
| http://ppip.localhost | Frontend Angular |
| http://api.ppip.localhost | Platform API (`/health`, `/ready`) |
| http://auth.ppip.localhost | Keycloak |
| http://localhost:8080 | Dashboard de Traefik (solo dev, inseguro — ver `docker-compose.override.yml`) |

## Decisiones y límites explícitos de esta fase (transparencia de trade-offs)

1. **Sin stack de observabilidad todavía.** OTel Collector/Prometheus/Grafana/Loki son alcance de **FASE 2** (`docs/ROADMAP.md`); esta fase solo reserva la red `obs`. Mezclarlos ahora violaría la disciplina de fases del proyecto.
2. **Keycloak en modo `start-dev`** (almacenamiento `dev-file`, sin Postgres) — válido para POC local; no usar así en producción/OCI. PostgreSQL se incorpora recién en FASE 15 (ADR-002); no se introduce antes solo para Keycloak.
3. **Sin usuarios de aplicación con privilegio mínimo en MongoDB todavía** — se usan credenciales root vía `.env` porque aún no existen colecciones/repositorios reales (eso es FASE 4+). Se documenta como TODO explícito, no como omisión oculta.
4. **`/ready` de los 4 servicios .NET verifica dependencias reales** (Mongo, Redis, RabbitMQ, MinIO, Qdrant, Ollama según corresponda a cada servicio) — no es un stub que siempre responde 200. Esto es intencional: el criterio de éxito de FASE 1 es demostrar que la topología de red y las credenciales están correctamente cableadas, no solo que el proceso arrancó.
5. **Perfil `demo` es un placeholder.** El seeding real (FR-052) depende de dominio que no existe hasta FASE 4+; el contenedor existe pero documenta explícitamente que no hace nada todavía.
6. **Tags de imágenes no fijados por dígest** (se usan tags estables conocidos o `latest` documentado). Fijar por dígest queda como tarea de endurecimiento antes de FASE 19 (migración/producción-like) — ver comentarios en `docker-compose.yml`.
7. **Traefik monta el socket Docker** (`/var/run/docker.sock`) para descubrimiento de contenedores — patrón estándar de desarrollo local; en OCI el edge es un servicio gestionado (ver `docs/04-architecture/11-oci-mapping.md`), no un símil de este mecanismo.
