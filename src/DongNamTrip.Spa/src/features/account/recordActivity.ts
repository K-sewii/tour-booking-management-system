import { api } from '../../api/client';

export function recordActivity(kind: 'search' | 'tour' | 'destination', itemId?: number, query?: string) {
  void api.post<void>('proxy/account/activity', { kind, itemId: itemId ?? null, query: query ?? null }).catch(() => {});
}
