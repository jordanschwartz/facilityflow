import apiClient from './client';
import type {
  PagedResult,
  UserListItem,
  UserDetail,
  CreateUserRequest,
  UpdateUserRequest,
  UpdateProfileRequest,
  ChangePasswordRequest,
} from '../types';

export const usersApi = {
  list: (params?: { search?: string; page?: number; pageSize?: number }) =>
    apiClient.get<PagedResult<UserListItem>>('/users', { params }),
  getById: (id: string) => apiClient.get<UserDetail>(`/users/${id}`),
  create: (data: CreateUserRequest) => apiClient.post<UserDetail>('/users', data),
  update: (id: string, data: UpdateUserRequest) => apiClient.put<UserDetail>(`/users/${id}`, data),
  resetPassword: (id: string) => apiClient.post<{ temporaryPassword: string }>(`/users/${id}/reset-password`),
  getProfile: () => apiClient.get<UserDetail>('/users/profile'),
  updateProfile: (data: UpdateProfileRequest) => apiClient.put<UserDetail>('/users/profile', data),
  changePassword: (data: ChangePasswordRequest) => apiClient.put<void>('/users/profile/password', data),
};
