import { Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';

@Component({
  selector: 'app-coming-soon',
  standalone: true,
  imports: [PageHeaderComponent, RouterLink],
  template: `
    <app-page-header [title]="title" subtitle="This domain is planned for a later SheikhGo-Nexus-ERP phase." />
    <div class="card" style="padding: 2rem; max-width: 36rem;">
      <p style="color: var(--text-muted); margin: 0 0 1rem;">
        Phase 1 focuses on Core Administration, Document Engine, and Procurement.
        <strong>{{ title }}</strong> will be enabled after the Document Engine is reusable.
      </p>
      <a routerLink="/dashboard" class="btn btn--primary">Back to Dashboard</a>
    </div>
  `,
})
export class ComingSoonComponent {
  private readonly route = inject(ActivatedRoute);
  get title(): string {
    const domain = this.route.snapshot.paramMap.get('domain') ?? 'Module';
    return domain.replace(/-/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());
  }
}
