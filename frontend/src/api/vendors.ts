import apiClient from './client';
import type { Vendor, PagedResult, VendorNote, VendorPayment, VendorSourcingResult, DiscoveredVendor } from '../types';

export const vendorsApi = {
  list: (params?: { trade?: string; zip?: string; search?: string; page?: number; pageSize?: number; activeOnly?: boolean; hideDnu?: boolean }) =>
    apiClient.get<PagedResult<Vendor>>('/vendors', { params }),
  get: (id: string) => apiClient.get<Vendor>(`/vendors/${id}`),
  create: (data: {
    companyName: string;
    primaryContactName: string;
    email: string;
    phone?: string;
    primaryZip: string;
    serviceRadiusMiles: number;
    trades: string[];
    isActive: boolean;
  }) => apiClient.post<Vendor>('/vendors', data),
  update: (id: string, data: {
    companyName: string;
    primaryContactName: string;
    email: string;
    phone?: string;
    primaryZip: string;
    serviceRadiusMiles: number;
    trades: string[];
    isActive: boolean;
  }) => apiClient.put<Vendor>(`/vendors/${id}`, data),

  toggleDnu: (id: string, isDnu: boolean, reason?: string) =>
    apiClient.patch<Vendor>(`/vendors/${id}/dnu`, { isDnu, reason }),

  // Notes
  getVendorNotes: (id: string) =>
    apiClient.get<VendorNote[]>(`/vendors/${id}/notes`),
  createVendorNote: (id: string, text: string, attachmentUrl?: string, attachmentFilename?: string) =>
    apiClient.post<VendorNote>(`/vendors/${id}/notes`, { text, attachmentUrl, attachmentFilename }),
  deleteVendorNote: (vendorId: string, noteId: string) =>
    apiClient.delete(`/vendors/${vendorId}/notes/${noteId}`),

  // Payments
  getVendorPayments: (id: string) =>
    apiClient.get<VendorPayment[]>(`/vendors/${id}/payments`),
  createVendorPayment: (id: string, data: { workOrderId?: string; amount: number; status: 'Pending' | 'Paid'; paidAt?: string; notes?: string }) =>
    apiClient.post<VendorPayment>(`/vendors/${id}/payments`, data),
  updateVendorPayment: (vendorId: string, paymentId: string, data: { status: 'Pending' | 'Paid'; paidAt?: string; notes?: string }) =>
    apiClient.put<VendorPayment>(`/vendors/${vendorId}/payments/${paymentId}`, data),

  // Sourcing
  getNearbyVendors: (zip: string, radiusMiles?: number, trade?: string, search?: string) =>
    apiClient.get<VendorSourcingResult[]>('/vendors/nearby', { params: { zip, radiusMiles, trade, search } }),

  // Discovery
  discover: (params: { trade: string; zip: string; radiusMiles?: number }) =>
    apiClient.get<DiscoveredVendor[]>('/vendors/discover', { params }),
  addProspect: (data: { companyName: string; primaryContactName?: string; email?: string; phone?: string; primaryZip: string; website?: string; rating?: number; reviewCount?: number; googleProfileUrl?: string; trades?: string[] }) =>
    apiClient.post<Vendor>('/vendors/prospects', data),
  promote: (id: string) =>
    apiClient.post<Vendor>(`/vendors/${id}/promote`),
};
