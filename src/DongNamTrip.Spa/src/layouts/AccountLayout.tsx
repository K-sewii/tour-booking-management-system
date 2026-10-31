import type { ReactNode } from 'react';
import { Link, NavLink } from 'react-router';

const menu = [
  ['/Account/Profile', 'Hồ sơ tài khoản'], ['/Trips', 'Chuyến đi của tôi'],
  ['/Account/Security', 'Bảo mật tài khoản'], ['/Account/Payments', 'Thanh toán và hoàn tiền'],
  ['/Account/Favorites', 'Yêu thích và lịch sử'], ['/Account/Notices', 'Thông báo và tùy chọn'],
];

export function AccountLayout({ children }: { children: ReactNode }) {
  return <div className="account-page"><div className="container account-wrap">
    <nav className="account-breadcrumb" aria-label="Đường dẫn"><Link to="/">Trang chủ</Link><span aria-hidden="true">/</span><span>Tài khoản</span></nav>
    <div className="account-layout"><aside className="account-sidebar"><div className="account-sidebar-heading"><span className="account-icon" aria-hidden="true">◎</span><div><strong>Tài khoản của bạn</strong><p>Đồng hành mỗi chuyến đi</p></div></div>
      <nav aria-label="Quản lý tài khoản">{menu.map(([to, text]) => <NavLink key={to} to={to} end={to === '/Account/Profile' || to === '/Trips'}>{text}</NavLink>)}</nav></aside>
      <section className="account-content" aria-labelledby="account-title">{children}</section></div>
  </div></div>;
}
