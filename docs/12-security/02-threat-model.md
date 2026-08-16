# 02 — Threat Model

Metodología ligera STRIDE por activo. P/S: Baja/Media/Alta. Mitigaciones trazan a controles de [01-security-controls.md](01-security-controls.md).

| # | Amenaza | Vector | P | S | Mitigación | Fase |
|---|---|---|---|---|---|---|
| T1 | Prompt injection | Instrucciones maliciosas dentro de PDF/bases que el RAG inyecta al LLM | A | A | Contenido = datos; delimitadores + instrucción explícita; salida solo JSON validado; sin tools desde contexto documental; evaluación adversarial en /evaluation | F9-10 |
| T2 | PDF malicioso | Exploit de parser (PDF bomb, XXE en formatos office) | M | A | Librerías mantenidas y actualizadas; límites de tamaño/páginas; parseo en worker aislado con límites de memoria/CPU; IMalwareScanner | F7-8 |
| T3 | SSRF | URLs de documentos apuntando a red interna/metadata endpoints | M | A | Allowlist de dominios; bloqueo IP privadas/loopback; no seguir redirects fuera de allowlist | F7 |
| T4 | Fuga de credenciales | Ticket/API keys en código, logs o repos | M | A | Secrets fuera de Git; escaneo CI; redacción en logs (nunca loggear headers auth); rotación | F3 |
| T5 | Abuso de API | Scraping/DoS sobre endpoints propios; abuso del RAG (costo LLM) | M | M | Rate limit por usuario e IP; presupuestos IA por usuario/operación; 429 con backoff | F3, F10 |
| T6 | Data poisoning | Documentos manipulados que sesgan análisis/propuestas | B | M | Evidencia navegable (humano verifica fuente); hash + versión de documento; provenance en cada afirmación | F8+ |
| T7 | Acceso no autorizado a propuestas | Propuestas contienen estrategia comercial sensible | M | A | RBAC (editor+); auditoría de acceso a propuestas; sin URLs públicas de MinIO (firmadas, TTL corto) | F3, F13 |
| T8 | LLM data leakage | Envío de datos sensibles (perfil de empresa, precios) a proveedores externos | M | M | Default local (Ollama); envío a cloud requiere opt-in de configuración; documentación de qué se envía por operación | F10 |
| T9 | Fuga desde vector DB | Qdrant expone chunks entre compras o a no autenticados | B | M | Qdrant en red data sin exposición; acceso solo vía API con filtro server-side; API key Qdrant | F1, F9 |
| T10 | Escalación de privilegios | Manipulación de roles/JWT | B | A | Validación completa de JWT (firma/issuer/audience/exp); roles solo desde token, jamás del body; tests de authZ | F3 |
| T11 | Supply chain | Dependencias NuGet/npm o imágenes Docker comprometidas | M | A | Lock files; dependabot/audit en CI; imágenes oficiales con digest pinning; escaneo de imágenes | F0+ |
| T12 | Container escape | Compromiso de un worker que parsea contenido hostil | B | A | Contenedores non-root, read-only fs donde aplique, sin privileged, límites de recursos; segmentación de redes | F1 |

Revisión del threat model: al cierre de FASE 3 (seguridad base), FASE 10 (IA) y FASE 18.
