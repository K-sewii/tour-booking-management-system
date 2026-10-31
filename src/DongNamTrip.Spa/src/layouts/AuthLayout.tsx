import type { ReactNode } from 'react';

export function AuthLayout({ children, mode }: { children: ReactNode; mode: 'login' | 'register' }) {
  return <div className={`container section auth-layout auth-page auth-page--${mode}`}>{children}</div>;
}
