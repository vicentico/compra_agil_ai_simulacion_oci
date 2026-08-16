# 07 — Supuestos

| ID | Supuesto | Impacto si es falso | Validación |
|---|---|---|---|
| ASM-01 | La API Compra Ágil v2 está disponible con ticket/API key personal y expone `GET /v2/compra-agil` y `GET /v2/compra-agil/{codigo}` con parámetros incrementales (ttl_cambio_ms, cambio_desde, cambio_hasta) | Rediseñar estrategia de sincronización (polling completo + diffing local) | Spike técnico al inicio de FASE 5 contra la API real |
| ASM-02 | Los documentos adjuntos son mayoritariamente PDF descargables por URL pública/autenticada | Ampliar pipeline a otros formatos (docx, xlsx, imágenes) | Muestreo de compras reales en FASE 5 |
| ASM-03 | Un LLM local vía Ollama es suficiente para demo; proveedores cloud (OpenAI/Gemini) mejoran calidad cuando hay API key | Ajustar expectativas de calidad del análisis en demo | Evaluación comparativa en /evaluation |
| ASM-04 | El volumen del POC es bajo (cientos de compras, miles de documentos), no requiere Kafka ni Kubernetes | Activar evoluciones previstas (ADR-003, MP2 §30) | Métricas de FASE 18 |
| ASM-05 | Un único tenant (una empresa proveedora) usa el sistema | Introducir multi-tenancy (FUTURE) | N/A en POC |
| ASM-06 | .NET 10 y Angular 20 están disponibles y estables en tooling local | Fijar versiones inmediatamente anteriores estables | Verificación en FASE 1 |
| ASM-07 | Los rate limits de ChileCompra permiten sincronización periódica razonable (orden de minutos) | Aumentar intervalo, priorizar por cierre próximo | Observación de 429 en FASE 5-6 |
| ASM-08 | No hay restricciones legales para almacenar copias locales de información pública de contratación y sus documentos | Revisar términos de uso; limitar retención | Revisión de términos de Mercado Público antes de FASE 5 |
