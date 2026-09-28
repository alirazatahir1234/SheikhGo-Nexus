import { Component, input } from '@angular/core';
import { CurrencyPipe } from '@angular/common';

export interface DocumentLineView {
  productName?: string;
  qty: number;
  rate: number;
  amount: number;
}

@Component({
  selector: 'app-document-lines',
  standalone: true,
  imports: [CurrencyPipe],
  template: `
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Product</th>
            <th class="num">Qty</th>
            <th class="num">Rate</th>
            <th class="num">Amount</th>
          </tr>
        </thead>
        <tbody>
          @for (line of lines(); track $index) {
            <tr>
              <td>{{ line.productName || '—' }}</td>
              <td class="num">{{ line.qty }}</td>
              <td class="num">{{ line.rate | currency: 'PKR' : 'symbol' : '1.2-2' }}</td>
              <td class="num">{{ line.amount | currency: 'PKR' : 'symbol' : '1.2-2' }}</td>
            </tr>
          } @empty {
            <tr><td colspan="4" class="empty">No lines.</td></tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class DocumentLinesComponent {
  readonly lines = input<DocumentLineView[]>([]);
}
