import apiClient from './client';
import type { ActivityLog } from '../types';

export const activityLogsApi = {
  list: (params: { serviceRequestId?: string; workOrderId?: string; category?: string }) =>
    apiClient.get<ActivityLog[]>('/activity-logs', { params }),
};
