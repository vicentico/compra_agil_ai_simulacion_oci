# 04 — Deployment Diagram (local, FASE 1+)

```mermaid
flowchart TB
    subgraph Host[Host del desarrollador - Docker Compose]
        subgraph net_edge[network: edge]
            TR[traefik :80/:443]
        end
        subgraph net_app[network: app]
            SPA[frontend nginx]
            API[platform-api]
            SW[sync-worker]
            DW[document-worker]
            AIW[ai-worker]
            KC[keycloak]
            OLL[ollama]
        end
        subgraph net_data[network: data - sin acceso desde edge]
            MDB[(mongodb + volumen)]
            PG[(postgresql + volumen)]
            MIO[(minio + volumen)]
            QD[(qdrant + volumen)]
            RD[(redis)]
            MQ[(rabbitmq + volumen)]
        end
        subgraph net_obs[network: obs]
            OTL[otel-collector]
            PROM[prometheus + volumen]
            GRAF[grafana + volumen]
            LOKI[loki + volumen]
        end
    end
    TR --> SPA & API & KC & GRAF
    API & SW & DW & AIW --> MDB & MQ & RD
    DW --> MIO & QD
    API --> MIO & QD & PG
    AIW --> QD & OLL
    API & SW & DW & AIW -.OTLP.-> OTL
```

## Convenciones

- **Redes segmentadas**: `edge` (solo Traefik), `app`, `data` (sin exposición al host salvo puertos de desarrollo), `obs`.
- **Volúmenes nombrados** por servicio de datos; backups fuera de alcance del POC (documentado en operaciones).
- **Secretos**: Docker secrets / archivo `.env` no versionado; `.env.example` en el repo.
- **Health checks** en cada servicio; `depends_on: condition: service_healthy` para orden de arranque.
- **Perfiles Compose**: `core` (infra), `app`, `obs`, `demo` — permite levantar subconjuntos.
- Evolución: mismas imágenes → múltiples instancias → Kubernetes → OKE ([../17-oci-migration/](../17-oci-migration/)).
