import { Component, input } from '@angular/core';
import { DatePipe } from '@angular/common';

export interface VerificationEntry {
  verifiedAt?: string | null;
  verifiedBy?: string | null;
  remarks?: string | null;
  isVerified?: boolean;
}

@Component({
  selector: 'app-verification-panel',
  standalone: true,
  imports: [DatePipe],
  template: `
    <section class="verify-panel" [class.verify-panel--required]="required()">
      <header>
        <h3>Verification</h3>
        @if (required()) {
          <span class="badge">Required</span>
        } @else {
          <span class="badge badge--muted">Not required</span>
        }
      </header>

      @if (!required()) {
        <p class="muted">This document type skips verification.</p>
      } @else if (entries().length) {
        <ul>
          @for (e of entries(); track $index) {
            <li>
              <strong>{{ e.isVerified === false ? 'Rejected' : 'Verified' }}</strong>
              @if (e.verifiedBy) {
                <span> by {{ e.verifiedBy }}</span>
              }
              @if (e.verifiedAt) {
                <span class="muted"> · {{ e.verifiedAt | date: 'medium' }}</span>
              }
              @if (e.remarks) {
                <div class="remarks">{{ e.remarks }}</div>
              }
            </li>
          }
        </ul>
      } @else {
        <p class="muted">Awaiting verification after submit.</p>
      }
    </section>
  `,
  styles: [
    `
      .verify-panel {
        margin: 1rem 0;
        padding: 0.85rem 1rem;
        border: 1px solid var(--border);
        border-radius: var(--radius-sm);
        background: var(--surface);
      }
      .verify-panel--required {
        border-color: rgba(13, 148, 136, 0.35);
      }
      header {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        margin-bottom: 0.5rem;
      }
      h3 {
        margin: 0;
        font-size: 0.9rem;
      }
      .badge {
        font-size: 0.65rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 0.04em;
        padding: 0.15rem 0.45rem;
        border-radius: 999px;
        background: rgba(13, 148, 136, 0.12);
        color: #0f766e;
      }
      .badge--muted {
        background: #f1f5f9;
        color: #64748b;
      }
      ul {
        list-style: none;
        margin: 0;
        padding: 0;
      }
      li {
        font-size: 0.85rem;
        padding: 0.35rem 0;
        border-top: 1px solid #f1f5f9;
      }
      .remarks {
        color: var(--text-muted);
        margin-top: 0.15rem;
      }
      .muted {
        color: var(--text-muted);
        font-size: 0.82rem;
        margin: 0;
      }
    `,
  ],
})
export class VerificationPanelComponent {
  readonly required = input(true);
  readonly entries = input<VerificationEntry[]>([]);
}
