import apiClient from './client';
import type { AttachmentDto, Quote } from '../types';

export const quotesApi = {
  submit: (serviceRequestId: string, data: { price: number; scopeOfWork: string }) =>
    apiClient.post<Quote>(`/service-requests/${serviceRequestId}/quotes`, data),
  updateStatus: (id: string, status: string) =>
    apiClient.patch<Quote>(`/quotes/${id}/status`, { status }),
  submitByToken: (token: string, data: {
    price: number;
    scopeOfWork: string;
    proposedStartDate?: string;
    estimatedDurationValue?: number;
    estimatedDurationUnit?: string;
    notToExceedPrice?: number;
    assumptions?: string;
    exclusions?: string;
    vendorAvailability?: string;
    validUntil?: string;
    lineItems?: { description: string; quantity: number; unitPrice: number }[];
  }) =>
    apiClient.post(`/quotes/submit/${token}`, data),
  getByToken: (token: string) =>
    apiClient.get<{ serviceRequest: { title: string; location: string; category: string }; quote?: Quote }>(`/quotes/submit/${token}`),
  uploadAttachment: (token: string, file: File) => {
    const form = new FormData();
    form.append('file', file);
    return apiClient.post<AttachmentDto>(`/quotes/submit/${token}/attachments`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },
  deleteAttachment: (token: string, attachmentId: string) =>
    apiClient.delete(`/quotes/submit/${token}/attachments/${attachmentId}`),
};
