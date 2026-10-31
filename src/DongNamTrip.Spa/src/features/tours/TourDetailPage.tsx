import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router';
import { api } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Destination, Tour } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { art, Failure, Loading, price } from '../../components/Catalog';
import { FavoriteButton } from '../account/FavoriteButton';
import { recordActivity } from '../account/recordActivity';

export function TourDetailPage() {
  const id = Number(useParams().id);
  const { user } = useAuth();
  const tours = useLoad('tour-detail-' + id, () => api.get<Tour[]>('proxy/tours'));
  const destinations = useLoad('tour-places-' + id, () => api.get<Destination[]>('proxy/destinations'));
  const [guests, setGuests] = useState(2);
  const tour = tours.data?.find(item => item.id === id);
  const places = tour?.destinationIds.map(placeId => destinations.data?.find(item => item.id === placeId)).filter((item): item is Destination => Boolean(item)) || [];
  useEffect(() => { if (user && id > 0 && tour) recordActivity('tour', id); }, [user?.id, id, tour?.id]);
  if (tours.loading || destinations.loading) return <div className="container section"><Loading/></div>;
  if (tours.error || destinations.error) return <div className="container section"><Failure message={tours.error || destinations.error} retry={() => { tours.retry(); destinations.retry(); }}/></div>;
  if (!tour) return <div className="container section"><h1>Tour không còn hiển thị.</h1><Link to="/Tours">Xem các tour</Link></div>;
  return <div className="container section"><nav className="breadcrumbs"><Link to="/">Trang chủ</Link> / <Link to="/Tours">Tour</Link> / {tour.region}</nav>
    <div className="detail-heading"><span className="pill">{tour.category} · {tour.duration}</span><h1>{tour.name}</h1><p>{tour.summary}</p></div><FavoriteButton kind="tour" id={id}/>
    <img className="detail-hero" src={art(tour.illustration)} alt={`Minh họa ${tour.region}`}/><div className="detail-layout"><article><div className="subnav"><a href="#overview">Tổng quan</a><a href="#itinerary">Lịch trình</a><a href="#notes">Lưu ý</a></div>
      <h2 id="overview">Một ngày, nhiều điều để nhớ.</h2><p>{tour.summary}</p><div className="facts"><div><small>Thời lượng</small><b>{tour.duration}</b></div><div><small>Khu vực</small><b>{tour.region}</b></div><div><small>Điểm tham quan</small><b>{places.length} địa điểm</b></div></div>
      <h2 id="itinerary">Lịch trình gợi ý</h2><ol className="timeline">{places.map(place => <li key={place.id}><h3>{place.name}</h3><p>{place.description}</p><Link className="text-link" to={`/Destinations/Details/${place.id}`}>Khám phá địa điểm ↗</Link></li>)}</ol>
      <h2 id="notes">Trước khi bạn lên đường</h2><p>Đây là tour mẫu phục vụ trải nghiệm giao diện. Ngày khởi hành, dịch vụ bao gồm, chính sách hủy và giá chính thức sẽ được cập nhật khi chức năng đặt tour hoàn thiện.</p></article>
      <aside className="booking-panel"><small>Giá tham khảo / khách</small><h2>{price(tour.referencePrice)}</h2><label>Ngày dự kiến<input type="date" min={new Date().toISOString().slice(0, 10)} aria-label="Ngày đi dự kiến"/></label>
        <label>Số khách<input id="guest-count" type="number" min="1" max="40" value={guests} onChange={event => setGuests(Math.max(1, Math.min(40, Number(event.target.value) || 1)))}/></label>
        <div className="estimate"><span>Tạm tính minh họa</span><b id="estimate-value">{price(tour.referencePrice * guests)}</b></div><button className="btn full" disabled>Đặt tour · Đang phát triển</button><p className="hint">Chưa giữ chỗ hoặc tạo đơn. Bạn có thể dùng tour này làm ý tưởng cho lịch trình.</p><Link className="btn outline full" to={`/Planner?tourId=${id}`}>Tùy chỉnh lịch trình ↗</Link></aside></div></div>;
}

