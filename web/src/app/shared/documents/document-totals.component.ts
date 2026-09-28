import { Component, input } from '@angular/core';
import { CurrencyPipe } from '@angular/common';

@Component({
  selector: 'app-document-totals',
  standalone: true,
  imports: [CurrencyPipe],
  template: `
    <dl class="doc-totals">
      <div><dt>Subtotal</dt><dd>{{ subTotal() | currency: 'PKR' : 'symbol' : '1.2-2' }}</dd></div>
      @if (discount() != null && discount() !== 0) {
        <div><dt>Discount</dt><dd>{{ discount()! | currency: 'PKR' : 'symbol' : '1.2-2' }}</dd></div>
      }
      @if (tax() != null) {
        <div><dt>Tax</dt><dd>{{ tax()! | currency: 'PKR' : 'symbol' : '1.2-2' }}</dd></div>
      }
      <div class="doc-totals__grand">
        <dt>Grand total</dt>
        <dd>{{ grandTotal() | currency: 'PKR' : 'symbol' : '1.2-2' }}</dd>
      </div>
    </dl>
  `,
  styles: [
    `
      .doc-totals {
        display: grid;
        gap: 0.35rem;
        margin: 1rem 0;
        padding: 0.85rem 1rem;
        border: 1px solid var(--border);
        border-radius: var(--radius-sm);
        max-width: 280px;
        margin-left: auto;
      }
      .doc-totals > div {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        font-size: 0.9rem;
      }
      dt {
        margin: 0;
        color: var(--text-muted);
        font-weight: 500;
      }
      dd {
        margin: 0;
        font-variant-numeric: tabular-nums;
        font-weight: 600;
      }
      .doc-totals__grand {
        border-top: 1px solid var(--border);
        padding-top: 0.4rem;
        margin-top: 0.2rem;
      }
      .doc-totals__grand dd {
        font-size: 1.05rem;
      }
    `,
  ],
})
export class DocumentTotalsComponent {
  readonly subTotal = input.required<number>();
  readonly tax = input<number | undefined>();
  readonly discount = input<number | undefined>();
  readonly grandTotal = input.required<number>();
}
