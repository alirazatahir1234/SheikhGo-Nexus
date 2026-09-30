import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import {
  AiApiService,
  AiActionPreview,
  AiAgentRun,
  AiChatResponse,
  AiConversationListItem,
  AiMessage,
} from '../../core/services/ai-api.service';

@Component({
  selector: 'app-ai',
  standalone: true,
  imports: [FormsModule, PageHeaderComponent, DatePipe],
  templateUrl: './ai.component.html',
  styleUrl: './ai.component.scss',
})
export class AiComponent implements OnInit {
  readonly conversations = signal<AiConversationListItem[]>([]);
  readonly messages = signal<AiMessage[]>([]);
  readonly conversationId = signal<string | null>(null);
  readonly input = signal('');
  readonly loading = signal(false);
  readonly error = signal('');
  readonly lastMeta = signal<{ intent: string; sources: string[]; denied?: boolean; kind?: string } | null>(null);
  readonly activeAction = signal<AiActionPreview | null>(null);
  readonly activeAgent = signal<AiAgentRun | null>(null);
  readonly editQty = signal<number | null>(null);
  readonly editRate = signal<number | null>(null);
  readonly editAmount = signal<number | null>(null);

  constructor(private readonly api: AiApiService) {}

  ngOnInit(): void {
    this.refreshList();
  }

  refreshList(): void {
    this.api.conversations().subscribe({
      next: (rows) => this.conversations.set(rows),
      error: () => this.conversations.set([]),
    });
  }

  newChat(): void {
    this.conversationId.set(null);
    this.messages.set([]);
    this.lastMeta.set(null);
    this.activeAction.set(null);
    this.activeAgent.set(null);
    this.error.set('');
  }

  openConversation(id: string): void {
    this.loading.set(true);
    this.error.set('');
    this.activeAction.set(null);
    this.activeAgent.set(null);
    this.api.conversation(id).subscribe({
      next: (detail) => {
        this.conversationId.set(detail.id);
        this.messages.set(detail.messages);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Could not load conversation.');
      },
    });
  }

  send(): void {
    const text = this.input().trim();
    if (!text || this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    this.messages.update((m) => [
      ...m,
      { id: crypto.randomUUID(), role: 'user', content: text, createdAt: new Date().toISOString() },
    ]);
    this.input.set('');

    this.api.chat(text, this.conversationId()).subscribe({
      next: (res: AiChatResponse) => {
        this.conversationId.set(res.conversationId);
        this.messages.update((m) => [
          ...m,
          {
            id: crypto.randomUUID(),
            role: 'assistant',
            content: res.answer,
            intent: res.intent,
            kind: res.kind,
            action: res.action ?? null,
            agent: res.agent ?? null,
            createdAt: new Date().toISOString(),
          },
        ]);
        this.lastMeta.set({
          intent: res.intent,
          sources: (res.sources || []).map((s) => s.label || s.tool),
          denied: res.denied,
          kind: res.kind,
        });
        if (res.agent) {
          this.activeAgent.set(res.agent);
          this.activeAction.set(null);
        } else if (res.action) {
          this.setActiveAction(res.action);
          this.activeAgent.set(null);
        }
        this.loading.set(false);
        this.refreshList();
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'AI request failed.');
      },
    });
  }

  private setActiveAction(action: AiActionPreview): void {
    this.activeAction.set(action);
    this.editQty.set(this.numField(action, 'qty'));
    this.editRate.set(this.numField(action, 'rate'));
    this.editAmount.set(this.numField(action, 'amount'));
  }

  private numField(action: AiActionPreview, key: string): number | null {
    const f = action.fields?.find((x) => x.key === key);
    if (!f?.value) return null;
    const n = Number(f.value);
    return Number.isFinite(n) ? n : null;
  }

  applyEdits(): void {
    const action = this.activeAction();
    if (!action || action.status !== 'Pending' || this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    const body: { qty?: number; rate?: number; amount?: number } = {};
    if (this.editQty() != null) body.qty = this.editQty()!;
    if (this.editRate() != null) body.rate = this.editRate()!;
    if (this.editAmount() != null) body.amount = this.editAmount()!;
    this.api.updateAction(action.id, body).subscribe({
      next: (updated) => {
        this.setActiveAction(updated);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Could not update draft.');
      },
    });
  }

  confirmAction(): void {
    const action = this.activeAction();
    if (!action || action.status !== 'Pending' || this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    this.api.confirmAction(action.id).subscribe({
      next: (res) => {
        this.messages.update((m) => [
          ...m,
          {
            id: crypto.randomUUID(),
            role: 'assistant',
            content: res.message + (res.documentNo ? `\nDocument: ${res.documentNo}` : ''),
            kind: 'Executed',
            createdAt: new Date().toISOString(),
          },
        ]);
        this.activeAction.update((a) =>
          a
            ? {
                ...a,
                status: res.status,
                resultDocumentId: res.documentId,
                resultDocumentNo: res.documentNo,
              }
            : a
        );
        this.lastMeta.set({ intent: action.actionType, sources: [], kind: 'Executed' });
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Confirm failed.');
      },
    });
  }

  cancelAction(): void {
    const action = this.activeAction();
    if (!action || action.status !== 'Pending' || this.loading()) return;
    this.loading.set(true);
    this.api.cancelAction(action.id).subscribe({
      next: (updated) => {
        this.setActiveAction(updated);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Cancel failed.');
      },
    });
  }

  confirmAgent(): void {
    const agent = this.activeAgent();
    if (!agent || agent.status !== 'WaitingConfirmation' || this.loading()) return;
    this.loading.set(true);
    this.error.set('');
    this.api.confirmAgent(agent.id).subscribe({
      next: (res) => {
        const docs = (res.results || [])
          .map((r) => r.documentNo)
          .filter(Boolean)
          .join(', ');
        this.messages.update((m) => [
          ...m,
          {
            id: crypto.randomUUID(),
            role: 'assistant',
            content: res.message + (docs ? `\nDocuments: ${docs}` : ''),
            kind: 'Executed',
            createdAt: new Date().toISOString(),
          },
        ]);
        this.api.getAgent(agent.id).subscribe({
          next: (updated) => {
            this.activeAgent.set(updated);
            this.lastMeta.set({ intent: updated.agentType, sources: [], kind: 'Executed' });
            this.loading.set(false);
          },
          error: () => {
            this.activeAgent.update((a) => (a ? { ...a, status: res.status } : a));
            this.loading.set(false);
          },
        });
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Agent confirm failed.');
      },
    });
  }

  cancelAgent(): void {
    const agent = this.activeAgent();
    if (!agent || this.loading()) return;
    if (agent.status === 'Completed' || agent.status === 'Cancelled') return;
    this.loading.set(true);
    this.api.cancelAgent(agent.id).subscribe({
      next: (updated) => {
        this.activeAgent.set(updated);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err?.error?.message || 'Agent cancel failed.');
      },
    });
  }
}
