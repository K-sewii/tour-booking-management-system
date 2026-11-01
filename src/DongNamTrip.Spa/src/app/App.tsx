import { useEffect, type ReactNode } from 'react';
import { BrowserRouter, Link, Navigate, Route, Routes, useLocation } from 'react-router';
import { AuthProvider, useAuth } from '../auth/AuthContext';
import { DestinationDetailPage } from '../features/destinations/DestinationDetailPage';
import { DestinationsPage } from '../features/destinations/DestinationsPage';
import { FavoritesPage } from '../features/account/FavoritesPage';
import { HomePage } from '../features/home/HomePage';
import { LoginPage } from '../features/auth/LoginPage';
import { NoticesPage } from '../features/account/NoticesPage';
import { PaymentsPage } from '../features/account/PaymentsPage';
import { PlannerPage } from '../features/trips/PlannerPage';
import { ProfilePage } from '../features/account/ProfilePage';
import { RegisterPage } from '../features/auth/RegisterPage';
import { SecurityPage } from '../features/account/SecurityPage';
import { TourDetailPage } from '../features/tours/TourDetailPage';
import { ToursPage } from '../features/tours/ToursPage';
import { TripsPage } from '../features/trips/TripsPage';
import { AuthLayout } from '../layouts/AuthLayout';
import { ShellLayout } from '../layouts/ShellLayout';

function RequireAuth({ children }: { children: ReactNode }) {
  const { user, loading, error } = useAuth();
  const location = useLocation();
  if (loading) return <div className="container section spa-state" role="status">Đang kiểm tra phiên đăng nhập…</div>;
  if (error) return <div className="container section account-message account-message--error" role="alert">{error} <button className="btn outline" onClick={() => window.location.reload()}>Thử lại</button></div>;
  if (!user) return <Navigate to={`/Account/Login?returnUrl=${encodeURIComponent(location.pathname + location.search)}`} replace/>;
  return <>{children}</>;
}

function RouteTitle() {
  const location = useLocation();
  useEffect(() => {
    const path = location.pathname;
    document.title = (path.startsWith('/Account/') ? path.split('/')[2] : path === '/' ? 'Đi gần, khám phá nhiều hơn' : path.split('/')[1]) + ' · DongNamTrip';
  }, [location.pathname]);
  return null;
}

function Content() {
  return <ShellLayout><RouteTitle/><Routes>
    <Route path="/" element={<HomePage/>}/>
    <Route path="/Tours" element={<ToursPage/>}/><Route path="/Tours/Details/:id" element={<TourDetailPage/>}/>
    <Route path="/Destinations" element={<DestinationsPage/>}/><Route path="/Destinations/Details/:id" element={<DestinationDetailPage/>}/>
    <Route path="/Planner" element={<PlannerPage/>}/>
    <Route path="/Account/Login" element={<AuthLayout mode="login"><LoginPage/></AuthLayout>}/>
    <Route path="/Account/Register" element={<AuthLayout mode="register"><RegisterPage/></AuthLayout>}/>
    <Route path="/Trips" element={<RequireAuth><TripsPage/></RequireAuth>}/>
    <Route path="/Account/Profile" element={<RequireAuth><ProfilePage/></RequireAuth>}/>
    <Route path="/Account/Security" element={<RequireAuth><SecurityPage/></RequireAuth>}/>
    <Route path="/Account/Payments" element={<RequireAuth><PaymentsPage/></RequireAuth>}/>
    <Route path="/Account/Payments/:id" element={<RequireAuth><PaymentsPage/></RequireAuth>}/>
    <Route path="/Account/Favorites" element={<RequireAuth><FavoritesPage/></RequireAuth>}/>
    <Route path="/Account/Notices" element={<RequireAuth><NoticesPage/></RequireAuth>}/>
    <Route path="*" element={<div className="container section spa-state"><h1>Không tìm thấy trang.</h1><Link className="btn" to="/">Về trang chủ</Link></div>}/>
  </Routes></ShellLayout>;
}

export default function App() { return <BrowserRouter><AuthProvider><Content/></AuthProvider></BrowserRouter>; }
