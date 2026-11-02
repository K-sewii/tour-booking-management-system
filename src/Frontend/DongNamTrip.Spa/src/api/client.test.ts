import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError, errorFor, messageFor, request } from './client';

const reply = (status: number, body?: unknown) =>
  new Response(body === undefined ? null : JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });

afterEach(() => vi.unstubAllGlobals());

describe('errorFor / messageFor', () => {
  it('tìm lỗi theo tên trường không phân biệt hoa thường', () => {
    const error = new ApiError(400, 'x', { Email: ['Sai định dạng.', 'Đã dùng.'] });
    expect(errorFor(error, 'email')).toBe('Sai định dạng. Đã dùng.');
    expect(errorFor(error, 'name')).toBe('');
    expect(errorFor(new Error('x'), 'email')).toBe('');
  });
  it('messageFor lấy message hoặc dùng thông báo mặc định', () => {
    expect(messageFor(new Error('Lỗi A'))).toBe('Lỗi A');
    expect(messageFor('lạ')).toContain('Chưa xử lý được');
  });
});

describe('request', () => {
  it('trả dữ liệu JSON của GET, không lấy CSRF', async () => {
    const fetchMock = vi.fn().mockResolvedValue(reply(200, { ok: true }));
    vi.stubGlobal('fetch', fetchMock);
    expect(await request('x')).toEqual({ ok: true });
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });
  it('lệnh ghi lấy CSRF trước rồi gửi kèm header', async () => {
    const fetchMock = vi.fn().mockResolvedValueOnce(reply(200, { token: 't1' })).mockResolvedValueOnce(reply(204));
    vi.stubGlobal('fetch', fetchMock);
    await request('x', 'POST', { a: 1 });
    expect(fetchMock.mock.calls[1][1].headers.RequestVerificationToken).toBe('t1');
  });
  it('lỗi 404 dùng thông báo mặc định, lỗi kiểm tra dữ liệu giữ errors', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValueOnce(reply(404)).mockResolvedValueOnce(reply(400, { errors: { Name: ['Thiếu tên.'] } })));
    await expect(request('x')).rejects.toMatchObject({ status: 404, message: 'Không tìm thấy dữ liệu được yêu cầu.' });
    await expect(request('x')).rejects.toMatchObject({ status: 400, errors: { Name: ['Thiếu tên.'] } });
  });
  it('401 phát sự kiện hết phiên, trừ khi là đăng nhập', async () => {
    const target = new EventTarget();
    const listener = vi.fn();
    target.addEventListener('spa:unauthorized', listener);
    vi.stubGlobal('window', target);
    vi.stubGlobal('fetch', vi.fn().mockImplementation(() => Promise.resolve(reply(401))));
    await expect(request('profile')).rejects.toBeInstanceOf(ApiError);
    await expect(request('login')).rejects.toBeInstanceOf(ApiError);
    expect(listener).toHaveBeenCalledTimes(1);
  });
  it('mất kết nối trả ApiError status 0', async () => {
    vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('offline')));
    await expect(request('x')).rejects.toMatchObject({ status: 0 });
  });
});
