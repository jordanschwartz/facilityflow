import apiClient from './client';
import type { Proposal } from '../types';

export const proposalsApi = {
  create: (serviceRequestId: string, data: { quoteId: string; price: number; scopeOfWork: string }) =>
    apiClient.post<Proposal>(`/service-requests/${serviceRequestId}/proposals`, data),
  getByServiceRequest: (serviceRequestId: string) =>
    apiClient.get<Proposal>(`/service-requests/${serviceRequestId}/proposals`),
  get: (id: string) => apiClient.get<Proposal>(`/proposals/${id}`),
  update: (id: string, data: { price: number; scopeOfWork: string }) =>
    apiClient.put<Proposal>(`/proposals/${id}`, data),
  send: (id: string) => apiClient.post(`/proposals/${id}/send`),
  respond: (id: string, data: { token?: string; decision: string; clientResponse?: string }) =>
    apiClient.post(`/proposals/${id}/respond`, data),
  getByToken: (token: string) => apiClient.get<Proposal>(`/proposals/view/${token}`),
};
