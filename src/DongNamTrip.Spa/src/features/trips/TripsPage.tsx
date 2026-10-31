import { useMemo, useState } from 'react';
import { Link } from 'react-router';
import { api } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Destination, Itinerary, Payment } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { dateTime, Failure, Loading, price } from '../../components/Catalog';
import { AccountLayout } from '../../layouts/AccountLayout';

export function TripsPage() {
  const { user } = useAuth();
  const [tab, setTab] = useState<'drafts' | 'bookings' | 'quotes'>('drafts');
  const trips = useLoad('my-trips-' + user?.id, () => api.get<Itinerary[]>('proxy/itineraries'));
  const destinations = useLoad('my-trip-places', () => api.get<Destination[]>('proxy/destinations'));
  const payments = useLoad('my-trip-orders-' + user?.id, () => api.get<Payment[]>('proxy/account/payments'));
  const names = useMemo(() => new Map((destinations.data || []).map(place => [place.id, place.name])), [destinations.data]);
  return <AccountLayout><div className="account-heading"><div className="eyebrow">CHÀO {user?.name}</div><h1 id="account-title">Chuyến đi của tôi</h1><p>Tiếp tục từ một ý tưởng bạn đã lưu.</p></div>
    <div className="account-trips"><div className="tabs" role="tablist" aria-label="Loại chuyến đi">
      <button role="tab" className={tab === 'drafts' ? 'current' : ''} aria-selected={tab === 'drafts'} onClick={() => setTab('drafts')}>Lịch trình nháp ({trips.data?.length || 0})</button>
      <button role="tab" className={tab === 'bookings' ? 'current' : ''} aria-selected={tab === 'bookings'} onClick={() => setTab('bookings')}>Tour đã đặt</button>
      <button role="tab" className={tab === 'quotes' ? 'current' : ''} aria-selected={tab === 'quotes'} onClick={() => setTab('quotes')}>Yêu cầu & báo giá</button></div>
      {tab === 'drafts' && (trips.loading || destinations.loading ? <Loading/> : trips.error || destinations.error ? <Failure message={trips.error || destinations.error} retry={() => { trips.retry(); destinations.retry(); }}/> : !trips.data?.length ?
        <div className="empty"><span className="empty-icon">↗</span><h2>Chuyến đi tiếp theo bắt đầu ở đây.</h2><p>Bạn chưa lưu lịch trình nào. Hãy chọn vài điểm đến yêu thích.</p><Link className="btn" to="/Planner">Tạo lịch trình đầu tiên</Link></div>
        : <div className="card-grid">{trips.data.map(trip => <article className="saved-trip" key={trip.id}><span className="pill">Lịch trình nháp #{trip.id}</span><h2>{trip.name}</h2><p>{trip.startDate} · {trip.guests} khách</p><ol>{trip.destinationIds.map(id => <li key={id}>{names.get(id) || 'Địa điểm không còn hiển thị'}</li>)}</ol><small>Đã lưu trong phiên chạy API demo.</small></article>)}</div>)}
      {tab === 'bookings' && (payments.loading ? <Loading/> : payments.error ? <Failure message={payments.error} retry={payments.retry}/> : !payments.data?.length ? <div className="empty"><h2>Chưa có tour đã đặt.</h2><p>Chức năng đặt tour và xử lý đơn đang được phát triển.</p><Link className="btn" to="/Tours">Khám phá tour</Link></div>
        : <div className="account-card"><ul className="account-list">{payments.data.map(order => <li key={order.id}><div><strong>Đơn #{order.id} · {order.status}</strong><p>{dateTime(order.placedAt)} · Giá chốt {price(order.agreedPrice)}</p></div><Link className="btn outline" to={`/Account/Payments/${order.id}`}>Xem giao dịch</Link></li>)}</ul></div>)}
      {tab === 'quotes' && <div className="empty"><h2>Ý tưởng hôm nay, hành trình ngày mai.</h2><p>Chức năng gửi yêu cầu tour riêng và nhận báo giá sẽ được bổ sung sau.</p><Link className="btn" to="/Planner">Lên ý tưởng chuyến đi</Link></div>}
      <p className="hint">Bản demo giữ lịch trình khi bạn đăng xuất và đăng nhập lại, nhưng dữ liệu sẽ mất khi API khởi động lại.</p></div>
    </AccountLayout>;
}
