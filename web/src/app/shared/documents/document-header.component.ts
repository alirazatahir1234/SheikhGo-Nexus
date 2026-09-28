import { Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { statusLabel } from './document-status-stepper.component';

@Component({
  selector: 'app-document-header',
  standalone: true,
  imports: [DatePipe],
  template: `
    <header class="doc-header">
      <div>
        <h2>{{ title() }}</h2>
        <p class="muted">
          <strong>{{ docNo() }}</strong>
          · {{ date() | date: 'mediumDate' }}
          · {{ partyName() || '—' }}
        </p>
        @if (remarks()) {
          <p class="doc-header__remarks">{{ remarks() }}</p>
        }
      </div>
      <span class="badge" [class.badge--muted]="statusLabel(status()) === 'Cancelled'">
        {{ statusLabel(status()) }}
      </span>
    </header>
  `,
  styles: [
    `
      .doc-header {
        display: flex;
        flex-wrap: wrap;
        align-items: flex-start;
        justify-content: space-between;
        gap: 0.75rem;
        margin-bottom: 1rem;
      }
      h2 {
        margin: 0;
        font-size: 1.15rem;
      }
      p {
        margin: 0.35rem 0 0;
        font-size: 0.9rem;
      }
      .doc-header__remarks {
        color: var(--text-muted);
        font-style: italic;
      }
      .badge--muted {
        background: #f2f4f7;
        color: var(--text-muted);
      }
    `,
  ],
})
export class DocumentHeaderComponent {
  readonly title = input.required<string>();
  readonly docNo = input.required<string>();
  readonly date = input.required<string | Date>();
  readonly partyName = input('');
  readonly status = input<string | number>('Draft');
  readonly remarks = input<string | undefined>('');

  readonly statusLabel = statusLabel;
}
