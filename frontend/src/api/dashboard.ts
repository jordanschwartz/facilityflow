import apiClient from './client';
import type { PipelineResponse } from '../types';

export const dashboardApi = {
  getPipeline: () => apiClient.get<PipelineResponse>('/dashboard/pipeline'),
};
