import apiClient from './client';
import type { Proposal, ClientProposal, CreateProposalRequest, UpdateProposalRequest, ProposalVersion } from '../types';

export const proposalsApi = {
  create: (serviceRequestId: string, data: CreateProposalRequest) =>
    apiClient.post<Proposal>(`/service-requests/${serviceRequestId}/proposals`, data),

  getByServiceRequest: (serviceRequestId: string) =>
    apiClient.get<Proposal>(`/service-requests/${serviceRequestId}/proposals`),

  get: (id: string) => apiClient.get<Proposal>(`/proposals/${id}`),

  update: (id: string, data: UpdateProposalRequest) =>
    apiClient.put<Proposal>(`/proposals/${id}`, data),

  send: (id: string) => apiClient.post<Proposal>(`/proposals/${id}/send`),

  generateSummary: (id: string, data: { scopeOfWork: string; notes?: string; jobDescription?: string; additionalContext?: string }) =>
    apiClient.post<{ summary: string }>(`/proposals/${id}/generate-summary`, data),

  getByToken: (token: string) => apiClient.get<ClientProposal>(`/proposals/view/${token}`),

  respond: (id: string, data: { token?: string; decision: string; clientResponse?: string }) =>
    apiClient.post(`/proposals/${id}/respond`, data),

  getVersions: (id: string) => apiClient.get<ProposalVersion[]>(`/proposals/${id}/versions`),
};
