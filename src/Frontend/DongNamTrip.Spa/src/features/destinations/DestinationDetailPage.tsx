import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router';
import { ApiError, api } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { DestinationDetail } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { art, Failure, Loading, price } from '../../components/Catalog';
import { FavoriteButton } from '../account/FavoriteButton';
import { recordActivity } from '../account/recordActivity';
import { dayLabel, groupSlots, hhmm, slotDays } from './slots';
import '../../styles/content.css';

// SCR07: thông tin địa điểm, hoạt động, điều kiện, phí tham khảo và các suất đã công bố theo ngày.
export function DestinationDetailPage() {
  const id = Number(useParams().id);
  const { user } = useAuth();
  const [day, setDay] = useState('');
  const detail = useLoad('destination-detail-' + id, () => Number.isInteger(id) && id > 0
    ? api.get<DestinationDetail>(`proxy/destinations/${id}`).catch(error => { if (error instanceof ApiError && error.status === 404) return null; throw error; })
    : Promise.resolve(null));
  const destination = detail.data;
  const userId = user?.id, destinationId = destination?.id;
  useEffect(() => { if (userId && id > 0 && destinationId) recordActivity('destination', id); }, [userId, id, destinationId]);
  useEffect(() => { setDay(''); }, [id]);
  if (detail.loading) return <div className="container section"><Loading/></div>;
  if (detail.error) return <div className="container section"><Failure message={detail.error} retry={detail.retry}/></div>;
  if (!destination) return <div className="container section"><h1>Địa điểm không còn hiển thị.</h1><Link to="/Destinations">Xem các điểm đến</Link></div>;
  const days = slotDays(destination.activities);
  const hours = destination.openTime && destination.closeTime ? `${hhmm(destination.openTime)} – ${hhmm(destination.closeTime)}` : null;
  const map = destination.latitude !== null && destination.longitude !== null ? `https://www.google.com/maps?q=${destination.latitude},${destination.longitude}` : null;
  return <div className="container section"><nav className="breadcrumbs"><Link to="/Destinations">Điểm đến</Link> / {destination.region}</nav>
    <div className="detail-heading"><span className="pill">{destination.category}</span><h1>{destination.name}</h1><p>{destination.address}</p></div><FavoriteButton kind="destination" id={id}/>
    <img className="detail-hero" src={art(destination.illustration)} alt={`Minh họa ${destination.region}`}/>
    <div className="detail-layout"><article><h2>Một chút về điểm đến</h2><p>{destination.description}</p>
      <div className="facts"><div><small>Khu vực du lịch</small><b>{destination.region}</b></div><div><small>Thời lượng dự kiến</small><b>{destination.visitMinutes} phút</b></div>
        {hours && <div><small>Giờ mở cửa</small><b>{hours}</b></div>}
        {map && <div><small>Vị trí</small><b><a href={map} target="_blank" rel="noreferrer">Xem bản đồ ↗</a></b></div>}</div>
      <h2>Hoạt động & suất tổ chức</h2>
      {!destination.activities.length && <div className="notice">Địa điểm này chưa có hoạt động được công bố.</div>}
      {days.length > 1 && <div className="cx-filter"><label htmlFor="slot-day">Xem suất theo ngày</label>
        <select id="slot-day" value={day} onChange={event => setDay(event.target.value)}><option value="">Tất cả các ngày</option>{days.map(item => <option key={item} value={item}>{dayLabel(item)}</option>)}</select></div>}
      {destination.activities.map(activity => {
        const grouped = groupSlots(activity.slots, day);
        return <section className="cx-activity" key={activity.id} aria-label={activity.name}>
          <h3>{activity.name}</h3>{activity.description && <p>{activity.description}</p>}
          <div className="cx-meta"><span>Thời lượng: <b>{activity.durationMinutes} phút</b></span>
            <span>Phí tham khảo: <b>{activity.referenceFee === null ? 'Chưa công bố' : activity.referenceFee === 0 ? 'Miễn phí' : price(activity.referenceFee)}</b></span></div>
          {activity.conditions && <p><b>Điều kiện tham gia:</b> {activity.conditions}</p>}
          <div className="cx-days">{grouped.length
            ? grouped.map(([date, slots]) => <div className="cx-day" key={date}><strong>{dayLabel(date)}</strong>
              {slots.map(slot => <span className="cx-slot" key={slot.id}>{hhmm(slot.start)} – {hhmm(slot.end)} <span className="cx-status">Đã công bố</span></span>)}</div>)
            : <p className="cx-note">{activity.slots.length ? 'Không có suất trong ngày đã chọn.' : 'Chưa có suất được công bố cho hoạt động này.'}</p>}</div>
        </section>;
      })}
      {!!destination.activities.length && <p className="cx-note">Phí chỉ mang tính tham khảo; lịch cụ thể được xác nhận khi lập tour.</p>}</article>
      <aside className="booking-panel"><div className="eyebrow">THÊM MỘT ĐIỂM ĐÁNG NHỚ</div><h2>Nơi này có trong chuyến đi của bạn?</h2><p>Lưu vào lịch trình, chọn thêm điểm và sắp xếp theo ý thích.</p>
        <Link className="btn full" to={`/Planner?destinationId=${id}`}>Thêm vào lịch trình ↗</Link>
        <Link className="btn outline full" to={`/Listen?destinationId=${id}`}>Nghe thuyết minh ♪</Link></aside></div></div>;
}
