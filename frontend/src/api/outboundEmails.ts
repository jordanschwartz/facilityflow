import apiClient from './client';
import type { OutboundEmail, OutboundEmailDetail, PagedResult } from '../types';

const API_BASE = import.meta.env.VITE_API_URL ?? '';

export const outboundEmailsApi = {
  list: (serviceRequestId: string, page?: number, pageSize?: number) =>
    apiClient.get<PagedResult<OutboundEmail>>(`/service-requests/${serviceRequestId}/outbound-emails`, {
      params: { page, pageSize },
    }),

  get: (id: string) =>
    apiClient.get<OutboundEmailDetail>(`/outbound-emails/${id}`),

  getAttachmentDownloadUrl: (emailId: string, attachmentId: string) =>
    `${API_BASE}/outbound-emails/${emailId}/attachments/${attachmentId}`,

  resend: (id: string) =>
    apiClient.post(`/outbound-emails/${id}/actions/resend`),

  forward: (id: string, recipientEmail: string, recipientName?: string) =>
    apiClient.post(`/outbound-emails/${id}/actions/forward`, { recipientEmail, recipientName }),
};
