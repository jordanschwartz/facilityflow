import apiClient from './client';
import type { Vendor, PagedResult } from '../types';

export const vendorsApi = {
  list: (params?: { trade?: string; zip?: string; search?: string; page?: number; pageSize?: number }) =>
    apiClient.get<PagedResult<Vendor>>('/vendors', { params }),
  get: (id: string) => apiClient.get<Vendor>(`/vendors/${id}`),
  create: (data: { userId: string; companyName: string; phone: string; trades: string[]; zipCodes: string[] }) =>
    apiClient.post<Vendor>('/vendors', data),
  update: (id: string, data: { companyName: string; phone: string; trades: string[]; zipCodes: string[] }) =>
    apiClient.put<Vendor>(`/vendors/${id}`, data),
};
