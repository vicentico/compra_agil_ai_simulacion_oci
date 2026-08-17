# Fixtures — API Compra Ágil v2 (FASE 5)

Procedencia de cada archivo, honesta y explícita (no todo es igual de confiable):

| Archivo | Procedencia |
|---|---|
| `real-list-response.json` | **Capturado real** — `GET /v2/compra-agil?ttl_cambio_ms=86400000&tamano_pagina=10` contra la API real, 2026-08-16. Datos públicos de contratación (sin PII). |
| `real-detail-response.json` | **Capturado real** — `GET /v2/compra-agil/418-1191-COT26` contra la API real, mismo spike. |
| `error-400-tamano-pagina.json` | **Capturado real** — hallazgo del spike: `tamano_pagina=5` devuelve 400 con `"tamano_pagina debe estar entre 10 y 50"`. El mínimo real (10) **no está documentado** en la Guía de Uso v3.0 (que solo menciona el máximo, 50). |
| `error-401-unauthorized.json` | **Literal de la documentación** (Guía de Uso API Compra Ágil v2, §3.2/§7) — no se probó con un ticket real inválido para no gastar cuota innecesariamente (el ticket de este proyecto es de uso limitado). |
| `error-429-rate-limited.json` | **Literal de la documentación** (§4.2). |
| `error-404-not-found.json` | **Construido** a partir del patrón de envelope documentado (§7) + la causa típica de la tabla de errores — la API nunca devolvió este código durante el spike (no se probó deliberadamente con un código inexistente). |

Ningún archivo contiene el ticket real (verificado antes de commitear). Ver `docs/ROADMAP.md` nota de cierre de FASE 5 para el detalle completo de discrepancias encontradas entre la documentación oficial y el comportamiento real observado.
