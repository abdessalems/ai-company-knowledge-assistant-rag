import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatService } from '../../core/services/chat.service';
import { SourceDto } from '../../core/models';

interface ChatMessage {
  role: 'user' | 'assistant';
  text: string;
  sources?: SourceDto[];
  error?: boolean;
}

@Component({
  selector: 'app-chat',
  imports: [FormsModule, DecimalPipe],
  template: `
    <div class="chat">
      @if (messages().length) {
        <div class="chat-top">
          <button class="btn-ghost new-btn" (click)="newChat()">＋ New chat</button>
        </div>
      }
      <div class="messages" #scroll>
        @if (messages().length === 0) {
          <div class="welcome">
            <div class="w-icon">🧠</div>
            <h2>Ask your documents</h2>
            <p class="muted">Answers come only from files you've uploaded — always with citations.</p>
            <div class="suggestions">
              @for (s of suggestions; track s) {
                <button class="suggestion" (click)="suggest(s)">{{ s }}</button>
              }
            </div>
          </div>
        }

        @for (m of messages(); track $index) {
          <div class="msg animate-in" [class.user]="m.role === 'user'">
            <div class="avatar" [class.assistant]="m.role === 'assistant'">
              {{ m.role === 'user' ? '🙂' : '🧠' }}
            </div>
            <div class="bubble" [class.err]="m.error">
              <div class="text">{{ m.text }}</div>
              @if (m.sources && m.sources.length) {
                <div class="chips">
                  @for (s of m.sources; track $index) {
                    <span class="chip" [title]="'Relevance ' + (s.relevance * 100 | number:'1.0-0') + '%'">
                      📄 {{ s.document }} · p.{{ s.page }}
                    </span>
                  }
                </div>
              }
            </div>
          </div>
        }

        @if (loading()) {
          <div class="msg">
            <div class="avatar assistant">🧠</div>
            <div class="bubble typing"><span></span><span></span><span></span></div>
          </div>
        }
      </div>

      <div class="composer-wrap">
        <div class="composer">
          <textarea
            #composerInput
            class="box"
            [(ngModel)]="draft"
            (keydown.enter)="onEnter($event)"
            (input)="autoGrow()"
            rows="1"
            placeholder="Ask a question about your documents…"
            [disabled]="loading()"></textarea>
          <button class="btn send" (click)="send()" [disabled]="loading() || !draft.trim()">➤</button>
        </div>
        <div class="hint muted">Enter to send · Shift+Enter for a new line</div>
      </div>
    </div>
  `,
  styles: [`
    :host { flex: 1; min-height: 0; display: flex; flex-direction: column; }
    .chat { flex: 1; min-height: 0; display: flex; flex-direction: column; }
    .chat-top { display: flex; justify-content: flex-end; padding: .7rem 1.1rem; border-bottom: 1px solid var(--border); }
    .new-btn { font-size: .82rem; padding: .4rem .8rem; }
    .messages { flex: 1; min-height: 0; overflow-y: auto; padding: 2rem 1.5rem; display: flex; flex-direction: column; gap: 1.4rem; }

    .welcome { margin: auto; text-align: center; color: var(--text-muted); max-width: 560px; }
    .w-icon {
      width: 64px; height: 64px; margin: 0 auto .8rem; display: grid; place-items: center; font-size: 1.8rem;
      background: var(--accent-soft); border: 1px solid var(--border); border-radius: 18px;
    }
    .welcome h2 { color: var(--text); margin: .3rem 0 .35rem; font-size: 1.5rem; }
    .suggestions { display: grid; grid-template-columns: 1fr 1fr; gap: .6rem; margin-top: 1.6rem; }
    .suggestion {
      text-align: left; padding: .8rem .95rem; background: var(--surface); color: var(--text);
      border: 1px solid var(--border); border-radius: var(--radius-sm); font: inherit; font-size: .88rem;
      cursor: pointer; transition: border-color var(--t), background var(--t), transform var(--t);
    }
    .suggestion:hover { border-color: var(--accent); background: var(--surface-2); transform: translateY(-1px); }

    .msg { display: flex; gap: .8rem; max-width: 820px; width: 100%; margin: 0 auto; }
    .msg.user { flex-direction: row-reverse; }
    .avatar {
      width: 34px; height: 34px; border-radius: 10px; flex-shrink: 0; display: grid; place-items: center; font-size: 1rem;
      background: var(--surface-2); border: 1px solid var(--border);
    }
    .avatar.assistant { background: var(--accent-soft); }
    .bubble {
      background: var(--surface); border: 1px solid var(--border);
      border-radius: 16px; padding: .85rem 1.1rem; max-width: 82%; box-shadow: var(--shadow-sm);
    }
    .msg.user .bubble { background: var(--accent-soft); border-color: color-mix(in srgb, var(--accent) 30%, transparent); }
    .bubble.err { border-color: var(--danger); }
    .text { white-space: pre-wrap; line-height: 1.6; }
    .chips { display: flex; flex-wrap: wrap; gap: .45rem; margin-top: .75rem; padding-top: .6rem; border-top: 1px solid var(--border); }
    .chip {
      font-size: .72rem; background: var(--chip); border: 1px solid var(--border);
      color: var(--text-muted); padding: .25rem .6rem; border-radius: 20px; cursor: default;
    }

    /* typing dots */
    .typing { display: flex; gap: .3rem; align-items: center; }
    .typing span { width: 7px; height: 7px; border-radius: 50%; background: var(--text-muted); animation: bounce 1.2s infinite; }
    .typing span:nth-child(2) { animation-delay: .15s; }
    .typing span:nth-child(3) { animation-delay: .3s; }
    @keyframes bounce { 0%, 60%, 100% { transform: translateY(0); opacity: .4; } 30% { transform: translateY(-5px); opacity: 1; } }

    .composer-wrap { padding: .5rem 1.5rem 1.1rem; max-width: 820px; width: 100%; margin: 0 auto; box-sizing: border-box; }
    .composer {
      display: flex; gap: .5rem; align-items: flex-end; padding: .5rem .5rem .5rem .9rem;
      background: var(--surface); border: 1px solid var(--border); border-radius: var(--radius); transition: border-color var(--t), box-shadow var(--t);
    }
    .composer:focus-within { border-color: var(--accent); box-shadow: var(--ring); }
    .box {
      flex: 1; resize: none; max-height: 170px; background: transparent; border: none; color: var(--text);
      font: inherit; padding: .45rem 0; outline: none;
    }
    .box::placeholder { color: var(--text-faint); }
    .send { padding: 0; width: 40px; height: 40px; flex-shrink: 0; font-size: 1rem; border-radius: 10px; }
    .hint { text-align: center; font-size: .74rem; margin-top: .5rem; }
  `],
})
export class Chat {
  private chatService = inject(ChatService);
  private scroll = viewChild<ElementRef<HTMLDivElement>>('scroll');
  private composerInput = viewChild<ElementRef<HTMLTextAreaElement>>('composerInput');

