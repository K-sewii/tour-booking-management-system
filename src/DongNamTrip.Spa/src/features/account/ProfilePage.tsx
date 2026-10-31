import { useEffect, useState, type FormEvent } from 'react';
import { api, errorFor, messageFor } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Profile } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { Failure, Loading } from '../../components/Catalog';
import { PageState } from '../../components/PageState';
import { AccountLayout } from '../../layouts/AccountLayout';

type LoadedProfile = { profile: Profile; loadToken: string };
type Form = { name: string; email: string; phoneNumber: string };
const fromProfile = (profile: Profile): Form => ({ name: profile.name, email: profile.email, phoneNumber: profile.phoneNumber || '' });

export function ProfilePage() {
  const auth = useAuth();
  const loaded = useLoad('profile-' + auth.user?.id, () => api.get<LoadedProfile>('profile'));
  const [form, setForm] = useState<Form>({ name: '', email: '', phoneNumber: '' });
  const [error, setError] = useState<unknown>(null);
  const [success, setSuccess] = useState('');
  const [busy, setBusy] = useState(false);
  useEffect(() => { if (loaded.data) setForm(fromProfile(loaded.data.profile)); }, [loaded.data]);
  const oldPhone = loaded.data?.profile.phoneNumber || '';
  const legacy = Boolean(oldPhone && !/^[0-9]{1,15}$/.test(oldPhone));
  function validate() {
    if (form.name.trim().length < 2 || form.name.trim().length > 100) return 'Họ tên cần có từ 2 đến 100 ký tự.';
    if (!/^[^\s@]+@[^\s@]+$/.test(form.email.trim()) || /[^\x00-\x7F]/.test(form.email)) return 'Email không đúng định dạng.';
    if (form.phoneNumber.trim() && form.phoneNumber.trim() !== oldPhone.trim() && !/^[0-9]{1,15}$/.test(form.phoneNumber.trim())) return 'Số điện thoại chỉ gồm tối đa 15 chữ số.';
    return '';
  }
  async function submit(event: FormEvent) {
    event.preventDefault(); setSuccess(''); setError(null);
    if (!loaded.data?.loadToken || !loaded.data.profile) { setError(new Error('Hãy tải lại hồ sơ đầy đủ trước khi lưu.')); return; }
    const invalid = validate(); if (invalid) { setError(new Error(invalid)); return; }
    setBusy(true);
    try {
      const updated = await api.put<Profile>('profile', { profile: { name: form.name.trim(), email: form.email.trim(), phoneNumber: form.phoneNumber.trim() || null }, loadToken: loaded.data.loadToken });
      loaded.setData({ ...loaded.data, profile: updated });
      auth.setUser(auth.user ? { ...auth.user, name: updated.name, email: updated.email } : null);
      setForm(fromProfile(updated)); setSuccess('Đã lưu thay đổi hồ sơ tài khoản.');
    } catch (caught) { setError(caught); }
    finally { setBusy(false); }
  }
  function cancel() { setError(null); setSuccess(''); loaded.retry(); }
  return <AccountLayout><div className="account-heading"><div className="eyebrow">THÔNG TIN CÁ NHÂN</div><h1 id="account-title">Hồ sơ tài khoản</h1><p>Quản lý thông tin liên hệ cho những hành trình của bạn.</p></div>
    {success && <PageState kind="success">{success}</PageState>}
    {loaded.loading ? <Loading text="Đang tải hồ sơ…"/> : loaded.error ? <Failure message={loaded.error} retry={loaded.retry}/> : loaded.data && <div className="account-card"><div className="account-card-heading"><h2>Thông tin liên hệ</h2><p>Các trường có dấu * là bắt buộc.</p></div>
      <form onSubmit={event => void submit(event)} noValidate>{error !== null && <div className="account-message account-message--error" role="alert">{messageFor(error)}</div>}
        <div className="account-fields"><div className="account-field"><label htmlFor="profile-name">Họ và tên <span className="account-required">*</span></label><input id="profile-name" value={form.name} onChange={event => setForm({ ...form, name: event.target.value })} autoComplete="name" maxLength={100} required/><span className="account-field-error">{errorFor(error, 'Name')}</span></div>
          <div className="account-field"><label htmlFor="profile-email">Email <span className="account-required">*</span></label><input id="profile-email" type="email" value={form.email} onChange={event => setForm({ ...form, email: event.target.value })} autoComplete="email" maxLength={254} required/><span className="account-hint">Đây cũng là email bạn dùng để đăng nhập.</span><span className="account-field-error">{errorFor(error, 'Email')}</span></div>
          <div className="account-field"><label htmlFor="profile-phone">Số điện thoại <span className="account-optional">(không bắt buộc)</span></label><input id="profile-phone" type="tel" value={form.phoneNumber} onChange={event => setForm({ ...form, phoneNumber: event.target.value })} autoComplete="tel" inputMode="numeric" maxLength={legacy ? undefined : 15}/>
            <span className="account-hint">{legacy ? 'Số cũ được giữ nguyên nếu không sửa. Khi thay đổi, nhập tối đa 15 chữ số.' : 'Chỉ gồm chữ số, tối đa 15 chữ số. Để trống nếu chưa muốn cung cấp.'}</span><span className="account-field-error">{errorFor(error, 'PhoneNumber')}</span></div></div>
        <div className="account-actions"><button className="btn" type="submit" disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu thay đổi'}</button><button className="btn outline" type="button" onClick={cancel} disabled={busy}>Hủy thay đổi</button><span className="account-status" role="status" aria-live="polite">{busy ? 'Đang lưu thay đổi…' : ''}</span></div>
      </form></div>}
    <p className="account-footnote">◇ Thông tin được cập nhật cho tài khoản đang đăng nhập.</p>
  </AccountLayout>;
}
