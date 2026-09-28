import { Component, computed, input, output } from '@angular/core';
import { DocumentActionsComponent, DocumentAction } from './document-actions.component';

/** @deprecated Prefer app-document-actions — kept for compatibility. */
export type ApprovalAction = DocumentAction;

@Component({
  selector: 'app-approval-panel',
  standalone: true,
  imports: [DocumentActionsComponent],
  template: `
    <app-document-actions
      [status]="status()"
      [busy]="busy()"
      [requiresVerification]="requiresVerification()"
      [requiresApproval]="requiresApproval()"
      [allowed]="allowed()"
      (action)="action.emit($event)"
    />
  `,
})
export class ApprovalPanelComponent {
  readonly status = input<string | number>('Draft');
  readonly busy = input(false);
  readonly requiresVerification = input(true);
  readonly requiresApproval = input(true);
  readonly allowed = input<Partial<Record<DocumentAction, boolean>>>({});
  readonly action = output<DocumentAction>();
}
