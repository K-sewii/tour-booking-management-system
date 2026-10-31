import type { ReactNode } from 'react';

export function PageState({ children, kind = 'error' }: { children: ReactNode; kind?: 'error' | 'success' }) {
  return <div className={`account-message account-message--${kind}`} role={kind === 'error' ? 'alert' : 'status'}>{children}</div>;
}
