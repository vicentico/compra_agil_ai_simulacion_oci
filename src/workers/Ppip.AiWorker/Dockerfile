# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/building-blocks/ building-blocks/
COPY src/workers/Ppip.AiWorker/ workers/Ppip.AiWorker/
WORKDIR /src/workers/Ppip.AiWorker
RUN dotnet restore
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
RUN useradd --uid 1654 --user-group --no-create-home appuser && chown -R appuser /app
USER appuser
EXPOSE 8080
ENTRYPOINT ["dotnet", "Ppip.AiWorker.dll"]
