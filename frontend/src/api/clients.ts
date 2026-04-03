import apiClient from './client';
import type { Client, PagedResult } from '../types';

export const clientsApi = {
  list: (params?: { search?: string; page?: number; pageSize?: number }) =>
    apiClient.get<PagedResult<Client>>('/clients', { params }),
  get: (id: string) => apiClient.get<Client>(`/clients/${id}`),
  create: (data: { companyName: string; contactName: string; email: string; phone: string; address: string; workOrderPrefix?: string }) =>
    apiClient.post<Client>('/clients', data),
  update: (id: string, data: { companyName: string; contactName: string; email: string; phone: string; address: string; workOrderPrefix?: string }) =>
    apiClient.put<Client>(`/clients/${id}`, data),
};
