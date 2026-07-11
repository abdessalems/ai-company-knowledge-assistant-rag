import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { DocumentChunkDto, DocumentDto } from '../models';

/** Calls the /api/documents endpoints. */
@Injectable({ providedIn: 'root' })
export class DocumentService {
  private http = inject(HttpClient);
  private base = `${API_BASE_URL}/documents`;

  list(): Observable<DocumentDto[]> {
    return this.http.get<DocumentDto[]>(this.base);
  }

  /**
   * Upload a file. We send FormData (multipart/form-data) with a "file" part —
   * the field name MUST be "file" to match the API's IFormFile parameter.
   * Do NOT set Content-Type manually; HttpClient sets it (with the boundary).
   */
  upload(file: File): Observable<DocumentDto> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<DocumentDto>(`${this.base}/upload`, form);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  chunks(id: string): Observable<DocumentChunkDto[]> {
    return this.http.get<DocumentChunkDto[]>(`${this.base}/${id}/chunks`);
  }
}
