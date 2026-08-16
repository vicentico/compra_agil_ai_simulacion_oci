# 08 — Registro de riesgos

Escala: Probabilidad (P) y Severidad (S) en Baja/Media/Alta.

| ID | Riesgo | P | S | Mitigación | Dueño |
|---|---|---|---|---|---|
| RSK-01 | Cambio o inestabilidad de la API ChileCompra (contrato, auth, rate limits) | M | A | Anti-corruption layer; raw payload preservado; contract tests contra fixtures; circuit breaker | Sync Worker |
| RSK-02 | Alucinaciones del LLM en análisis/propuestas con consecuencias comerciales | A | A | Evidencia obligatoria, FACT/INFERENCE/UNKNOWN, JSON Schema, compliance determinístico primero, human-in-the-loop, evaluación continua | AI Governance |
| RSK-03 | Prompt injection desde documentos externos | M | A | Documentos = datos no confiables; instrucción explícita al LLM; separación system/context; sin herramientas ejecutables desde contenido documental | Security |
| RSK-04 | PDFs corruptos, escaneados de baja calidad u OCR deficiente | A | M | Clasificación previa, confianza OCR persistida, etapas reintentables, estado de fallo visible al usuario | Document Worker |
| RSK-05 | Sobreingeniería: microservicios/K8s prematuros consumen el presupuesto del POC | A | M | Modular monolith (ADR-001), gates por fase, MP2 §38 como regla de revisión | Arquitectura |
| RSK-06 | Costos IA descontrolados con proveedores cloud | M | M | Cost tracking (NFR-016), cache, modelos pequeños para clasificación, Ollama por defecto | AI Governance |
| RSK-07 | Fuga de secretos (ticket ChileCompra, API keys LLM) | B | A | Secretos fuera del código, Docker secrets, escaneo, rotación documentada | Security |
| RSK-08 | Pérdida de trazabilidad al evolucionar esquemas de eventos/datos | M | M | Eventos versionados compatibles hacia atrás (NFR-019), ADRs por cambio de contrato | Arquitectura |
| RSK-09 | Concurrencia humano-IA en edición de propuestas corrompe versiones | M | M | Versionado append-only + bloqueo optimista (FR-035); escenario en docs/14-reliability | Proposal Service |
| RSK-10 | Dependencia de un único desarrollador/entorno local | M | M | Documentación como producto; docker compose reproducible; seed y demo mode | Operaciones |
| RSK-11 | Datos derivados (Qdrant) desincronizados de la fuente (MongoDB/MinIO) | M | M | Índices reconstruibles; jobs de reconciliación; hash por chunk | Knowledge |
| RSK-12 | El alcance MUST es demasiado grande para el POC | M | A | Roadmap incremental con gates; recorte consciente vía MoSCoW, nunca silencioso | Producto |
