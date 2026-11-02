import { api } from '../../api/client';
import type { Activity } from '../../api/types';

export function recordActivity(kind: Activity['kind'], itemId?: number, query?: string) {
  void api.post<void>('proxy/account/activity', { kind, itemId: itemId ?? null, query: query ?? null }).catch(() => {});
}
