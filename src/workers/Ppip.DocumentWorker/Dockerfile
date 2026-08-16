# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Directory.Build.props src/global.json ./
COPY src/building-blocks/ building-blocks/
COPY src/workers/Ppip.DocumentWorker/ workers/Ppip.DocumentWorker/
WORKDIR /src/workers/Ppip.DocumentWorker
RUN dotnet restore
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
# La imagen base ya trae un usuario no-root "app" en UID/GID 1654 ($APP_UID) —
# crear otro con el mismo UID falla (pre-existente de FASE 1, nunca compilado
# antes en un entorno con Docker real, ver docs/ROADMAP.md nota FASE 1).
RUN chown -R $APP_UID /app
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Ppip.DocumentWorker.dll"]
