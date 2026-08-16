import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../environments/environment';

/**
 * PPIP — landing de FASE 1 (Docker infrastructure).
 *
 * Sin lógica de negocio todavía: los módulos (dashboard, opportunities,
 * compra-agil, documents, analysis, requirements, compliance, proposals,
 * traceability, settings) se implementan a partir de FASE 16 — ver
 * docs/ROADMAP.md y docs/04-architecture/03-component-diagram.md.
 *
 * Este componente sí cumple un propósito real de FASE 1: confirma
 * visualmente que el camino navegador → Traefik → Platform API → /health
 * está correctamente cableado de punta a punta.
 */
@Component({
  selector: 'app-root',
  imports: [],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly http = inject(HttpClient);

  protected readonly title = signal('PPIP');
  protected readonly apiStatus = signal<'checking' | 'ok' | 'error'>('checking');
  protected readonly apiDetail = signal<string>('Consultando Platform API…');

  constructor() {
    this.http.get<{ service: string; phase: string; status: string }>(`${environment.apiBaseUrl}/`).subscribe({
      next: (res) => {
        this.apiStatus.set('ok');
        this.apiDetail.set(`${res.service} — ${res.phase} (${res.status})`);
      },
      error: () => {
        this.apiStatus.set('error');
        this.apiDetail.set(
          `No se pudo contactar ${environment.apiBaseUrl}. Verifica que platform-api esté healthy (docker compose ps).`,
        );
      },
    });
  }
}
