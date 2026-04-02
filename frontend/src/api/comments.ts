import apiClient from './client';
import type { Comment } from '../types';

export const commentsApi = {
  list: (params: { serviceRequestId?: string; quoteId?: string; workOrderId?: string }) =>
    apiClient.get<Comment[]>('/comments', { params }),
  create: (data: { text: string; serviceRequestId?: string; quoteId?: string; workOrderId?: string; files?: File[] }) => {
    const form = new FormData();
    form.append('text', data.text);
    if (data.serviceRequestId) form.append('serviceRequestId', data.serviceRequestId);
    if (data.quoteId) form.append('quoteId', data.quoteId);
    if (data.workOrderId) form.append('workOrderId', data.workOrderId);
    if (data.files) {
      data.files.forEach(f => form.append('files', f));
    }
    return apiClient.post<Comment>('/comments', form);
  },
};
