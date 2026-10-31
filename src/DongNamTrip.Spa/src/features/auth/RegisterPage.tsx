import { useState, type FormEvent } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router';
import { ApiError, errorFor, messageFor } from '../../api/client';
import { useAuth } from '../../auth/AuthContext';
import { AuthArt, safeReturn } from './AuthShared';

export function RegisterPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const [form, setForm] = useState({ name: '', email: '', phoneNumber: '', password: '', confirmPassword: '', termsAccepted: false });
  const [error, setError] = useState<unknown>(null);
  const [touched, setTouched] = useState(false);
  const [busy, setBusy] = useState(false);
  const invalid = {
    name: !form.name.trim() ? 'Vui lòng nhập họ tên.' : form.name.trim().length > 100 ? 'Họ tên tối đa 100 ký tự.' : '',
    email: !form.email.trim() ? 'Vui lòng nhập email' : !/^[^\s@]+@[^\s@]+$/.test(form.email) || /[^\x00-\x7F]/.test(form.email) ? 'Email không đúng định dạng.' : '',
    phoneNumber: form.phoneNumber && !/^[0-9]{1,15}$/.test(form.phoneNumber) ? 'Số điện thoại chỉ gồm tối đa 15 chữ số.' : '',
    password: form.password.length < 8 || form.password.length > 128 || !/[\p{L}]/u.test(form.password) || !/[0-9]/.test(form.password) ? 'Mật khẩu cần từ 8 đến 128 ký tự, có chữ và số.' : '',
    confirmPassword: form.password !== form.confirmPassword ? 'Nhập lại mật khẩu chưa khớp.' : '',
    termsAccepted: !form.termsAccepted ? 'Vui lòng đồng ý với điều khoản.' : '',
  };
  async function submit(event: FormEvent) {
    event.preventDefault(); setTouched(true); setError(null);
    if (Object.values(invalid).some(Boolean)) return;
    setBusy(true);
    try { await auth.register({ name: form.name.trim(), email: form.email.trim(), phoneNumber: form.phoneNumber || null, password: form.password, confirmPassword: form.confirmPassword, termsAccepted: form.termsAccepted }); navigate(safeReturn(params.get('returnUrl')), { replace: true }); }
    catch (caught) { setError(caught); setForm(current => ({ ...current, password: '', confirmPassword: '' })); }
    finally { setBusy(false); }
  }
  const set = (key: keyof typeof form, value: string | boolean) => setForm(current => ({ ...current, [key]: value }));
  return <><AuthArt mode="register"/><section className="auth-form"><span className="pill">Tài khoản khách hàng</span><h1>Tạo tài khoản.</h1><p className="auth-page__description">Lưu hành trình yêu thích và quản lý chuyến đi của bạn dễ dàng hơn.</p>
    <form className="auth-form-card" onSubmit={event => void submit(event)} noValidate>{error !== null && <div className="validation validation-summary" role="alert">{error instanceof ApiError && error.status === 409 ? 'Email đã được sử dụng.' : messageFor(error)}</div>}
      <label className="auth-field" htmlFor="Name"><span>Họ và tên <b aria-hidden="true">*</b></span><input id="Name" autoComplete="name" autoFocus value={form.name} onChange={event => set('name', event.target.value)} maxLength={100} placeholder="Họ và tên của bạn"/><span className="field-error">{touched && invalid.name || errorFor(error, 'Name')}</span></label>
      <label className="auth-field" htmlFor="Email"><span>Email <b aria-hidden="true">*</b></span><input id="Email" type="email" autoComplete="email" value={form.email} onChange={event => set('email', event.target.value)} maxLength={254} placeholder="Email của bạn"/><span className="field-error">{touched && invalid.email || errorFor(error, 'Email')}</span></label>
      <label className="auth-field" htmlFor="PhoneNumber"><span>Số điện thoại</span><input id="PhoneNumber" type="tel" autoComplete="tel" inputMode="numeric" value={form.phoneNumber} onChange={event => set('phoneNumber', event.target.value.replace(/[^0-9]/g, '').slice(0, 15))} maxLength={15} placeholder="Ví dụ: 0901234567"/><span className="field-error">{touched && invalid.phoneNumber || errorFor(error, 'PhoneNumber')}</span></label>
      <label className="auth-field" htmlFor="Password"><span>Mật khẩu <b aria-hidden="true">*</b></span><input id="Password" type="password" autoComplete="new-password" value={form.password} onChange={event => set('password', event.target.value)} maxLength={128} placeholder="Ít nhất 8 ký tự, có chữ và số"/><span className="field-error">{touched && invalid.password || errorFor(error, 'Password')}</span></label>
      <label className="auth-field" htmlFor="ConfirmPassword"><span>Nhập lại mật khẩu <b aria-hidden="true">*</b></span><input id="ConfirmPassword" type="password" autoComplete="new-password" value={form.confirmPassword} onChange={event => set('confirmPassword', event.target.value)} maxLength={128} placeholder="Nhập lại mật khẩu"/><span className="field-error">{touched && invalid.confirmPassword || errorFor(error, 'ConfirmPassword')}</span></label>
      <label className="terms-field" htmlFor="TermsAccepted"><input id="TermsAccepted" type="checkbox" checked={form.termsAccepted} onChange={event => set('termsAccepted', event.target.checked)}/><span>Tôi đồng ý với điều khoản sử dụng <b aria-hidden="true">*</b></span></label><span className="field-error">{touched && invalid.termsAccepted || errorFor(error, 'TermsAccepted')}</span>
      <button className="btn full" type="submit" disabled={busy || !form.termsAccepted}>{busy ? 'Đang tạo tài khoản…' : 'Đăng ký →'}</button><p className="auth-status" role="status" aria-live="polite"/></form>
    <p className="auth-switch">Đã có tài khoản? <Link className="text-link" to={`/Account/Login${params.has('returnUrl') ? '?returnUrl=' + encodeURIComponent(params.get('returnUrl')!) : ''}`}>Đăng nhập</Link></p></section></>;
}

