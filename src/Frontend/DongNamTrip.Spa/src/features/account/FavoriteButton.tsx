import { useEffect, useState } from 'react';
import { api, messageFor } from '../../api/client';
import type { Collections, ItemKind } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';

export function FavoriteButton({ kind, id }: { kind: ItemKind; id: number }) {
  const { user } = useAuth();
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const userId = user?.id;
  useEffect(() => {
    if (!userId) return;
    let active = true;
    api.get<Collections>('proxy/account/collections').then(items => { if (active) setSaved(items.favorites.some(item => item.kind === kind && item.itemId === id)); })
      .catch(caught => { if (active) setError(messageFor(caught)); });
    return () => { active = false; };
  }, [kind, id, userId]);
  if (!user) return null;
  async function toggle() {
    setBusy(true); setError('');
    try { await (saved ? api.delete(`proxy/account/favorites/${kind}/${id}`) : api.put(`proxy/account/favorites/${kind}/${id}`, {})); setSaved(!saved); }
    catch (caught) { setError(messageFor(caught)); }
    finally { setBusy(false); }
  }
  return <div className="spa-favorite"><button className="btn outline" onClick={() => void toggle()} disabled={busy}>{busy ? 'Đang xử lý…' : saved ? `Bỏ lưu ${kind === 'tour' ? 'tour' : 'địa điểm'}` : `Lưu ${kind === 'tour' ? 'tour' : 'địa điểm'} yêu thích`}</button>{error && <p role="alert">{error}</p>}</div>;
}

