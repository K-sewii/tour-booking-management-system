import { useEffect, useRef, useState } from 'react';
import { Link, useParams } from 'react-router';
import { api, messageFor } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Booking, Departure, TourDetail, TourEstimate } from '../../api/types';
import { useMeta } from '../../api/meta';
import { useAuth } from '../../auth/AuthContext';
import { dateTime, Failure, Loading, price } from '../../components/Catalog';
import { TourImageCollage } from '../../components/TourImageCollage';
import { FavoriteButton } from '../account/FavoriteButton';
import { recordActivity } from '../account/recordActivity';

function BookingPanel({ tourId, guests }: { tourId: number; guests: number }) {
  const { user } = useAuth();
  const departures = useLoad('tour-departures-' + tourId, () => api.get<Departure[]>(`proxy/tours/${tourId}/departures`));
  const [departureId, setDepartureId] = useState('');
  const [contact, setContact] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [placed, setPlaced] = useState<Booking | null>(null);
  const [confirming, setConfirming] = useState(false);
  const bookButton = useRef<HTMLButtonElement>(null), confirmButton = useRef<HTMLButtonElement>(null), changed = useRef(false);
  // Mở bước xác nhận: tiêu điểm sang nút xác nhận; đóng: trả về nút "Đặt tour" (không chiếm tiêu điểm lúc tải trang).
  useEffect(() => { if (changed.current) (confirming ? confirmButton : bookButton).current?.focus(); changed.current = true; }, [confirming]);
  if (departures.loading) return <Loading/>;
  if (departures.error) return <Failure message={departures.error} retry={departures.retry}/>;
  const items = departures.data ?? [];
  if (!items.length) return <p className="hint">Tour này chưa có lịch khởi hành đang mở. Bạn có thể dùng tour làm ý tưởng cho lịch trình riêng.</p>;
  const chosen = items.find(item => String(item.id) === departureId);
  if (placed) return <div role="status" className="notice"><p>Đã tạo đơn #{placed.id} và giữ chỗ đến {placed.holdUntil ? dateTime(placed.holdUntil) : '—'}.</p><Link className="btn full" to={`/Account/Bookings/${placed.id}`}>Xem đơn</Link></div>;

  async function submit() {
    setError(''); setBusy(true);
    try { setPlaced(await api.post<Booking>('proxy/account/bookings', { departureId: Number(departureId), guests, contact: contact.trim() })); }
    catch (caught) { setError(messageFor(caught)); departures.retry(); }
    finally { setBusy(false); setConfirming(false); }
  }

  return <div className="private-request-form">
    <label>Lịch khởi hành<select value={departureId} onChange={event => setDepartureId(event.target.value)}><option value="">Chọn lịch khởi hành</option>
      {items.map(item => <option key={item.id} value={item.id}>{dateTime(item.startsAt)} · {price(item.price)} / khách · còn {item.remaining} chỗ</option>)}</select></label>
    {chosen?.cancelPolicy && <p className="hint">Điều kiện hủy: {chosen.cancelPolicy}</p>}
    {chosen && <p className="hint">Giá chốt tạm tính {price(chosen.price * guests)} cho {guests} khách. Chỗ được giữ có thời hạn; đơn xác nhận sau khi nhân viên ghi nhận khoản cần thu.</p>}
    <label>Thông tin liên hệ<input value={contact} onChange={event => setContact(event.target.value)} maxLength={250} placeholder="Số điện thoại hoặc email"/></label>
    {error && <p role="alert" className="account-message account-message--error">{error}</p>}
    {chosen && guests > chosen.remaining && <p role="alert" className="account-message account-message--error">Lịch khởi hành chỉ còn {chosen.remaining} chỗ.</p>}
    {user && confirming && chosen ? <div role="group" aria-label="Xác nhận đặt tour" className="notice"><p>Xác nhận đặt {guests} khách, khởi hành {dateTime(chosen.startsAt)}, tạm tính {price(chosen.price * guests)}. Chỗ được giữ có thời hạn; đơn xác nhận sau khi nhân viên ghi nhận khoản cần thu.</p>
        <button ref={confirmButton} className="btn full" type="button" disabled={busy} onClick={() => void submit()}>{busy ? 'Đang giữ chỗ…' : 'Xác nhận đặt'}</button>
        <button className="btn outline full" type="button" disabled={busy} onClick={() => setConfirming(false)}>Quay lại</button></div>
      : user ? <button ref={bookButton} className="btn full" type="button" disabled={!chosen || !contact.trim() || guests > (chosen?.remaining ?? 0)} onClick={() => setConfirming(true)}>Đặt tour</button>
      : <Link className="btn full" to={`/Account/Login?returnUrl=${encodeURIComponent('/Tours/Details/' + tourId)}`}>Đăng nhập để đặt tour</Link>}
  </div>;
}

