import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react';
import { api, messageFor } from '../api/client';
import type { User } from '../api/types';

type AuthState = {
  user: User | null;
  loading: boolean;
  error: string;
  refresh: () => Promise<void>;
  login: (email: string, password: string) => Promise<void>;
  register: (data: Record<string, unknown>) => Promise<void>;
  logout: () => Promise<void>;
  setUser: (user: User | null) => void;
};

const Context = createContext<AuthState | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const refresh = useCallback(async () => {
    try { setUser((await api.get<{ user: User | null }>('session')).user); setError(''); }
    catch (caught) { setUser(null); setError(messageFor(caught)); }
    finally { setLoading(false); }
  }, []);
  useEffect(() => { void refresh(); }, [refresh]);
  useEffect(() => {
    const expired = () => setUser(null);
    window.addEventListener('spa:unauthorized', expired);
    return () => window.removeEventListener('spa:unauthorized', expired);
  }, []);
  const login = async (email: string, password: string) => {
    const result = await api.post<{ user: User }>('login', { email, password });
    setUser(result.user);
    setError('');
  };
  const register = async (data: Record<string, unknown>) => {
    const result = await api.post<{ user: User }>('register', data);
    setUser(result.user);
    setError('');
  };
  const logout = async () => { try { await api.post<void>('logout'); } finally { setUser(null); } };
  return <Context.Provider value={{ user, loading, error, refresh, login, register, logout, setUser }}>{children}</Context.Provider>;
}

export function useAuth() {
  const value = useContext(Context);
  if (!value) throw new Error('AuthProvider is required');
  return value;
}
