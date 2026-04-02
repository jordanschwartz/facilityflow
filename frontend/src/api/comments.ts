import apiClient from './client';
import type { Comment } from '../types';

export const commentsApi = {
  list: (params: { serviceRequestId?: string; quoteId?: string; workOrderId?: string }) =>
    apiClient.get<Comment[]>('/comments', { params }),
  create: (data: { text: string; serviceRequestId?: string; quoteId?: string; workOrderId?: string }) =>
    apiClient.post<Comment>('/comments', data),
};
