import apiClient from './client';
import type { AuthUser } from '../types';

export const authApi = {
  login: (email: string, password: string) =>
    apiClient.post<{ token: string; user: AuthUser }>('/auth/login', { email, password }),
  register: (data: { email: string; password: string; name: string; role: 'Client' | 'Vendor' }) =>
    apiClient.post<{ token: string; user: AuthUser }>('/auth/register', data),
  me: () => apiClient.get<AuthUser>('/auth/me'),
  getPermissions: () => apiClient.get<string[]>('/auth/permissions'),
};
