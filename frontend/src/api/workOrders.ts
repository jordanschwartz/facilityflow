import apiClient from './client';
import type { WorkOrder, WorkOrderViewDto, PagedResult, AttachmentDto } from '../types';

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

  // Vendor invitation & work order dispatch
  sendWorkOrder: (serviceRequestId: string, vendorInviteId: string) =>
    apiClient.post(`/work-orders/${serviceRequestId}/send`, { vendorInviteId }),
  previewWorkOrderPdf: (serviceRequestId: string, vendorInviteId: string) =>
    apiClient.get(`/work-orders/${serviceRequestId}/preview-pdf`, {
      params: { vendorInviteId },
      responseType: 'blob',
    }),

  // Public endpoints (no auth needed — interceptor adds token if present, that's fine)
  getWorkOrderByToken: (token: string) =>
    apiClient.get<WorkOrderViewDto>(`/work-orders/view/${token}`),
  downloadWorkOrderPdf: (token: string) =>
    apiClient.get(`/work-orders/view/${token}/pdf`, { responseType: 'blob' }),
};
