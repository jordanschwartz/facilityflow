import apiClient from './client';
import type { InboundEmail, InboundEmailDetail, PagedResult } from '../types';

const API_BASE = import.meta.env.VITE_API_URL ?? '';

export const inboundEmailsApi = {
  list: (serviceRequestId: string, page?: number, pageSize?: number) =>
    apiClient.get<PagedResult<InboundEmail>>(`/service-requests/${serviceRequestId}/emails`, {
      params: { page, pageSize },
    }),

  get: (id: string) =>
    apiClient.get<InboundEmailDetail>(`/inbound-emails/${id}`),

  getAttachmentDownloadUrl: (emailId: string, attachmentId: string) =>
    `${API_BASE}/inbound-emails/${emailId}/attachments/${attachmentId}`,

  link: (emailId: string, serviceRequestId: string) =>
    apiClient.post(`/inbound-emails/${emailId}/link/${serviceRequestId}`),

  createQuoteFromEmail: (id: string) =>
    apiClient.post<{ quoteId: string }>(`/inbound-emails/${id}/actions/create-quote`),

  attachAsPo: (id: string, attachmentId: string) =>
    apiClient.post(`/inbound-emails/${id}/actions/attach-as-po`, { attachmentId }),

  addToNotes: (id: string) =>
    apiClient.post(`/inbound-emails/${id}/actions/add-to-notes`),
};
