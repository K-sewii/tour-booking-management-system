import { useEffect, useState } from 'react';
import { messageFor } from './client';

export function useLoad<T>(key: string, loader: () => Promise<T>) {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    let active = true;
    setLoading(true);
    setError('');
    loader().then(result => { if (active) setData(result); })
      .catch(caught => { if (active) { setData(null); setError(messageFor(caught)); } })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
    // The caller supplies a stable key for route/filter changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key, attempt]);
  return { data, loading, error, retry: () => setAttempt(value => value + 1), setData };
}
