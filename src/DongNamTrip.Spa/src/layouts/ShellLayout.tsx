import { useEffect, useState, type ReactNode } from 'react';
import { Link, NavLink, useLocation, useNavigate } from 'react-router';
import { useAuth } from '../auth/AuthContext';

export function ShellLayout({ children }: { children: ReactNode }) {
  const { user, loading, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);
  useEffect(() => { setOpen(false); window.scrollTo(0, 0); }, [location.pathname, location.search]);
  async function signOut() { await logout(); navigate('/'); }
  return <>
    <a className="skip" href="#main">Đến nội dung chính</a>
    <header className="header"><div className="container nav">
      <Link className="brand" to="/"><img src="/images/mark.svg" alt=""/>DongNam<span>Trip</span><i>Đi gần. Khám phá nhiều hơn.</i></Link>
      <button className="menu-toggle" aria-label="Mở menu" aria-expanded={open} aria-controls="navigation" onClick={() => setOpen(!open)}>☰</button>
      <nav id="navigation" className={open ? 'open' : ''} aria-label="Điều hướng chính">
        <NavLink to="/Tours">Khám phá tour</NavLink><NavLink to="/Destinations">Điểm đến</NavLink>
        <NavLink to="/Planner">Tự tạo chuyến đi</NavLink><NavLink to="/Trips">Chuyến đi của tôi</NavLink>
        {!loading && (user ? <><Link className="btn small outline" to="/Account/Profile">Hồ sơ</Link><button className="btn small outline" onClick={() => void signOut()}>Đăng xuất</button></>
          : <Link className="btn small" to="/Account/Login">Đăng nhập →</Link>)}
      </nav>
    </div></header>
    <main id="main">{children}</main>
    <footer><div className="container footer-grid"><div><Link className="brand light" to="/">DongNam<span>Trip</span></Link><p>Những hành trình gần.<br/>Những trải nghiệm thật đáng nhớ.</p></div>
      <div><b>Bắt đầu khám phá</b><Link to="/Tours">Tour có sẵn</Link><Link to="/Destinations">Điểm đến Đông Nam Bộ</Link><Link to="/Planner">Tự tạo chuyến đi</Link></div>
      <div><b>Chuyến đi của bạn</b><Link to="/Trips">Lịch trình đã lưu</Link><Link to={user ? '/Account/Profile' : '/Account/Login'}>Tài khoản</Link><p>Dự án học tập của nhóm DongNamTrip</p></div></div>
      <div className="container footer-bottom">© 2026 DongNamTrip <span>Hình minh họa • Giá mẫu • Chưa giao dịch thực tế</span></div>
    </footer>
  </>;
}
