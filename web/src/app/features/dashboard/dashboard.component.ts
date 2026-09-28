import { Component, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { DashboardService } from '../../core/services/dashboard.service';
import { DashboardSummary } from '../../core/models/erp.models';
import { PageHeaderComponent } from '../../shared/components/page-header/page-header.component';
import { KpiCardComponent } from '../../shared/components/kpi-card/kpi-card.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [PageHeaderComponent, KpiCardComponent, CurrencyPipe, DatePipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  readonly loading = signal(true);
  readonly error = signal('');
  readonly summary = signal<DashboardSummary | null>(null);

  constructor(private readonly dashboard: DashboardService) {}

  ngOnInit(): void {
    this.dashboard.getSummary().subscribe({
      next: (data) => {
        this.summary.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.summary.set({
          salesToday: 0,
          purchasesToday: 0,
          inventoryValue: 0,
          receivables: 0,
          payables: 0,
          recentVouchers: [],
          pendingApprovals: [],
          recentDocuments: [],
        });
        this.error.set('Unable to load live dashboard data. Showing empty state until API is available.');
        this.loading.set(false);
      },
    });
  }
}
