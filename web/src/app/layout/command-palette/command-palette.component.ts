import {
  Component,
  HostListener,
  inject,
  signal,
  computed,
  ElementRef,
  viewChild,
  effect,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { debounceTime, distinctUntilChanged, Subject, switchMap, of, catchError, map, forkJoin } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavService } from '../../core/nav/nav.service';
import { NavIconComponent } from '../../core/nav/nav-icon.component';
import { API_BASE_URL } from '../../core/models/api.constants';

interface PaletteHit {
  group: string;
  label: string;
  route: string;
  hint?: string;
}

@Component({
  selector: 'app-command-palette',
  standalone: true,
  imports: [FormsModule, NavIconComponent],
  template: `
    @if (open()) {
      <div class="palette-backdrop" (click)="close()"></div>
      <div class="palette" role="dialog" aria-label="Search Nexus" (click)="$event.stopPropagation()">
        <div class="palette__input-row">
          <app-nav-icon name="search" [size]="18" />
          <input
            #queryInput
            type="search"
            placeholder="Search Nexus…"
            [ngModel]="query()"
            (ngModelChange)="onQuery($event)"
            (keydown)="onKey($event)"
          />
          <kbd>esc</kbd>
        </div>
        <div class="palette__results">
          @for (group of grouped(); track group.name) {
            <div class="palette__group">
              <div class="palette__group-title">{{ group.name }}</div>
              @for (hit of group.items; track hit.route + hit.label; let i = $index) {
                <button
                  type="button"
                  class="palette__hit"
                  [class.is-active]="flatIndex(group.name, i) === activeIndex()"
                  (click)="go(hit)"
                  (mouseenter)="activeIndex.set(flatIndex(group.name, i))"
                >
                  <span>{{ hit.label }}</span>
                  @if (hit.hint) {
                    <small>{{ hit.hint }}</small>
                  }
                </button>
              }
            </div>
          } @empty {
            <p class="palette__empty">No matches</p>
          }
        </div>
      </div>
    }
  `,
  styles: [
    `
      .palette-backdrop {
        position: fixed;
        inset: 0;
        background: rgba(15, 23, 42, 0.45);
        z-index: 100;
      }
      .palette {
        position: fixed;
        top: 12vh;
        left: 50%;
        transform: translateX(-50%);
        width: min(560px, 92vw);
        background: #fff;
        border-radius: 12px;
        box-shadow: 0 24px 80px rgba(15, 23, 42, 0.28);
        z-index: 101;
        overflow: hidden;
        border: 1px solid var(--border);
      }
      .palette__input-row {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        padding: 0.9rem 1rem;
        border-bottom: 1px solid var(--border);
      }
      .palette__input-row input {
        flex: 1;
        border: none;
        outline: none;
        font-size: 1rem;
        background: transparent;
      }
      .palette__input-row kbd {
        font-size: 0.7rem;
        color: var(--text-muted);
        border: 1px solid var(--border);
        border-radius: 4px;
        padding: 0.15rem 0.4rem;
      }
      .palette__results {
        max-height: 50vh;
        overflow-y: auto;
        padding: 0.5rem;
      }
      .palette__group-title {
        font-size: 0.65rem;
        letter-spacing: 0.08em;
        text-transform: uppercase;
        color: var(--text-muted);
        padding: 0.5rem 0.65rem 0.25rem;
      }
      .palette__hit {
        display: flex;
        flex-direction: column;
        align-items: flex-start;
        gap: 0.15rem;
        width: 100%;
        border: none;
        background: transparent;
        padding: 0.55rem 0.65rem;
        border-radius: 8px;
        cursor: pointer;
        text-align: left;
        font-family: inherit;
        font-size: 0.9rem;
      }
      .palette__hit small {
        color: var(--text-muted);
        font-size: 0.75rem;
      }
      .palette__hit.is-active,
      .palette__hit:hover {
        background: rgba(20, 184, 166, 0.12);
      }
      .palette__empty {
        padding: 1.5rem;
        text-align: center;
        color: var(--text-muted);
      }
    `,
  ],
})
export class CommandPaletteComponent {
  private readonly nav = inject(NavService);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);
  private readonly queryInput = viewChild<ElementRef<HTMLInputElement>>('queryInput');
  private readonly query$ = new Subject<string>();

  readonly open = signal(false);
  readonly query = signal('');
  readonly activeIndex = signal(0);
  readonly entityHits = signal<PaletteHit[]>([]);

  readonly navHits = computed(() => {
    const q = this.query().trim().toLowerCase();
    const leaves = this.nav.visibleLeaves();
    return leaves
      .filter((l) => !q || l.label.toLowerCase().includes(q) || (l.route ?? '').toLowerCase().includes(q))
      .slice(0, 12)
      .map(
        (l): PaletteHit => ({
          group: 'Navigation',
          label: l.label,
          route: l.route!,
          hint: l.route,
        }),
      );
  });

  readonly grouped = computed(() => {
    const groups = new Map<string, PaletteHit[]>();
    for (const hit of [...this.navHits(), ...this.entityHits()]) {
      const list = groups.get(hit.group) ?? [];
      list.push(hit);
      groups.set(hit.group, list);
    }
    return [...groups.entries()].map(([name, items]) => ({ name, items }));
  });

  readonly flatHits = computed(() => this.grouped().flatMap((g) => g.items));

  constructor() {
    this.query$
      .pipe(
        debounceTime(220),
        distinctUntilChanged(),
        switchMap((q) => this.searchEntities(q)),
        takeUntilDestroyed(),
      )
      .subscribe((hits) => {
        this.entityHits.set(hits);
        this.activeIndex.set(0);
      });

    effect(() => {
      if (this.open()) {
        setTimeout(() => this.queryInput()?.nativeElement.focus(), 0);
      }
    });
  }

  @HostListener('document:keydown', ['$event'])
  onGlobalKey(ev: KeyboardEvent): void {
    if ((ev.metaKey || ev.ctrlKey) && ev.key.toLowerCase() === 'k') {
      ev.preventDefault();
      if (this.open()) this.close();
      else this.openPalette();
    } else if (ev.key === 'Escape' && this.open()) {
      this.close();
    }
  }

  onQuery(value: string): void {
    this.query.set(value);
    this.query$.next(value);
  }

  onKey(ev: KeyboardEvent): void {
    const hits = this.flatHits();
    if (ev.key === 'ArrowDown') {
      ev.preventDefault();
      this.activeIndex.set(Math.min(this.activeIndex() + 1, Math.max(hits.length - 1, 0)));
    } else if (ev.key === 'ArrowUp') {
      ev.preventDefault();
      this.activeIndex.set(Math.max(this.activeIndex() - 1, 0));
    } else if (ev.key === 'Enter') {
      ev.preventDefault();
      const hit = hits[this.activeIndex()];
      if (hit) this.go(hit);
    }
  }

  flatIndex(groupName: string, indexInGroup: number): number {
    let n = 0;
    for (const g of this.grouped()) {
      if (g.name === groupName) return n + indexInGroup;
      n += g.items.length;
    }
    return indexInGroup;
  }

  go(hit: PaletteHit): void {
    this.close();
    void this.router.navigateByUrl(hit.route);
  }

  openPalette(): void {
    this.query.set('');
    this.entityHits.set([]);
    this.activeIndex.set(0);
    this.open.set(true);
  }

  close(): void {
    this.open.set(false);
  }

  private searchEntities(q: string) {
    const term = q.trim();
    if (term.length < 2) return of([] as PaletteHit[]);

    const parties$ = this.http.get<{ id: string; name: string; code?: string; partyType?: string }[]>(`${API_BASE_URL}/parties`).pipe(
      map((rows) =>
        (rows ?? [])
          .filter((p) => `${p.name} ${p.code ?? ''}`.toLowerCase().includes(term.toLowerCase()))
          .slice(0, 5)
          .map(
            (p): PaletteHit => ({
              group: p.partyType === 'Supplier' ? 'Suppliers' : 'Customers',
              label: p.name,
              route:
                p.partyType === 'Supplier'
                  ? `/purchase/orders?q=${encodeURIComponent(p.name)}`
                  : `/sales/customers?q=${encodeURIComponent(p.name)}`,
              hint: p.code,
            }),
          ),
      ),
      catchError(() => of([] as PaletteHit[])),
    );

    const products$ = this.http.get<{ id: string; name: string; code: string }[]>(`${API_BASE_URL}/products`).pipe(
      map((rows) =>
        (rows ?? [])
          .filter((p) => `${p.name} ${p.code}`.toLowerCase().includes(term.toLowerCase()))
          .slice(0, 5)
          .map(
            (p): PaletteHit => ({
              group: 'Products',
              label: `${p.code} — ${p.name}`,
              route: `/inventory/stock?q=${encodeURIComponent(p.code)}`,
              hint: p.code,
            }),
          ),
      ),
      catchError(() => of([] as PaletteHit[])),
    );

    const accounts$ = this.http.get<{ id: string; name: string; code: string }[]>(`${API_BASE_URL}/accounts`).pipe(
      map((rows) =>
        (rows ?? [])
          .filter((a) => `${a.name} ${a.code}`.toLowerCase().includes(term.toLowerCase()))
          .slice(0, 5)
          .map(
            (a): PaletteHit => ({
              group: 'Accounts',
              label: `${a.code} — ${a.name}`,
              route: `/finance/chart-of-accounts?q=${encodeURIComponent(a.code)}`,
              hint: a.code,
            }),
          ),
      ),
      catchError(() => of([] as PaletteHit[])),
    );

    return forkJoin([parties$, products$, accounts$]).pipe(map((parts) => parts.flat()));
  }
}
