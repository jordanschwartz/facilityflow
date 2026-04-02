import apiClient from './client';
import type { Notification } from '../types';

export const notificationsApi = {
  list: (params?: { unreadOnly?: boolean; page?: number }) =>
    apiClient.get<{ items: Notification[]; unreadCount: number; totalCount: number }>('/notifications', { params }),
  markRead: (id: string) => apiClient.patch(`/notifications/${id}/read`),
  markAllRead: () => apiClient.patch('/notifications/read-all'),
};
