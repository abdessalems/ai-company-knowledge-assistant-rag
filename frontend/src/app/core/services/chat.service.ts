import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { ChatResponse } from '../models';

/** Calls the RAG chat endpoint. */
@Injectable({ providedIn: 'root' })
export class ChatService {
  private http = inject(HttpClient);

  ask(question: string): Observable<ChatResponse> {
    return this.http.post<ChatResponse>(`${API_BASE_URL}/chat/ask`, { question });
  }
}
