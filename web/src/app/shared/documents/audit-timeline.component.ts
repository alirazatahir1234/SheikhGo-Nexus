import { Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';

export interface AuditTimelineEntry {
  action: string;
  timestamp: string | Date;
  oldValues?: string | null;
  newValues?: string | null;
  userId?: string | null;
}

@Component({
  selector: 'app-audit-timeline',
  standalone: true,
  imports: [DatePipe],
  template: `
    <section class="audit-timeline">
      <h3>Audit trail</h3>
      @if (!entries().length) {
        <p class="muted">No audit entries yet.</p>
      } @else {
        <ul>
          @for (e of entries(); track $index) {
            <li>
              <div class="audit-timeline__head">
                <strong>{{ e.action }}</strong>
                <span class="muted">{{ e.timestamp | date: 'medium' }}</span>
              </div>
              @if (e.userId) {
                <p class="muted">User {{ e.userId }}</p>
              }
              @if (e.oldValues || e.newValues) {
                <p class="audit-timeline__diff">
                  @if (e.oldValues) {
                    <span>{{ e.oldValues }}</span>
                  }
                  @if (e.oldValues && e.newValues) {
                    <span> → </span>
                  }
                  @if (e.newValues) {
                    <span>{{ e.newValues }}</span>
                  }
                </p>
              }
            </li>
          }
        </ul>
      }
    </section>
  `,
  styles: [
    `
      .audit-timeline {
        margin-top: 1.25rem;
      }
      h3 {
        margin: 0 0 0.65rem;
        font-size: 0.95rem;
      }
      ul {
        list-style: none;
        margin: 0;
        padding: 0;
        border-left: 2px solid var(--border);
      }
      li {
        position: relative;
        padding: 0.55rem 0 0.55rem 1rem;
        margin-left: 0.35rem;
      }
      li::before {
        content: '';
        position: absolute;
        left: -0.45rem;
        top: 0.85rem;
        width: 0.55rem;
        height: 0.55rem;
        border-radius: 999px;
        background: rgba(20, 184, 166, 0.55);
      }
      .audit-timeline__head {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem 1rem;
        align-items: baseline;
      }
      p {
        margin: 0.25rem 0 0;
        font-size: 0.85rem;
      }
      .audit-timeline__diff {
        font-family: ui-monospace, monospace;
        font-size: 0.8rem;
        color: var(--text-muted);
      }
    `,
  ],
})
export class AuditTimelineComponent {
  readonly entries = input<AuditTimelineEntry[]>([]);
}
