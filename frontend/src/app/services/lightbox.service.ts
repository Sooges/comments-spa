import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';

export type LightboxContent =
  | { kind: 'image'; url: string; caption: string }
  | { kind: 'text'; text: string; caption: string };

// A window over the page where an attached file is shown. There is only one such window on the page,
// the components just ask this service to open a file.
@Injectable({ providedIn: 'root' })
export class LightboxService {
  private readonly http = inject(HttpClient);

  readonly content = signal<LightboxContent | null>(null);

  openImage(url: string, caption: string): void {
    this.content.set({ kind: 'image', url, caption });
  }

  openText(url: string, caption: string): void {
    this.http.get(url, { responseType: 'text' }).subscribe({
      next: text => this.content.set({ kind: 'text', text, caption }),
      error: () => this.content.set({ kind: 'text', text: 'Не удалось загрузить файл.', caption })
    });
  }

  close(): void {
    this.content.set(null);
  }
}
