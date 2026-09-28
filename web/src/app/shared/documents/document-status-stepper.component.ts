import { Component, computed, input } from '@angular/core';

const STEPS = ['Draft', 'Submitted', 'Verified', 'Approved', 'Posted'] as const;

const STATUS_MAP: Record<string, number> = {
  Draft: 0,
  Submitted: 1,
  Verified: 2,
  Approved: 3,
  Posted: 4,
  Cancelled: 5,
  '0': 0,
  '1': 1,
  '2': 2,
  '3': 3,
  '4': 4,
  '5': 5,
};

export function normalizeDocStatus(status: string | number | null | undefined): number {
  if (status == null || status === '') return 0;
  if (typeof status === 'number') return Math.min(5, Math.max(0, status));
  const key = String(status).trim();
  return STATUS_MAP[key] ?? STATUS_MAP[key.charAt(0).toUpperCase() + key.slice(1).toLowerCase()] ?? 0;
}

export function statusLabel(status: string | number | null | undefined): string {
  const n = normalizeDocStatus(status);
  if (n === 5) return 'Cancelled';
  return STEPS[n] ?? 'Draft';
}

@Component({
  selector: 'app-document-status-stepper',
  standalone: true,
  template: `
    @if (isCancelled()) {
      <div class="doc-stepper doc-stepper--cancelled">
        <span class="badge badge--danger">Cancelled</span>
        <p class="muted">This document was cancelled and cannot advance.</p>
      </div>
    } @else {
      <ol class="doc-stepper" aria-label="Document status">
        @for (step of steps; track step; let i = $index) {
          <li
            class="doc-stepper__step"
            [class.doc-stepper__step--done]="i < currentIndex()"
            [class.doc-stepper__step--active]="i === currentIndex()"
          >
            <span class="doc-stepper__dot">{{ i + 1 }}</span>
            <span class="doc-stepper__label">{{ step }}</span>
          </li>
        }
      </ol>
    }
  `,
  styles: [
    `
      .doc-stepper {
        display: flex;
        flex-wrap: wrap;
        gap: 0.35rem 0.5rem;
        list-style: none;
        margin: 0 0 1rem;
        padding: 0;
        align-items: center;
      }
      .doc-stepper--cancelled {
        display: block;
        margin-bottom: 1rem;
      }
      .doc-stepper--cancelled p {
        margin: 0.4rem 0 0;
        font-size: 0.85rem;
      }
      .doc-stepper__step {
        display: inline-flex;
        align-items: center;
        gap: 0.35rem;
        color: var(--text-muted);
        font-size: 0.8rem;
        font-weight: 600;
      }
      .doc-stepper__step:not(:last-child)::after {
        content: '→';
        margin-left: 0.5rem;
        color: var(--border);
        font-weight: 400;
      }
      .doc-stepper__dot {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 1.35rem;
        height: 1.35rem;
        border-radius: 999px;
        border: 1px solid var(--border);
        background: var(--surface);
        font-size: 0.7rem;
      }
      .doc-stepper__step--done {
        color: var(--teal-600, #0f766e);
      }
      .doc-stepper__step--done .doc-stepper__dot {
        background: rgba(20, 184, 166, 0.16);
        border-color: transparent;
      }
      .doc-stepper__step--active {
        color: var(--text);
      }
      .doc-stepper__step--active .doc-stepper__dot {
        background: rgba(20, 184, 166, 0.28);
        border-color: #0f766e;
      }
      .badge--danger {
        background: #fef3f2;
        color: var(--danger, #b42318);
      }
    `,
  ],
})
export class DocumentStatusStepperComponent {
  readonly status = input<string | number>('Draft');
  readonly steps = STEPS;

  readonly currentIndex = computed(() => {
    const n = normalizeDocStatus(this.status());
    return n === 5 ? -1 : n;
  });

  readonly isCancelled = computed(() => normalizeDocStatus(this.status()) === 5);
}
