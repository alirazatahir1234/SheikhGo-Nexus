import { Injectable, OnDestroy, inject, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './auth.service';

export interface PosNotifyEvent {
  type: string;
  message: string;
  data?: unknown;
  at: number;
}

@Injectable({ providedIn: 'root' })
export class PosNotifyService implements OnDestroy {
  private readonly auth = inject(AuthService);
  private connection: signalR.HubConnection | null = null;
  readonly toast = signal<PosNotifyEvent | null>(null);
  /** Connected | Reconnecting | Disconnected — for a subtle status chip */
  readonly hubStatus = signal<'Connected' | 'Reconnecting' | 'Disconnected'>('Disconnected');
  private hideTimer: ReturnType<typeof setTimeout> | null = null;

  async start(): Promise<void> {
    if (!this.auth.isAuthenticated()) return;
    if (this.connection) return;
    const token = this.auth.getToken();
    if (!token) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/pos-notify', { accessTokenFactory: () => this.auth.getToken() || '' })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    this.connection.on('posNotify', (payload: { type?: string; message?: string; data?: unknown }) => {
      this.show({
        type: payload?.type || 'Info',
        message: payload?.message || 'POS update',
        data: payload?.data,
        at: Date.now(),
      });
    });

    this.connection.onreconnecting(() => {
      this.hubStatus.set('Reconnecting');
      this.show({ type: 'Status', message: 'Live updates reconnecting…', at: Date.now() });
    });
    this.connection.onreconnected(() => {
      this.hubStatus.set('Connected');
      this.show({ type: 'Status', message: 'Live updates restored', at: Date.now() });
    });
    this.connection.onclose(() => {
      this.hubStatus.set('Disconnected');
      this.connection = null;
    });

    try {
      await this.connection.start();
      this.hubStatus.set('Connected');
    } catch {
      this.hubStatus.set('Disconnected');
      this.connection = null;
    }
  }

  dismiss(): void {
    this.toast.set(null);
    if (this.hideTimer) clearTimeout(this.hideTimer);
  }

  private show(ev: PosNotifyEvent): void {
    this.toast.set(ev);
    if (this.hideTimer) clearTimeout(this.hideTimer);
    const ms = ev.type === 'Status' ? 3500 : 6000;
    this.hideTimer = setTimeout(() => this.toast.set(null), ms);
  }

  async stop(): Promise<void> {
    if (!this.connection) return;
    try {
      await this.connection.stop();
    } catch {
      /* ignore */
    }
    this.connection = null;
    this.hubStatus.set('Disconnected');
  }

  ngOnDestroy(): void {
    void this.stop();
  }
}
