import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createElement } from 'react';

vi.mock('../../api/auth', () => ({
  authApi: {
    getPermissions: vi.fn(),
  },
}));

import { authApi } from '../../api/auth';
import { usePermissions } from '../usePermissions';

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return ({ children }: { children: React.ReactNode }) =>
    createElement(QueryClientProvider, { client: queryClient }, children);
}

describe('usePermissions', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('hasPermission returns true for FullAccess regardless of specific permission', async () => {
    vi.mocked(authApi.getPermissions).mockResolvedValue({ data: ['FullAccess'] } as never);

    const { result } = renderHook(() => usePermissions(), { wrapper: createWrapper() });

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    expect(result.current.hasPermission('ManageUsers')).toBe(true);
    expect(result.current.hasPermission('ManageSettings')).toBe(true);
    expect(result.current.hasPermission('AnythingElse')).toBe(true);
  });

  it('hasPermission returns true only for granted specific permissions', async () => {
    vi.mocked(authApi.getPermissions).mockResolvedValue({
      data: ['ManageUsers', 'ViewReports'],
    } as never);

    const { result } = renderHook(() => usePermissions(), { wrapper: createWrapper() });

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    expect(result.current.hasPermission('ManageUsers')).toBe(true);
    expect(result.current.hasPermission('ViewReports')).toBe(true);
    expect(result.current.hasPermission('ManageSettings')).toBe(false);
  });

  it('hasPermission returns false when no permissions are loaded', async () => {
    vi.mocked(authApi.getPermissions).mockResolvedValue({ data: [] } as never);

    const { result } = renderHook(() => usePermissions(), { wrapper: createWrapper() });

    await waitFor(() => expect(result.current.isLoading).toBe(false));

    expect(result.current.hasPermission('ManageUsers')).toBe(false);
  });
});
