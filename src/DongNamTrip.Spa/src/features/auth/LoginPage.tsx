import { useState, type FormEvent } from 'react';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router';
import { errorFor, messageFor } from '../../api/client';
import { useAuth } from '../../auth/AuthContext';
import { AuthArt, safeReturn } from './AuthShared';

export function LoginPage() {
  const auth = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [params] = useSearchParams();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<unknown>(null);
  const [busy, setBusy] = useState(false);
  const [touched, setTouched] = useState(false);
  async function submit(event: FormEvent) {
    event.preventDefault(); setTouched(true); setError(null);
    if (!email.trim() || !/^[^\s@]+@[^\s@]+$/.test(email) || !password) return;
    setBusy(true);
    try { await auth.login(email.trim(), password); navigate(safeReturn(params.get('returnUrl')), { replace: true }); }
    catch (caught) { setError(caught); setPassword(''); }
    finally { setBusy(false); }
  }
  return <><AuthArt mode="login"/><section className="auth-form"><span className="pill">DongNamTrip</span><h1>Chào bạn trở lại.</h1><p className="auth-page__description">Đăng nhập để lưu những lịch trình bạn yêu thích.</p>
    {typeof location.state === 'string' && <p className="account-message account-message--success" role="status">{location.state}</p>}
    <form className="auth-form-card" onSubmit={event => void submit(event)} noValidate>
      {error !== null && <div className="validation validation-summary" role="alert">{messageFor(error)}</div>}
      <label className="auth-field" htmlFor="Email"><span>Email <b aria-hidden="true">*</b></span><input id="Email" type="email" autoComplete="username" autoFocus value={email} onChange={event => setEmail(event.target.value)} maxLength={254} placeholder="Email của bạn"/><span className="field-error" role="alert">{touched && !email.trim() ? 'Vui lòng nhập email' : touched && !/^[^\s@]+@[^\s@]+$/.test(email) ? 'Email không đúng định dạng.' : errorFor(error, 'Email')}</span></label>
      <label className="auth-field" htmlFor="Password"><span>Mật khẩu <b aria-hidden="true">*</b></span><input id="Password" type="password" autoComplete="current-password" value={password} onChange={event => setPassword(event.target.value)} maxLength={128} placeholder="Nhập mật khẩu"/><span className="field-error" role="alert">{touched && !password ? 'Vui lòng nhập mật khẩu' : errorFor(error, 'Password')}</span></label>
      <button className="btn full" type="submit" disabled={busy}>{busy ? 'Đang đăng nhập…' : 'Đăng nhập →'}</button><p className="auth-status" role="status" aria-live="polite"/></form>
    <p className="auth-switch">Chưa có tài khoản? <Link className="text-link" to={`/Account/Register${params.has('returnUrl') ? '?returnUrl=' + encodeURIComponent(params.get('returnUrl')!) : ''}`}>Đăng ký</Link></p></section></>;
}

