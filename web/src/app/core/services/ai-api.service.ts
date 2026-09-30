import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_BASE_URL } from '../models/api.constants';

export interface AiChatSource {
  tool: string;
  label: string;
}

export interface AiActionField {
  key: string;
  label: string;
  value?: string | null;
  editable?: boolean;
  inputType?: string | null;
}

export interface AiActionPreview {
  id: string;
  actionType: string;
  status: string;
  permissionRequired: string;
  summary: string;
  fields: AiActionField[];
  validationErrors: string[];
  resultDocumentId?: string | null;
  resultDocumentNo?: string | null;
}

export interface AiAgentStep {
  id: string;
  stepNo: number;
  toolName: string;
  status: string;
  requiresConfirmation: boolean;
  linkedPendingActionId?: string | null;
  summary?: string | null;
  executedAt?: string | null;
}

export interface AiAgentProposedPo {
  pendingActionId?: string | null;
  supplierId: string;
  supplierName: string;
  lines: string[];
  totalQty: number;
}

export interface AiAgentRun {
  id: string;
  agentType: string;
  goal: string;
  status: string;
  summary: string;
  steps: AiAgentStep[];
  proposedOrders: AiAgentProposedPo[];
  pendingActions: AiActionPreview[];
  startedAt: string;
  completedAt?: string | null;
  failureReason?: string | null;
}

export interface AiChatResponse {
  conversationId: string;
  intent: string;
  answer: string;
  sources: AiChatSource[];
  denied?: boolean;
  deniedPermission?: string | null;
  kind?: string;
  action?: AiActionPreview | null;
  agent?: AiAgentRun | null;
}

export interface AiActionConfirmResponse {
  actionId: string;
  status: string;
  documentId?: string | null;
  documentNo?: string | null;
  message: string;
}

export interface AiAgentConfirmResponse {
  agentRunId: string;
  status: string;
  results: AiActionConfirmResponse[];
  message: string;
}

export interface AiConversationListItem {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
}

export interface AiMessage {
  id: string;
  role: string;
  content: string;
  intent?: string | null;
  createdAt: string;
  kind?: string;
  action?: AiActionPreview | null;
  agent?: AiAgentRun | null;
}

export interface AiConversationDetail {
  id: string;
  title: string;
  messages: AiMessage[];
}

@Injectable({ providedIn: 'root' })
export class AiApiService {
  private readonly url = `${API_BASE_URL}/ai`;

  constructor(private readonly http: HttpClient) {}

  chat(message: string, conversationId?: string | null): Observable<AiChatResponse> {
    return this.http.post<AiChatResponse>(`${this.url}/chat`, {
      message,
      conversationId: conversationId || null,
    });
  }

  conversations(): Observable<AiConversationListItem[]> {
    return this.http.get<AiConversationListItem[]>(`${this.url}/conversations`);
  }

  conversation(id: string): Observable<AiConversationDetail> {
    return this.http.get<AiConversationDetail>(`${this.url}/conversations/${id}`);
  }

  getAction(id: string): Observable<AiActionPreview> {
    return this.http.get<AiActionPreview>(`${this.url}/actions/${id}`);
  }

  updateAction(id: string, body: { qty?: number; rate?: number; amount?: number }): Observable<AiActionPreview> {
    return this.http.patch<AiActionPreview>(`${this.url}/actions/${id}`, body);
  }

  confirmAction(id: string, body?: { companyId?: string; branchId?: string }): Observable<AiActionConfirmResponse> {
    return this.http.post<AiActionConfirmResponse>(`${this.url}/actions/${id}/confirm`, body ?? {});
  }

  cancelAction(id: string): Observable<AiActionPreview> {
    return this.http.post<AiActionPreview>(`${this.url}/actions/${id}/cancel`, {});
  }

  getAgent(id: string): Observable<AiAgentRun> {
    return this.http.get<AiAgentRun>(`${this.url}/agents/${id}`);
  }

  confirmAgent(id: string, body?: { companyId?: string; branchId?: string }): Observable<AiAgentConfirmResponse> {
    return this.http.post<AiAgentConfirmResponse>(`${this.url}/agents/${id}/confirm`, body ?? {});
  }

  cancelAgent(id: string): Observable<AiAgentRun> {
    return this.http.post<AiAgentRun>(`${this.url}/agents/${id}/cancel`, {});
  }
}