  messages = signal<ChatMessage[]>([]);
  loading = signal(false);
  draft = '';

  suggestions = [
    'How many vacation days do employees get?',
    'What are the standard working hours?',
    'What are the password requirements?',
    'How many days per week can I work remotely?',
  ];

  suggest(question: string): void {
    this.draft = question;
    this.send();
  }

  /** Grow the textarea with its content, up to the CSS max-height. */
  autoGrow(): void {
    const ta = this.composerInput()?.nativeElement;
    if (!ta) return;
    ta.style.height = 'auto';
    ta.style.height = Math.min(ta.scrollHeight, 170) + 'px';
  }

  /** Start a fresh conversation. */
  newChat(): void {
    this.messages.set([]);
    this.draft = '';
    this.resetComposerHeight();
  }

  private resetComposerHeight(): void {
    const ta = this.composerInput()?.nativeElement;
    if (ta) ta.style.height = 'auto';
  }

  onEnter(event: Event): void {
    const ke = event as KeyboardEvent;
    if (ke.shiftKey) return;
    event.preventDefault();
    this.send();
  }

  send(): void {
    const question = this.draft.trim();
    if (!question || this.loading()) return;

    this.messages.update((m) => [...m, { role: 'user', text: question }]);
    this.draft = '';
    this.resetComposerHeight();
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
    setTimeout(() => {
      const el = this.scroll()?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    }, 0);
  }
}
