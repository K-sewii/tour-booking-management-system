import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router';
import { api, ApiError, messageFor } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Session } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { dateTime, Failure, Loading } from '../../components/Catalog';
import { PageState } from '../../components/PageState';
import { AccountLayout } from '../../layouts/AccountLayout';

export function SecurityPage() {
  const navigate = useNavigate();
  const auth = useAuth();
  const sessions = useLoad('sessions-' + auth.user?.id, () => api.get<Session[]>('proxy/auth/sessions'));
  const [form, setForm] = useState({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
  const [error, setError] = useState('');
  const [status, setStatus] = useState('');
  const [busy, setBusy] = useState(false);
  async function submit(event: FormEvent) {
    event.preventDefault(); setError(''); setStatus('');
    if (!form.currentPassword || form.newPassword.length < 8 || form.newPassword.length > 128 || !/[\p{L}]/u.test(form.newPassword) || !/[0-9]/.test(form.newPassword) || form.newPassword === form.currentPassword || form.newPassword !== form.confirmNewPassword) {
      setError('Kiểm tra mật khẩu hiện tại, mật khẩu mới từ 8 đến 128 ký tự có chữ và số, và xác nhận khớp.'); return;
    }
    setBusy(true);
    try {
      await api.post<void>('proxy/auth/password', form);
      auth.setUser(null);
      navigate('/Account/Login?returnUrl=%2FAccount%2FSecurity', { replace: true, state: 'Đã đổi mật khẩu. Tất cả phiên đăng nhập đã kết thúc; vui lòng đăng nhập lại.' });
    } catch (caught) {
      setError(caught instanceof ApiError && caught.status === 422 ? 'Mật khẩu hiện tại không đúng.' : messageFor(caught));
      setForm({ currentPassword: '', newPassword: '', confirmNewPassword: '' });
    } finally { setBusy(false); }
  }
  async function revoke(session: Session) {
    setError(''); setStatus('');
    try {
      await api.delete<void>('proxy/auth/sessions/' + session.id);
      if (session.isCurrent) { auth.setUser(null); navigate('/Account/Login?returnUrl=%2FAccount%2FSecurity', { replace: true }); }
      else { setStatus('Đã kết thúc phiên đã chọn.'); sessions.retry(); }
    } catch (caught) { setError(messageFor(caught)); }
  }
  return <AccountLayout><div className="account-heading"><div className="eyebrow">TÀI KHOẢN</div><h1 id="account-title">Bảo mật tài khoản</h1><p>Đổi mật khẩu và quản lý các phiên đăng nhập đang hoạt động.</p></div>
    {status && <PageState kind="success">{status}</PageState>}{error && <PageState>{error}</PageState>}
    <div className="account-card"><div className="account-card-heading"><h2>Đổi mật khẩu</h2><p>Sau khi lưu, mọi phiên đăng nhập sẽ kết thúc.</p></div><form onSubmit={event => void submit(event)} noValidate>
      <div className="account-fields"><div className="account-field"><label htmlFor="current-password">Mật khẩu hiện tại <span className="account-required">*</span></label><input id="current-password" type="password" autoComplete="current-password" value={form.currentPassword} onChange={event => setForm({ ...form, currentPassword: event.target.value })}/></div>
        <div className="account-field"><label htmlFor="new-password">Mật khẩu mới <span className="account-required">*</span></label><input id="new-password" type="password" autoComplete="new-password" value={form.newPassword} onChange={event => setForm({ ...form, newPassword: event.target.value })}/><span className="account-hint">Từ 8 đến 128 ký tự, có ít nhất một chữ và một số.</span></div>
        <div className="account-field"><label htmlFor="confirm-password">Xác nhận mật khẩu mới <span className="account-required">*</span></label><input id="confirm-password" type="password" autoComplete="new-password" value={form.confirmNewPassword} onChange={event => setForm({ ...form, confirmNewPassword: event.target.value })}/></div></div>
      <div className="account-actions"><button className="btn" type="submit" disabled={busy}>{busy ? 'Đang đổi mật khẩu…' : 'Đổi mật khẩu'}</button></div></form></div>
    <div className="account-card account-secondary"><div className="account-card-heading"><h2>Phiên đăng nhập đang hoạt động</h2><p>Chỉ hiển thị thời điểm và hạn của phiên do API ghi nhận. Phiên sẽ mất khi API khởi động lại.</p></div>
      {sessions.loading ? <Loading/> : sessions.error ? <Failure message={sessions.error} retry={sessions.retry}/> : !sessions.data?.length ? <p className="account-empty">Không có phiên đang hoạt động.</p> : <ul className="account-list">{sessions.data.map(session => <li key={session.id}><div><strong>{session.isCurrent ? 'Phiên hiện tại' : 'Phiên khác'}</strong><p>Bắt đầu {dateTime(session.createdAt)} · Hết hạn {dateTime(session.expiresAt)}</p></div><button className="btn outline" onClick={() => void revoke(session)}>Kết thúc phiên</button></li>)}</ul>}</div>
  </AccountLayout>;
}
