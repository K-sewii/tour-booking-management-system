import { Link } from 'react-router';
import type { Destination, Tour } from '../api/types';

export const regions = ['TP. Hồ Chí Minh', 'Tây Ninh', 'Vũng Tàu', 'Đồng Nai', 'Bình Dương'];
export const categories = ['Văn hóa', 'Thiên nhiên', 'Biển', 'Ẩm thực', 'Kết hợp'];
export const price = (amount: number) => `${new Intl.NumberFormat('vi-VN').format(amount)} ₫`;
export const dateTime = (value: string) => new Date(value).toLocaleString('vi-VN', { timeZone: 'UTC', hour12: false }) + ' UTC';
export const art = (illustration: string) => `/images/${illustration}.svg`;

export function TourCard({ tour }: { tour: Tour }) {
  const href = `/Tours/Details/${tour.id}`;
  return <article className="tour-card"><Link className="card-art" to={href}><img src={art(tour.illustration)} alt={`Minh họa ${tour.region}`} loading="lazy"/><span className="badge">{tour.category}</span></Link>
    <div className="card-body"><div className="eyebrow">{tour.region} <span>· {tour.duration}</span></div><h3><Link to={href}>{tour.name}</Link></h3><p>{tour.summary}</p>
      <div className="card-price"><div><small>Giá tham khảo / khách</small><strong>{price(tour.referencePrice)}</strong></div><Link className="circle-link" to={href} aria-label={`Xem tour ${tour.name}`}>↗</Link></div></div></article>;
}

export function DestinationCard({ destination }: { destination: Destination }) {
  return <article className="destination-card"><Link to={`/Destinations/Details/${destination.id}`}><img src={art(destination.illustration)} alt={`Minh họa ${destination.region}`} loading="lazy"/>
    <div><span className="eyebrow">{destination.region} · {destination.category}</span><h3>{destination.name}</h3><p>{destination.visitMinutes} phút tham quan dự kiến <span>↗</span></p></div></Link></article>;
}

export function Loading({ text = 'Đang tải dữ liệu…' }: { text?: string }) { return <p className="spa-state" role="status">{text}</p>; }
export function Failure({ message, retry }: { message: string; retry: () => void }) {
  return <div className="account-message account-message--error spa-state" role="alert">{message} <button className="btn outline" onClick={retry}>Thử lại</button></div>;
}
