# /src

Código fuente. Estructura (MP §37): `/building-blocks` (compartido, sin dependencias de negocio), `/services` (Platform API modular monolith), `/workers` (Sync/Document/AI), `/apps` (frontend Angular 20).

## Estado (FASE 1)

Esqueletos ejecutables con `/health` (liveness) y `/ready` (verifica dependencias reales: Mongo/Redis/RabbitMQ/MinIO/Qdrant/Ollama según corresponda a cada servicio — ver `docs/13-observability/01-observability-spec.md`). **Sin lógica de dominio todavía** — eso empieza en FASE 4 (Procurement domain) según `docs/ROADMAP.md`.

## Build local (requiere .NET 10 SDK — no verificado en el entorno que generó este scaffold, que no tenía el SDK instalado)

```bash
dotnet new sln -n Ppip
dotnet sln add building-blocks/**/*.csproj services/**/*.csproj workers/**/*.csproj
dotnet build
```

Los `Dockerfile` de cada servicio no dependen del .sln: publican su `.csproj` directamente (patrón monorepo estándar), así que `docker compose build` funciona sin este paso.
