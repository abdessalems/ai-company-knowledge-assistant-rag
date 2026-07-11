import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DocumentService } from '../../core/services/document.service';
import { DocumentDto } from '../../core/models';

@Component({
  selector: 'app-documents',
  imports: [DatePipe],
  template: `
    <div class="page">
      <header class="head">
        <div>
          <h1>Documents</h1>
          <p class="muted">Upload PDF or TXT files — they're processed and made searchable automatically.</p>
        </div>
        <input #fileInput type="file" accept=".pdf,.txt" hidden (change)="onFileSelected($event)" />
        <button class="btn" (click)="fileInput.click()" [disabled]="uploading()">
          {{ uploading() ? 'Uploading…' : '＋  Upload' }}
        </button>
      </header>

      @if (error()) { <div class="error-text banner">{{ error() }}</div> }

      @if (loading()) {
        <div class="skeleton-list">
          @for (i of [1,2,3]; track i) { <div class="skeleton"></div> }
        </div>
      } @else if (documents().length === 0) {
        <button class="dropzone" (click)="fileInput.click()">
          <div class="dz-icon">📄</div>
          <div class="dz-title">No documents yet</div>
          <div class="muted">Click to upload your first PDF or TXT file</div>
        </button>
      } @else {
        <div class="list">
          @for (doc of documents(); track doc.id) {
            <div class="row animate-in">
              <div class="badge" [class.pdf]="doc.fileType === 'Pdf'">
                {{ doc.fileType === 'Pdf' ? 'PDF' : 'TXT' }}
              </div>
              <div class="main">
                <div class="name">{{ doc.fileName }}</div>
                <div class="meta muted">
                  {{ formatSize(doc.fileSizeBytes) }} · {{ doc.uploadedAt | date:'MMM d, y, h:mm a' }}
                </div>
              </div>
              <button class="btn-ghost del" (click)="remove(doc)" [disabled]="deletingId() === doc.id">
                {{ deletingId() === doc.id ? '…' : 'Delete' }}
              </button>
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .page { padding: 2.2rem 2.4rem; overflow-y: auto; height: 100%; max-width: 900px; }
    .head { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: 1.8rem; }
    h1 { margin: 0 0 .3rem; font-size: 1.55rem; }
    .head p { margin: 0; max-width: 48ch; }
    .banner { padding: .7rem .9rem; background: color-mix(in srgb, var(--danger) 10%, transparent);
      border: 1px solid color-mix(in srgb, var(--danger) 35%, transparent); border-radius: var(--radius-sm); margin-bottom: 1.2rem; }

    .dropzone {
      width: 100%; text-align: center; padding: 3.5rem 2rem; cursor: pointer;
      background: var(--surface); color: var(--text); font: inherit;
      border: 1.5px dashed var(--border-strong); border-radius: var(--radius); transition: border-color var(--t), background var(--t);
    }
    .dropzone:hover { border-color: var(--accent); background: var(--surface-2); }
    .dz-icon { font-size: 2.4rem; opacity: .8; }
    .dz-title { font-weight: 600; margin: .6rem 0 .25rem; font-size: 1.05rem; }

    .list { display: flex; flex-direction: column; gap: .7rem; }
    .row {
      display: flex; align-items: center; gap: 1rem; padding: 1rem 1.1rem;
      background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius);
      transition: transform var(--t), border-color var(--t), box-shadow var(--t);
    }
    .row:hover { transform: translateY(-2px); border-color: var(--border-strong); box-shadow: var(--shadow); }
    .badge {
      flex-shrink: 0; width: 46px; height: 46px; border-radius: 11px; display: grid; place-items: center;
      font-size: .68rem; font-weight: 800; letter-spacing: .04em;
      background: var(--surface-3); color: var(--text-muted);
    }
    .badge.pdf { background: color-mix(in srgb, var(--danger) 16%, transparent); color: #ff8a9a; }
    .main { flex: 1; min-width: 0; }
    .name { font-weight: 600; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .meta { font-size: .8rem; margin-top: .2rem; }
    .del { color: var(--danger); border-color: transparent; }
    .del:hover:not(:disabled) { background: color-mix(in srgb, var(--danger) 14%, transparent); border-color: transparent; }

    .skeleton-list { display: flex; flex-direction: column; gap: .7rem; }
    .skeleton { height: 78px; border-radius: var(--radius); background: linear-gradient(90deg, var(--surface), var(--surface-2), var(--surface));
      background-size: 200% 100%; animation: shimmer 1.3s infinite; }
    @keyframes shimmer { from { background-position: 200% 0; } to { background-position: -200% 0; } }
  `],
})
export class Documents {
  private documentService = inject(DocumentService);

  documents = signal<DocumentDto[]>([]);
  loading = signal(true);
  uploading = signal(false);
  deletingId = signal<string | null>(null);
  error = signal<string | null>(null);

  constructor() {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.documentService.list().subscribe({
      next: (docs) => {
        this.documents.set(docs);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load documents.');
        this.loading.set(false);
      },
    });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploading.set(true);
    this.error.set(null);
    this.documentService.upload(file).subscribe({
      next: (doc) => {
        this.documents.update((list) => [doc, ...list]);
        this.uploading.set(false);
        input.value = '';
      },
      error: (err) => {
        this.error.set(err?.error?.error ?? err?.error?.message ?? 'Upload failed.');
        this.uploading.set(false);
        input.value = '';
      },
    });
  }

  remove(doc: DocumentDto): void {
    if (!confirm(`Delete "${doc.fileName}"?`)) return;
    this.deletingId.set(doc.id);
    this.documentService.delete(doc.id).subscribe({
      next: () => {
        this.documents.update((list) => list.filter((d) => d.id !== doc.id));
        this.deletingId.set(null);
      },
      error: () => {
        this.error.set('Delete failed.');
        this.deletingId.set(null);
      },
    });
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
