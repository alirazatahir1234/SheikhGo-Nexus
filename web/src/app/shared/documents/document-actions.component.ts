import { Component, computed, input, output } from '@angular/core';
import { normalizeDocStatus } from './document-status-stepper.component';

export type DocumentAction = 'submit' | 'verify' | 'approve' | 'post' | 'cancel';

interface ActionBtn {
  action: DocumentAction;
  label: string;
  primary?: boolean;
  danger?: boolean;
}

/**
 * Thin workflow action bar driven by DocumentType flags + current status.
 * PO: Draft → Submit → Approve → Post
 * GRN: Draft → Submit → Verify → Post
 * PI: Draft → Submit → Verify → Approve → Post
 */
@Component({
  selector: 'app-document-actions',
  standalone: true,
  template: `
    @if (buttons().length) {
      <div class="doc-actions">
        <span class="muted">Workflow</span>
        <div class="doc-actions__btns">
          @for (btn of buttons(); track btn.action) {
            <button
              type="button"
              class="btn"
              [class.btn--primary]="btn.primary"
              [class.btn--ghost]="!btn.primary && !btn.danger"
              [class.btn--danger]="btn.danger"
              [disabled]="busy() || !canRun(btn.action)"
              [title]="!canRun(btn.action) ? 'Missing permission' : ''"
              (click)="action.emit(btn.action)"
            >
              {{ btn.label }}
            </button>
          }
        </div>
      </div>
    }
  `,
  styles: [
    `
      .doc-actions {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        justify-content: space-between;
        gap: 0.75rem;
        margin: 1rem 0;
        padding: 0.85rem 1rem;
        border: 1px solid var(--border);
        border-radius: var(--radius-sm);
        background: var(--surface);
      }
      .doc-actions__btns {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
      }
      .btn--danger {
        border-color: #fecdca;
        color: var(--danger, #b42318);
        background: #fef3f2;
      }
      button:disabled {
        opacity: 0.45;
        cursor: not-allowed;
      }
    `,
  ],
})
export class DocumentActionsComponent {
  readonly status = input<string | number>('Draft');
  readonly busy = input(false);
  readonly requiresVerification = input(true);
  readonly requiresApproval = input(true);
  /** Map of action → allowed */
  readonly allowed = input<Partial<Record<DocumentAction, boolean>>>({});
  readonly action = output<DocumentAction>();

  readonly buttons = computed((): ActionBtn[] => {
    const n = normalizeDocStatus(this.status());
    if (n === 4 || n === 5) return [];

    const needVerify = this.requiresVerification();
    const needApprove = this.requiresApproval();

    let next: ActionBtn | null = null;
    if (n === 0) {
      next = { action: 'submit', label: 'Submit', primary: true };
    } else if (n === 1) {
      // Submitted
      if (needVerify) next = { action: 'verify', label: 'Verify', primary: true };
      else if (needApprove) next = { action: 'approve', label: 'Approve', primary: true };
      else next = { action: 'post', label: 'Post', primary: true };
    } else if (n === 2) {
      // Verified
      if (needApprove) next = { action: 'approve', label: 'Approve', primary: true };
      else next = { action: 'post', label: 'Post', primary: true };
    } else if (n === 3) {
      next = { action: 'post', label: 'Post', primary: true };
    }

    const list: ActionBtn[] = [];
    if (next) list.push(next);
    list.push({ action: 'cancel', label: 'Cancel', danger: true });
    return list;
  });

  canRun(action: DocumentAction): boolean {
    const map = this.allowed();
    if (!(action in map)) return true;
    return !!map[action];
  }
}
