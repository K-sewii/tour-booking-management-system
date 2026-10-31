import { useEffect } from 'react';
import { Link, useParams } from 'react-router';
import { api } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Destination } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { art, Failure, Loading } from '../../components/Catalog';
import { FavoriteButton } from '../account/FavoriteButton';
import { recordActivity } from '../account/recordActivity';

export function DestinationDetailPage() {
  const id = Number(useParams().id);
  const { user } = useAuth();
  const destinations = useLoad('destination-detail-' + id, () => api.get<Destination[]>('proxy/destinations'));
  const destination = destinations.data?.find(item => item.id === id);
  useEffect(() => { if (user && id > 0 && destination) recordActivity('destination', id); }, [user?.id, id, destination?.id]);
  if (destinations.loading) return <div className="container section"><Loading/></div>;
  if (destinations.error) return <div className="container section"><Failure message={destinations.error} retry={destinations.retry}/></div>;
  if (!destination) return <div className="container section"><h1>Địa điểm không còn hiển thị.</h1><Link to="/Destinations">Xem các điểm đến</Link></div>;
  return <div className="container section"><nav className="breadcrumbs"><Link to="/Destinations">Điểm đến</Link> / {destination.region}</nav>
    <div className="detail-heading"><span className="pill">{destination.category}</span><h1>{destination.name}</h1><p>{destination.address}</p></div><FavoriteButton kind="destination" id={id}/>
    <img className="detail-hero" src={art(destination.illustration)} alt={`Minh họa ${destination.region}`}/><div className="detail-layout"><article><h2>Một chút về điểm đến</h2><p>{destination.description}</p>
      <div className="facts"><div><small>Khu vực du lịch</small><b>{destination.region}</b></div><div><small>Thời lượng dự kiến</small><b>{destination.visitMinutes} phút</b></div></div>
      <h2>Hoạt động & suất tổ chức</h2><div className="notice">Chưa có lịch hoạt động được công bố. Mục này sẽ được bổ sung trong giai đoạn tiếp theo.</div></article>
      <aside className="booking-panel"><div className="eyebrow">THÊM MỘT ĐIỂM ĐÁNG NHỚ</div><h2>Nơi này có trong chuyến đi của bạn?</h2><p>Lưu vào lịch trình, chọn thêm điểm và sắp xếp theo ý thích.</p><Link className="btn full" to={`/Planner?destinationId=${id}`}>Thêm vào lịch trình ↗</Link></aside></div></div>;
}