export function TourDetailPage() {
  const id = Number(useParams().id);
  const { user } = useAuth();
  const detail = useLoad('tour-detail-' + id, () => api.get<TourDetail>('proxy/tours/' + id));
  const meta = useMeta();
  const [guests, setGuests] = useState(2);
  const estimate = useLoad('tour-estimate-' + id + '-' + guests, () => api.get<TourEstimate>(`proxy/tours/${id}/estimate?guests=${guests}`));
  const tour = detail.data?.tour;
  const places = detail.data?.places || [];
  const userId = user?.id, tourId = tour?.id;
  useEffect(() => { if (userId && id > 0 && tourId) recordActivity('tour', id); }, [userId, id, tourId]);
  if (detail.loading) return <div className="container section"><Loading/></div>;
  if (detail.error) return <div className="container section"><Failure message={detail.error} retry={detail.retry}/></div>;
  if (!tour) return <div className="container section"><h1>Tour không còn hiển thị.</h1><Link to="/Tours">Xem các tour</Link></div>;
  return <div className="container section"><nav className="breadcrumbs"><Link to="/">Trang chủ</Link> / <Link to="/Tours">Tour</Link> / {tour.region}</nav>
    <div className="detail-heading">{tour.referenceOnly && <p className="reference-notice">Dữ liệu tham khảo — chưa công bố</p>}<span className="pill">{tour.category} · {tour.duration}</span><h1>{tour.name}</h1><p>{tour.summary}</p></div>{!tour.referenceOnly && <FavoriteButton kind="tour" id={id}/>}
    <TourImageCollage tour={tour} detail/><div className="detail-layout"><article><div className="subnav"><a href="#overview">Tổng quan</a><a href="#itinerary">Lịch trình</a><a href="#notes">Lưu ý</a></div>
      <h2 id="overview">Một ngày, nhiều điều để nhớ.</h2><p>{tour.summary}</p><div className="facts"><div><small>Thời lượng</small><b>{tour.duration}</b></div><div><small>Khu vực</small><b>{tour.region}</b></div><div><small>Điểm tham quan</small><b>{places.length} địa điểm</b></div></div>
      <h2 id="itinerary">Lịch trình gợi ý</h2><ol className="timeline">{places.map(place => <li key={place.id}><h3>{place.name}</h3><p>{place.description}</p><Link className="text-link" to={`/Destinations/Details/${place.id}`}>Khám phá địa điểm ↗</Link></li>)}</ol>
      <h2 id="notes">Trước khi bạn lên đường</h2><p>Giá chính thức, điều kiện hủy và số chỗ còn lại hiển thị theo từng lịch khởi hành ở khung đặt tour. Chỗ chỉ được giữ khi bạn đặt tour; nhân viên xác nhận sau khi nhận khoản cần thu.</p></article>
      <aside className="booking-panel"><small>Giá tham khảo / khách</small><h2>{price(tour.referencePrice)}</h2>
        <label>Số khách<input id="guest-count" type="number" min={meta.data?.guestsMin} max={meta.data?.guestsMax} value={guests} onChange={event => setGuests(Math.max(meta.data?.guestsMin ?? 1, Math.min(meta.data?.guestsMax ?? 1, Number(event.target.value) || 1)))}/></label>
        <div className="estimate"><span>Tạm tính minh họa</span><b id="estimate-value">{estimate.data ? price(estimate.data.total) : '…'}</b></div>{tour.referenceOnly ? <p className="reference-notice">Dữ liệu tham khảo — chưa công bố. Không thể đặt tour.</p> : <><BookingPanel tourId={id} guests={guests}/><Link className="btn outline full" to={`/Planner?tourId=${id}`}>Tùy chỉnh lịch trình ↗</Link></>}</aside></div></div>;
}

