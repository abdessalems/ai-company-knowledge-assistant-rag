import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ChatService } from '../../core/services/chat.service';
import { SourceDto } from '../../core/models';

interface ChatMessage {
  role: 'user' | 'assistant';
  text: string;
  sources?: SourceDto[];
  error?: boolean;
}

/**
 * RAG chat: type a question, the answer streams back grounded in the user's
 * documents, with citation chips (document + page).
 */
@Component({
  selector: 'app-chat',
  imports: [FormsModule],
  template: `
    <div class="chat">
      <div class="messages" #scroll>
        @if (messages().length === 0) {
          <div class="welcome">
            <div class="w-icon">💬</div>
            <h2>Ask your documents</h2>
            <p class="muted">Answers come only from files you've uploaded, with citations.</p>
          </div>
        }

        @for (m of messages(); track $index) {
          <div class="msg" [class.user]="m.role === 'user'">
            <div class="avatar" [class.assistant]="m.role === 'assistant'">
              {{ m.role === 'user' ? 'You' : 'AI' }}
            </div>
            <div class="bubble" [class.err]="m.error">
              <div class="text">{{ m.text }}</div>
              @if (m.sources && m.sources.length) {
                <div class="chips">
                  @for (s of m.sources; track $index) {
                    <span class="chip">📄 {{ s.document }} · p.{{ s.page }}</span>
                  }
                </div>
              }
            </div>
          </div>
        }

        @if (loading()) {
          <div class="msg">
            <div class="avatar assistant">AI</div>
            <div class="bubble"><span class="dots">Thinking…</span></div>
          </div>
        }
      </div>

      <div class="composer">
        <textarea
          class="input box"
          [(ngModel)]="draft"
          (keydown.enter)="onEnter($event)"
          rows="1"
          placeholder="Ask a question about your documents…"
          [disabled]="loading()"></textarea>
        <button class="btn send" (click)="send()" [disabled]="loading() || !draft.trim()">Send</button>
      </div>
    </div>
  `,
  styles: [`
    .chat { display: flex; flex-direction: column; height: 100%; }
    .messages { flex: 1; overflow-y: auto; padding: 1.5rem; display: flex; flex-direction: column; gap: 1.25rem; }
    .welcome { margin: auto; text-align: center; color: var(--text-muted); }
    .w-icon { font-size: 2.5rem; }
    .welcome h2 { color: var(--text); margin: .5rem 0 .25rem; }

    .msg { display: flex; gap: .75rem; max-width: 820px; width: 100%; margin: 0 auto; }
    .msg.user { flex-direction: row-reverse; }
    .avatar {
      width: 30px; height: 30px; border-radius: 7px; flex-shrink: 0;
      background: var(--surface-2); color: var(--text-muted);
      display: flex; align-items: center; justify-content: center; font-size: .7rem; font-weight: 700;
    }
    .avatar.assistant { background: var(--accent); color: #fff; }
    .bubble {
      background: var(--surface); border: 1px solid var(--border);
      border-radius: var(--radius); padding: .75rem 1rem; max-width: 80%;
    }
    .msg.user .bubble { background: var(--surface-2); }
    .bubble.err { border-color: var(--danger); }
    .text { white-space: pre-wrap; line-height: 1.5; }
    .chips { display: flex; flex-wrap: wrap; gap: .4rem; margin-top: .6rem; }
    .chip {
      font-size: .72rem; background: var(--chip); border: 1px solid var(--border);
      color: var(--text-muted); padding: .2rem .5rem; border-radius: 20px;
    }
    .dots { color: var(--text-muted); }

    .composer {
      display: flex; gap: .6rem; padding: 1rem 1.5rem; border-top: 1px solid var(--border);
      max-width: 820px; width: 100%; margin: 0 auto; box-sizing: border-box;
    }
    .box { resize: none; max-height: 160px; }
    .send { align-self: stretch; }
  `],
})
export class Chat {
  private chatService = inject(ChatService);
  private scroll = viewChild<ElementRef<HTMLDivElement>>('scroll');

  messages = signal<ChatMessage[]>([]);
  loading = signal(false);
  draft = '';

  onEnter(event: Event): void {
    const ke = event as KeyboardEvent;
    if (ke.shiftKey) return; // Shift+Enter = newline
    event.preventDefault();
    this.send();
  }

  send(): void {
    const question = this.draft.trim();
    if (!question || this.loading()) return;

    this.messages.update((m) => [...m, { role: 'user', text: question }]);
    this.draft = '';
    this.loading.set(true);
    this.scrollToBottom();

    this.chatService.ask(question).subscribe({
      next: (res) => {
        this.messages.update((m) => [...m, { role: 'assistant', text: res.answer, sources: res.sources }]);
        this.loading.set(false);
        this.scrollToBottom();
      },
      error: () => {
        this.messages.update((m) => [...m, { role: 'assistant', text: 'Something went wrong. Please try again.', error: true }]);
        this.loading.set(false);
        this.scrollToBottom();
      },
    });
  }

  private scrollToBottom(): void {
    // Wait a tick so the new message is in the DOM before scrolling.
    setTimeout(() => {
      const el = this.scroll()?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    }, 0);
  }
}
