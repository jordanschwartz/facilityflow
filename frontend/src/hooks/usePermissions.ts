import { useQuery } from '@tanstack/react-query';
import { authApi } from '../api/auth';

export function usePermissions() {
  const { data, isLoading } = useQuery({
    queryKey: ['permissions'],
    queryFn: () => authApi.getPermissions(),
    staleTime: 5 * 60 * 1000,
  });

  const permissions = data?.data ?? [];
  const hasPermission = (perm: string) =>
    permissions.includes('FullAccess') || permissions.includes(perm);

  return { permissions, hasPermission, isLoading };
}
