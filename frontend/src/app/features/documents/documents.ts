import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DocumentService } from '../../core/services/document.service';
import { DocumentDto } from '../../core/models';

/**
 * Documents page: upload PDF/TXT files, see them listed, delete them.
 * On upload, the backend also extracts + chunks + embeds — so once a file
 * appears here it's already searchable in Chat.
 */
@Component({
  selector: 'app-documents',
  imports: [DatePipe],
  template: `
    <div class="page">
      <header class="head">
        <div>
          <h1>Documents</h1>
          <p class="muted">Upload PDF or TXT files. They're processed and made searchable automatically.</p>
        </div>

        <!-- Hidden native file input, triggered by the button -->
        <input #fileInput type="file" accept=".pdf,.txt" hidden (change)="onFileSelected($event)" />
        <button class="btn" (click)="fileInput.click()" [disabled]="uploading()">
          {{ uploading() ? 'Uploading…' : '＋ Upload' }}
        </button>
      </header>

      @if (error()) { <div class="error-text" style="margin-bottom:1rem;">{{ error() }}</div> }

      @if (loading()) {
        <p class="muted">Loading…</p>
      } @else if (documents().length === 0) {
        <div class="card empty">
          <div style="font-size:2rem;">📄</div>
          <p>No documents yet. Upload one to get started.</p>
        </div>
      } @else {
        <div class="list">
          @for (doc of documents(); track doc.id) {
            <div class="row card">
              <div class="doc-icon">{{ doc.fileType === 'Pdf' ? '📕' : '📄' }}</div>
              <div class="doc-main">
                <div class="doc-name">{{ doc.fileName }}</div>
                <div class="doc-meta muted">
                  {{ doc.fileType }} · {{ formatSize(doc.fileSizeBytes) }} · {{ doc.uploadedAt | date:'medium' }}
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
    .page { padding: 2rem; overflow-y: auto; height: 100%; }
    .head { display: flex; align-items: flex-start; justify-content: space-between; gap: 1rem; margin-bottom: 1.5rem; }
    h1 { margin: 0 0 .25rem; }
    .head p { margin: 0; max-width: 46ch; }
    .empty { text-align: center; padding: 3rem; color: var(--text-muted); }
    .list { display: flex; flex-direction: column; gap: .6rem; }
    .row { display: flex; align-items: center; gap: 1rem; padding: .9rem 1rem; }
    .doc-icon { font-size: 1.4rem; }
    .doc-main { flex: 1; min-width: 0; }
    .doc-name { font-weight: 600; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
    .doc-meta { font-size: .8rem; margin-top: .15rem; }
    .del { color: var(--danger); border-color: transparent; }
    .del:hover:not(:disabled) { background: color-mix(in srgb, var(--danger) 12%, transparent); }
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
        this.documents.update((list) => [doc, ...list]); // prepend the new doc
        this.uploading.set(false);
        input.value = ''; // allow re-uploading the same file
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
