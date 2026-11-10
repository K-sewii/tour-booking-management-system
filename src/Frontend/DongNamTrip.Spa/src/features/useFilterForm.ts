import { useEffect, useState, type FormEvent } from 'react';
import { useSearchParams } from 'react-router';

/** Bộ lọc đồng bộ với query string. `keys` phải là hằng cố định ở ngoài component. */
export function useFilterForm<K extends string>(keys: readonly K[]) {
  const [params, setParams] = useSearchParams();
  const applied = Object.fromEntries(keys.map(key => [key, (params.get(key) || '').trim()])) as Record<K, string>;
  const [form, setForm] = useState(applied);
  useEffect(() => { setForm(Object.fromEntries(keys.map(key => [key, (params.get(key) || '').trim()])) as Record<K, string>); }, [params, keys]);
  const page = Math.max(1, Number(params.get('page') || 1) || 1);
  function submit(event: FormEvent) {
    event.preventDefault();
    const next = new URLSearchParams();
    keys.forEach(key => { if (form[key].trim()) next.set(key, form[key].trim()); });
    setParams(next);
  }
  return { params, applied, form, setForm, page, submit };
}
