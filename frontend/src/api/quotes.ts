import apiClient from './client';
import type { Quote } from '../types';

export const quotesApi = {
  submit: (serviceRequestId: string, data: { price: number; scopeOfWork: string }) =>
    apiClient.post<Quote>(`/service-requests/${serviceRequestId}/quotes`, data),
  updateStatus: (id: string, status: string) =>
    apiClient.patch<Quote>(`/quotes/${id}/status`, { status }),
  submitByToken: (token: string, data: { price: number; scopeOfWork: string }) =>
    apiClient.post(`/quotes/submit/${token}`, data),
  getByToken: (token: string) =>
    apiClient.get<{ serviceRequest: { title: string; location: string; category: string }; quote?: Quote }>(`/quotes/submit/${token}`),
};
