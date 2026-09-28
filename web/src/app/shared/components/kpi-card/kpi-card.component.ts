import { Component, computed, input } from '@angular/core';
import { CurrencyPipe, DecimalPipe } from '@angular/common';

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  imports: [CurrencyPipe, DecimalPipe],
  template: `
    <article class="kpi">
      <p class="kpi__label">{{ label() }}</p>
      <p class="kpi__value">
        @if (currency()) {
          {{ value() | currency: 'PKR' : 'symbol' : '1.0-0' }}
        } @else {
          {{ value() | number: '1.0-0' }}
        }
      </p>
      @if (hint()) {
        <p class="kpi__hint">{{ hint() }}</p>
      }
    </article>
  `,
  styles: [
    `
      .kpi {
        background: var(--surface);
        border: 1px solid var(--border);
        border-radius: var(--radius);
        padding: 1.25rem 1.35rem;
        box-shadow: var(--shadow-sm);
      }
      .kpi__label {
        margin: 0;
        font-size: 0.8rem;
        font-weight: 600;
        letter-spacing: 0.04em;
        text-transform: uppercase;
        color: var(--text-muted);
      }
      .kpi__value {
        margin: 0.55rem 0 0;
        font-family: var(--font-display);
        font-size: 1.65rem;
        font-weight: 700;
        color: var(--text);
        letter-spacing: -0.02em;
      }
      .kpi__hint {
        margin: 0.4rem 0 0;
        font-size: 0.8rem;
        color: var(--teal-600);
      }
    `,
  ],
})
export class KpiCardComponent {
  readonly label = input.required<string>();
  readonly value = input.required<number>();
  readonly hint = input<string>('');
  readonly currency = input(true);
}
