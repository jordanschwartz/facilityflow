import apiClient from './client';
import type { ServiceRequest, ServiceRequestSummary, PagedResult, ServiceRequestStatus, VendorInvite, Quote } from '../types';

export const serviceRequestsApi = {
  list: (params?: { status?: string; clientId?: string; search?: string; page?: number; pageSize?: number }) =>
    apiClient.get<PagedResult<ServiceRequestSummary>>('/service-requests', { params }),
  get: (id: string) => apiClient.get<ServiceRequest>(`/service-requests/${id}`),
  create: (data: { title: string; description: string; location: string; category: string; priority: string; clientId: string }) =>
    apiClient.post<ServiceRequest>('/service-requests', data),
  update: (id: string, data: { title: string; description: string; location: string; category: string; priority: string }) =>
    apiClient.put<ServiceRequest>(`/service-requests/${id}`, data),
  updateStatus: (id: string, status: ServiceRequestStatus) =>
    apiClient.patch(`/service-requests/${id}/status`, { status }),
  getInvites: (id: string) => apiClient.get<VendorInvite[]>(`/service-requests/${id}/invites`),
  createInvites: (id: string, vendorIds: string[]) =>
    apiClient.post(`/service-requests/${id}/invites`, { vendorIds }),
  getQuotes: (id: string) => apiClient.get<Quote[]>(`/service-requests/${id}/quotes`),
  getAllowedTransitions: (id: string) => apiClient.get<string[]>(`/service-requests/${id}/allowed-transitions`),
  uploadPo: (id: string, data: { poNumber: string; poAmount?: number; file: File }) => {
    const form = new FormData();
    form.append('poNumber', data.poNumber);
    if (data.poAmount != null) form.append('poAmount', data.poAmount.toString());
    form.append('file', data.file);
    return apiClient.post(`/service-requests/${id}/upload-po`, form);
  },
  updateSchedule: (id: string, scheduledDate: string) =>
    apiClient.patch(`/service-requests/${id}/schedule`, { scheduledDate }),
};
