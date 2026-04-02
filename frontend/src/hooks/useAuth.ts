import { useAuthStore } from '../stores/authStore';

export function useAuth() {
  const { token, user, setAuth, clearAuth } = useAuthStore();
  return { token, user, setAuth, clearAuth, isAuthenticated: !!token };
}
