import { create } from 'zustand';
import type { AuthUser } from '../types';

interface AuthState {
  token: string | null;
  user: AuthUser | null;
  setAuth: (token: string, user: AuthUser) => void;
  clearAuth: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  token: localStorage.getItem('ff_token'),
  user: null,
  setAuth: (token, user) => {
    localStorage.setItem('ff_token', token);
    set({ token, user });
  },
  clearAuth: () => {
    localStorage.removeItem('ff_token');
    set({ token: null, user: null });
  },
}));
