import apiClient from './client';
import type { WorkOrder, PagedResult } from '../types';

export const workOrdersApi = {
  list: (params?: { status?: string; page?: number; pageSize?: number }) =>
    apiClient.get<PagedResult<WorkOrder>>('/work-orders', { params }),
  get: (id: string) => apiClient.get<WorkOrder>(`/work-orders/${id}`),
  updateStatus: (id: string, data: { status: string; vendorNotes?: string }) =>
    apiClient.patch<WorkOrder>(`/work-orders/${id}/status`, data),
};
