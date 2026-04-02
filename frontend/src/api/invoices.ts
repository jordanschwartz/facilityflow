import apiClient from './client';
import type { Invoice, InvoiceSummary, BillableWorkOrder, CreateInvoiceRequest, UpdateInvoiceRequest, PagedResult } from '../types';

export const invoicesApi = {
  list: (params?: { status?: string; clientId?: string; location?: string; dateFrom?: string; dateTo?: string; page?: number; pageSize?: number }) =>
    apiClient.get<PagedResult<InvoiceSummary>>('/invoices', { params }),
  billableWorkOrders: (params?: { page?: number; pageSize?: number }) =>
    apiClient.get<PagedResult<BillableWorkOrder>>('/invoices/billable-work-orders', { params }),
  get: (id: string) => apiClient.get<Invoice>(`/invoices/${id}`),
  create: (workOrderId: string, data: CreateInvoiceRequest) =>
    apiClient.post<Invoice>(`/invoices/${workOrderId}`, data),
  update: (id: string, data: UpdateInvoiceRequest) =>
    apiClient.put<Invoice>(`/invoices/${id}`, data),
  send: (id: string) => apiClient.post<Invoice>(`/invoices/${id}/send`),
  cancel: (id: string) => apiClient.post<Invoice>(`/invoices/${id}/cancel`),
};
