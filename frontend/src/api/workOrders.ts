import apiClient from './client';
import type { WorkOrder, PagedResult, AttachmentDto } from '../types';

export const workOrdersApi = {
  list: (params?: { status?: string; page?: number; pageSize?: number }) =>
    apiClient.get<PagedResult<WorkOrder>>('/work-orders', { params }),
  get: (id: string) => apiClient.get<WorkOrder>(`/work-orders/${id}`),
  updateStatus: (id: string, data: { status: string; vendorNotes?: string }) =>
    apiClient.patch<WorkOrder>(`/work-orders/${id}/status`, data),
  uploadAttachment: (id: string, file: File) => {
    const form = new FormData();
    form.append('file', file);
    return apiClient.post<AttachmentDto>(`/work-orders/${id}/attachments`, form);
  },
  deleteAttachment: (id: string, attachmentId: string) =>
    apiClient.delete(`/work-orders/${id}/attachments/${attachmentId}`),
};
