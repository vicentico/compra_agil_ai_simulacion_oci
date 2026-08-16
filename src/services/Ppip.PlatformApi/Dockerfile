# syntax=docker/dockerfile:1
# Build multi-stage — contexto = raíz del repo (ver docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/building-blocks/ building-blocks/
COPY src/services/Ppip.PlatformApi/ services/Ppip.PlatformApi/
WORKDIR /src/services/Ppip.PlatformApi
RUN dotnet restore
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# curl necesario para el HEALTHCHECK de Docker (no incluido en la imagen base).
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
RUN useradd --uid 1654 --user-group --no-create-home appuser && chown -R appuser /app
USER appuser
EXPOSE 8080
ENTRYPOINT ["dotnet", "Ppip.PlatformApi.dll"]
