import { useEffect, useState } from 'react';
import { api, messageFor } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Notice, Preference } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { dateTime, Failure, Loading } from '../../components/Catalog';
import { PageState } from '../../components/PageState';
import { AccountLayout } from '../../layouts/AccountLayout';

export function NoticesPage() {
  const { user } = useAuth();
  const notices = useLoad('notices-' + user?.id, () => api.get<Notice[]>('proxy/account/notices'));
  const preferences = useLoad('preferences-' + user?.id, () => api.get<Preference>('proxy/account/preferences'));
  const [securityNotices, setSecurityNotices] = useState(true);
  const [status, setStatus] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  useEffect(() => { if (preferences.data) setSecurityNotices(preferences.data.securityNotices); }, [preferences.data]);
  async function markRead(id: number) {
    setStatus(''); setError('');
    try { await api.put<void>(`proxy/account/notices/${id}/read`, {}); setStatus('Đã đánh dấu đã đọc.'); notices.retry(); }
    catch (caught) { setError(messageFor(caught)); }
  }
  async function save() {
    setBusy(true); setStatus(''); setError('');
    try { const result = await api.put<Preference>('proxy/account/preferences', { securityNotices }); preferences.setData(result); setStatus('Đã lưu tùy chọn thông báo trong ứng dụng.'); }
    catch (caught) { setError(messageFor(caught)); }
    finally { setBusy(false); }
  }
  return <AccountLayout><div className="account-heading"><div className="eyebrow">CẬP NHẬT TÀI KHOẢN</div><h1 id="account-title">Thông báo và tùy chọn</h1><p>Thông báo trong ứng dụng được tạo từ sự kiện bảo mật thực tế.</p></div>
    {status && <PageState kind="success">{status}</PageState>}{error && <PageState>{error}</PageState>}
    <div className="account-card"><div className="account-card-heading"><h2>Thông báo trong ứng dụng</h2><p>Thông báo email và SMS hiện chưa được tích hợp.</p></div>
      {notices.loading ? <Loading/> : notices.error ? <Failure message={notices.error} retry={notices.retry}/> : !notices.data?.length ? <p className="account-empty">Hiện chưa có thông báo.</p> : <ul className="account-list">{notices.data.map(item => <li key={item.id}><div><strong>{item.title} <span className="account-badge">{item.readAt ? 'Đã đọc' : 'Chưa đọc'}</span></strong><p>{item.body}</p><small>{dateTime(item.at)}</small></div>{!item.readAt && <button className="btn outline" onClick={() => void markRead(item.id)}>Đánh dấu đã đọc</button>}</li>)}</ul>}</div>
    <div className="account-card account-secondary"><div className="account-card-heading"><h2>Tùy chọn thông báo</h2><p>Tùy chọn này áp dụng khi đổi mật khẩu. Ngôn ngữ trang chưa có hệ thống bản địa hóa để thay đổi.</p></div>
      {preferences.loading ? <Loading/> : preferences.error ? <Failure message={preferences.error} retry={preferences.retry}/> : <><div className="account-settings"><label><input type="checkbox" checked={securityNotices} onChange={event => setSecurityNotices(event.target.checked)}/> Nhận thông báo bảo mật trong ứng dụng</label></div><div className="account-actions"><button className="btn" onClick={() => void save()} disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu tùy chọn'}</button></div></>}</div>
  </AccountLayout>;
}
