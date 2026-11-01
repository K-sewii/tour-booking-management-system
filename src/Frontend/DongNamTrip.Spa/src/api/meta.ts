import { api } from './client';
import type { Meta } from './types';
import { useLoad } from './useLoad';

// Quy tắc, danh mục và giới hạn do Backend công bố; tải một lần cho cả phiên.
let cached: Promise<Meta> | null = null;
export const loadMeta = () => (cached ??= api.get<Meta>('proxy/meta').catch(error => { cached = null; throw error; }));
// Số điện thoại: chỉ chữ số; độ dài tối đa do Backend công bố (bỏ qua khi chưa tải xong, API vẫn kiểm tra).
export const phoneOk = (value: string, meta?: Meta | null) => /^[0-9]+$/.test(value) && (!meta || value.length <= meta.phoneMax);
export const useMeta = () => useLoad('meta', loadMeta);
