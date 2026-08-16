# src/apps/frontend — PPIP Angular 20 (esqueleto FASE 1)

Generado con `@angular/cli@20` (standalone, sin routing/SSR todavía — se incorporan al construir los módulos reales en FASE 16, ver `docs/ROADMAP.md` y `docs/04-architecture/03-component-diagram.md`).

## Qué hace hoy

Una única página que confirma que el camino **navegador → Traefik → Platform API → `/health`** está correctamente cableado (criterio de éxito de FASE 1). Sin routing, sin módulos de negocio, sin llamadas a `/api/*` reales todavía.

## Desarrollo local

```bash
npm install
npm start            # ng serve, http://localhost:4200
npm test              # Karma + Jasmine (usa Chrome instalado; en CI/root usar --browsers=ChromeHeadlessCI, ver karma.conf.js)
npm run build         # dist/ppip-frontend/browser
```

## Validado en esta entrega

`npm ci && ng build --configuration development` compila sin errores; `ng test` corre 3 specs en verde (creación del componente, título renderizado, estado de error cuando la API no responde) usando `HttpTestingController` (sin llamadas HTTP reales en tests).

## Nota de versión (transparencia, no oculta el trade-off)

El stack documentado en `docs/` fija **Angular 20** (LTS elegido explícitamente, ver `ARCHITECTURE.md`). Al momento de generar este esqueleto (2026-08), el registro npm ya publica Angular 22 estable. Se respetó la decisión documentada (20.3.x) en vez de saltar de versión sin un ADR — si se quiere adoptar 22, corresponde una decisión explícita del equipo, no un cambio silencioso de un scaffold.
