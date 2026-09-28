import { Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DocumentWorkflowActors } from '../../core/services/purchases.service';

@Component({
  selector: 'app-workflow-actors',
  standalone: true,
  imports: [DatePipe],
  template: `
    @if (workflow(); as w) {
      <section class="actors">
        <h3>Workflow actors</h3>
        <ul>
          @for (row of rows(w); track row.label) {
            <li>
              <span class="label">{{ row.label }}</span>
              <span class="who">{{ row.who }}</span>
              @if (row.at) {
                <span class="when">{{ row.at | date: 'medium' }}</span>
              }
            </li>
          }
        </ul>
      </section>
    }
  `,
  styles: [
    `
      .actors {
        margin: 1rem 0;
        padding: 0.85rem 1rem;
        border: 1px solid var(--border);
        border-radius: var(--radius-sm);
        background: var(--surface);
      }
      h3 {
        margin: 0 0 0.5rem;
        font-size: 0.9rem;
      }
      ul {
        list-style: none;
        margin: 0;
        padding: 0;
      }
      li {
        display: grid;
        grid-template-columns: 6.5rem 1fr auto;
        gap: 0.5rem;
        font-size: 0.82rem;
        padding: 0.3rem 0;
        border-top: 1px solid #f1f5f9;
      }
      .label {
        font-weight: 600;
        color: var(--text-muted);
      }
      .when {
        color: var(--text-muted);
        white-space: nowrap;
      }
    `,
  ],
})
export class WorkflowActorsComponent {
  readonly workflow = input<DocumentWorkflowActors | null | undefined>(null);

  rows(w: DocumentWorkflowActors): { label: string; who: string; at?: string | null }[] {
    const out: { label: string; who: string; at?: string | null }[] = [];
    const push = (label: string, actor?: { userName?: string | null; userId?: string | null; at?: string | null } | null) => {
      if (!actor) return;
      out.push({
        label,
        who: actor.userName || actor.userId || '—',
        at: actor.at,
      });
    };
    push('Created', w.created);
    push('Submitted', w.submitted);
    push('Verified', w.verified);
    push('Approved', w.approved);
    push('Posted', w.posted);
    return out;
  }
}
