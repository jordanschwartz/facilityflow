import apiClient from './client';
import type { EmailConversation } from '../types';

export const emailConversationsApi = {
  getConversations: (serviceRequestId: string) =>
    apiClient.get<EmailConversation[]>(`/service-requests/${serviceRequestId}/email-conversations`),
};
