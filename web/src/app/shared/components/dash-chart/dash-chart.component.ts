import {
  AfterViewInit,
  Component,
  ElementRef,
  OnDestroy,
  effect,
  input,
  viewChild,
} from '@angular/core';
import {
  Chart,
  ChartConfiguration,
  ChartData,
  ChartOptions,
  ChartType,
  registerables,
} from 'chart.js';

Chart.register(...registerables);

@Component({
  selector: 'app-dash-chart',
  standalone: true,
  template: `
    <div class="chart-wrap">
      <canvas #canvas></canvas>
    </div>
  `,
  styles: [
    `
      .chart-wrap {
        position: relative;
        width: 100%;
        height: 100%;
        min-height: 200px;
      }
      canvas {
        width: 100% !important;
        height: 100% !important;
      }
    `,
  ],
})
export class DashChartComponent implements AfterViewInit, OnDestroy {
  readonly type = input<ChartType>('line');
  readonly data = input.required<ChartData>();
  readonly options = input<ChartOptions>({});

  private readonly canvas = viewChild<ElementRef<HTMLCanvasElement>>('canvas');
  private chart: Chart | null = null;
  private ready = false;

  constructor() {
    effect(() => {
      this.type();
      this.data();
      this.options();
      if (this.ready) this.render();
    });
  }

  ngAfterViewInit(): void {
    this.ready = true;
    this.render();
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
    this.chart = null;
  }

  private render(): void {
    const el = this.canvas()?.nativeElement;
    if (!el) return;

    this.chart?.destroy();
    const data = this.data();
    const seriesCount = data.datasets?.length ?? 0;

    // App is light-themed; do not follow OS dark preference for chart ink.
    const tickColor = '#5c7380';
    const gridColor = 'rgba(15, 28, 36, 0.06)';

    const defaults: ChartOptions = {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: seriesCount > 1,
          position: 'bottom',
          labels: {
            boxWidth: 10,
            font: { size: 11 },
            usePointStyle: true,
            pointStyle: 'circle',
            color: tickColor,
          },
        },
        tooltip: { mode: 'index', intersect: false },
      },
      scales:
        this.type() === 'doughnut'
          ? undefined
          : {
              x: {
                grid: { color: gridColor },
                ticks: { font: { size: 10 }, maxRotation: 0, color: tickColor },
              },
              y: {
                beginAtZero: true,
                grid: { color: gridColor },
                ticks: { font: { size: 10 }, color: tickColor },
              },
            },
    };

    const incoming = this.options();
    const merged: ChartOptions = {
      ...defaults,
      ...incoming,
      plugins: {
        ...defaults.plugins,
        ...incoming.plugins,
        legend: {
          ...defaults.plugins?.legend,
          ...incoming.plugins?.legend,
          display: incoming.plugins?.legend?.display ?? seriesCount > 1,
        },
      },
    };

    const config: ChartConfiguration = {
      type: this.type(),
      data,
      options: merged,
    };

    this.chart = new Chart(el, config);
  }
}
