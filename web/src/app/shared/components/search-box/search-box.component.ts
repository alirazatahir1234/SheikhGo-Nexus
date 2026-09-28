import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-search-box',
  standalone: true,
  imports: [FormsModule],
  template: `
    <label class="search">
      <span class="search__icon" aria-hidden="true">⌕</span>
      <input
        type="search"
        [placeholder]="placeholder()"
        [ngModel]="value()"
        (ngModelChange)="valueChange.emit($event)"
      />
    </label>
  `,
  styles: [
    `
      .search {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        background: var(--surface);
        border: 1px solid var(--border);
        border-radius: var(--radius-sm);
        padding: 0.55rem 0.85rem;
        min-width: min(100%, 280px);
      }
      .search__icon {
        color: var(--text-muted);
        font-size: 1rem;
      }
      input {
        border: 0;
        outline: 0;
        width: 100%;
        background: transparent;
        font: inherit;
        color: var(--text);
      }
    `,
  ],
})
export class SearchBoxComponent {
  readonly placeholder = input('Search…');
  readonly value = input('');
  readonly valueChange = output<string>();
}
