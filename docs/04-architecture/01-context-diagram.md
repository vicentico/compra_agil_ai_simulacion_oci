# 01 — System Context Diagram (C4 nivel 1)

```mermaid
C4Context
    title Public Procurement Intelligence Platform — Contexto
    Person(user, "Usuario", "Analista/comercial: explora compras, revisa análisis, edita propuestas")
    Person(admin, "Administrador", "Configura credenciales, plantillas, perfil de empresa, roles")
    System(ppip, "Public Procurement Intelligence Platform", "Sincroniza Compras Ágiles, procesa documentos, RAG, análisis IA, propuestas y compliance")
    System_Ext(chilecompra, "ChileCompra / Mercado Público", "API pública Compra Ágil v2 + documentos adjuntos")
    System_Ext(llm, "Proveedores LLM externos", "OpenAI / Gemini (opcionales; Ollama es local)")
    Rel(user, ppip, "Consulta, pregunta (RAG), edita propuestas", "HTTPS")
    Rel(admin, ppip, "Administra", "HTTPS")
    Rel(ppip, chilecompra, "Sincroniza procesos y descarga documentos", "HTTPS + ticket")
    Rel(ppip, llm, "Análisis / generación (opcional)", "HTTPS + API key")
```

## Notas

- ChileCompra es source of truth externo; el sistema mantiene copia local auditable (ACL en Procurement).
- Los proveedores LLM externos son opcionales: el sistema es demostrable 100% local con Ollama + MockOcr + seed data.
- No existe integración de salida hacia Mercado Público (no se presenta la cotización automáticamente): la propuesta generada es un entregable para que el humano la presente. Decisión consciente de human-in-the-loop y de alcance.
