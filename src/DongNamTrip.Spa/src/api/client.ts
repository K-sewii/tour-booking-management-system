export class ApiError extends Error {
  constructor(public status: number, message: string, public errors: Record<string, string[]> = {}) { super(message); }
}

async function csrf(): Promise<string> {
  let response: Response;
  try { response = await fetch('/spa-api/csrf', { credentials: 'same-origin' }); }
  catch { throw new ApiError(0, 'Chưa kết nối được máy chủ. Vui lòng thử lại.'); }
  if (!response.ok) throw new ApiError(response.status, 'Chưa tải được mã bảo vệ biểu mẫu. Vui lòng thử lại.');
  return (await response.json() as { token: string }).token;
}

export async function request<T>(path: string, method = 'GET', body?: unknown): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (method !== 'GET') headers.RequestVerificationToken = await csrf();
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  let response: Response;
  try {
    response = await fetch('/spa-api/' + path, {
      method, headers, body: body === undefined ? undefined : JSON.stringify(body), credentials: 'same-origin',
    });
  } catch {
    throw new ApiError(0, 'Chưa kết nối được máy chủ. Vui lòng thử lại.');
  }
  const raw = await response.text();
  let data: unknown = undefined;
  try { data = raw ? JSON.parse(raw) : undefined; } catch { data = undefined; }
  if (!response.ok) {
    if (response.status === 401 && path !== 'login') window.dispatchEvent(new Event('spa:unauthorized'));
    const payload = (data && typeof data === 'object' ? data : {}) as { message?: string; title?: string; errors?: Record<string, string[]> };
    const fallback = response.status === 401
      ? 'Phiên đã hết hạn. Vui lòng đăng nhập lại.'
      : response.status === 403
        ? 'Bạn không có quyền thực hiện thao tác này.'
        : response.status === 404
          ? 'Không tìm thấy dữ liệu được yêu cầu.'
          : 'Chưa xử lý được yêu cầu. Vui lòng thử lại.';
    throw new ApiError(response.status, payload.message || fallback, payload.errors || {});
  }
  return data as T;
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body?: unknown) => request<T>(path, 'POST', body),
  put: <T>(path: string, body: unknown) => request<T>(path, 'PUT', body),
  delete: <T>(path: string) => request<T>(path, 'DELETE'),
};

export function errorFor(error: unknown, field: string): string {
  if (!(error instanceof ApiError)) return '';
  const found = Object.entries(error.errors).find(([key]) => key.toLowerCase() === field.toLowerCase());
  return found?.[1]?.join(' ') || '';
}

export function messageFor(error: unknown): string {
  return error instanceof Error ? error.message : 'Chưa xử lý được yêu cầu. Vui lòng thử lại.';
}
