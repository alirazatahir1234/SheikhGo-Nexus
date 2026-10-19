import { Component, computed, input } from '@angular/core';

export type KpiTrend = 'up' | 'down' | 'flat';

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  template: `
    <article class="kpi" [class.kpi--accent]="!!accent()">
      <div class="kpi__top">
        <p class="kpi__label">{{ label() }}</p>
        @if (showDelta()) {
          <span class="kpi__delta" [class]="deltaClass()">
            {{ delta()! > 0 ? '+' : '' }}{{ formatDelta(delta()!) }}%
          </span>
        }
      </div>
      <p class="kpi__value">{{ displayValue() }}</p>
      @if (compareHint()) {
        <p class="kpi__hint">{{ compareHint() }}</p>
      } @else if (hint()) {
        <p class="kpi__hint">{{ hint() }}</p>
      }
      @if (sparkline().length > 1) {
        <svg class="kpi__spark" viewBox="0 0 100 28" preserveAspectRatio="none" aria-hidden="true">
          <polyline [attr.points]="sparkPoints()" fill="none" stroke="currentColor" stroke-width="2" />
        </svg>
      }
    </article>
  `,
  styles: [
    `
      .kpi {
        background: var(--surface);
        border: 1px solid var(--border);
        border-radius: var(--radius);
        padding: 1rem 1.15rem 0.85rem;
        box-shadow: var(--shadow-sm);
        display: flex;
        flex-direction: column;
        gap: 0.1rem;
        min-height: 7rem;
      }
      .kpi--accent {
        border-top: 3px solid var(--kpi-accent, var(--teal-500));
      }
      .kpi__top {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 0.5rem;
      }
      .kpi__label {
        margin: 0;
        font-size: 0.78rem;
        font-weight: 600;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        color: var(--text-muted);
      }
      .kpi__delta {
        font-size: 0.75rem;
        font-weight: 700;
        padding: 0.15rem 0.45rem;
        border-radius: 999px;
        white-space: nowrap;
      }
      .kpi__delta--up {
        color: var(--success);
        background: rgba(6, 118, 71, 0.1);
      }
      .kpi__delta--down {
        color: var(--warn);
        background: rgba(181, 71, 8, 0.12);
      }
      .kpi__delta--flat {
        color: var(--text-muted);
        background: color-mix(in srgb, var(--slate-600) 14%, transparent);
      }
      .kpi__value {
        margin: 0.35rem 0 0;
        font-family: var(--font-display);
        font-size: 1.45rem;
        font-weight: 700;
        color: var(--text);
        letter-spacing: -0.02em;
      }
      .kpi__hint {
        margin: 0.2rem 0 0;
        font-size: 0.78rem;
        color: var(--text-muted);
      }
      .kpi__spark {
        margin-top: auto;
        width: 100%;
        height: 1.6rem;
        color: var(--kpi-accent, var(--teal-500));
        opacity: 0.85;
      }
    `,
  ],
})
export class KpiCardComponent {
  readonly label = input.required<string>();
  readonly value = input.required<number>();
  readonly hint = input<string>('');
  readonly compareHint = input<string>('');
  readonly currency = input(true);
  readonly delta = input<number | null>(null);
  readonly trend = input<KpiTrend | null>(null);
  readonly sparkline = input<number[]>([]);
  readonly accent = input<string>('');

  readonly displayValue = computed(() => {
    const v = this.value();
    if (!this.currency()) {
      return new Intl.NumberFormat('en-PK', { maximumFractionDigits: 0 }).format(v);
    }
    return new Intl.NumberFormat('en-PK', {
      style: 'currency',
      currency: 'PKR',
      currencyDisplay: 'code',
      maximumFractionDigits: 0,
    }).format(v);
  });

  readonly showDelta = computed(() => {
    const d = this.delta();
    return d != null && Math.abs(d) >= 0.05;
  });

  readonly deltaClass = computed(() => {
    const t = this.trend() ?? this.inferTrend(this.delta());
    return `kpi__delta--${t}`;
  });

  readonly sparkPoints = computed(() => {
    const pts = this.sparkline();
    if (pts.length < 2) return '';
    const min = Math.min(...pts);
    const max = Math.max(...pts);
    const span = max - min || 1;
    return pts
      .map((v, i) => {
        const x = (i / (pts.length - 1)) * 100;
        const y = 26 - ((v - min) / span) * 24;
        return `${x.toFixed(1)},${y.toFixed(1)}`;
      })
      .join(' ');
  });

  formatDelta(d: number): string {
    return new Intl.NumberFormat('en-PK', { maximumFractionDigits: 1 }).format(d);
  }

  private inferTrend(delta: number | null | undefined): KpiTrend {
    if (delta == null || Math.abs(delta) < 0.05) return 'flat';
    return delta > 0 ? 'up' : 'down';
  }
}
