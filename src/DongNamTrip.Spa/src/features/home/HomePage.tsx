import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router';
import { api } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Destination, Tour } from '../../api/types';
import { TourCard, Failure, Loading, art, regions, categories } from '../../components/Catalog';

export function HomePage() {
  const navigate = useNavigate();
  const [region, setRegion] = useState('');
  const [category, setCategory] = useState('');
  const [query, setQuery] = useState('');
  const tours = useLoad('home-tours', () => api.get<Tour[]>('proxy/tours'));
  const destinations = useLoad('home-destinations', () => api.get<Destination[]>('proxy/destinations'));
  function search(event: FormEvent) {
    event.preventDefault();
    const params = new URLSearchParams();
    if (region) params.set('region', region);
    if (category) params.set('category', category);
    if (query.trim()) params.set('q', query.trim());
    navigate('/Tours' + (params.size ? '?' + params : ''));
  }
  return <>
    <section className="hero"><div className="hero-art" aria-hidden="true"/><div className="container hero-content"><div className="hero-kicker"><span/> MỘT CHUYẾN ĐI, NHIỀU CÂU CHUYỆN</div>
      <h1>Đi gần thôi.<br/>Thấy <em>nhiều hơn.</em></h1><p>Từ nhịp phố Sài Gòn đến những con đường ra biển.<br/>Khám phá Đông Nam Bộ theo cách của bạn.</p>
      <div className="hero-actions"><Link className="btn" to="/Tours">Khám phá tour <span>↗</span></Link><Link className="hero-secondary" to="/Planner">Tự tạo chuyến đi <span>→</span></Link></div>
      <div className="hero-note"><span>05</span> khu vực khám phá <i/><span>∞</span> cảm hứng lên đường</div></div><div className="hero-caption">MỘT GÓC ĐÔNG NAM BỘ <span>Hình minh họa</span></div></section>
    <div className="container"><form className="search-bar" onSubmit={search}><label><span>BẠN MUỐN ĐI ĐÂU?</span><select value={region} onChange={event => setRegion(event.target.value)}><option value="">Tất cả khu vực</option>{regions.map(item => <option key={item}>{item}</option>)}</select></label>
      <label><span>TRẢI NGHIỆM YÊU THÍCH</span><select value={category} onChange={event => setCategory(event.target.value)}><option value="">Mọi trải nghiệm</option>{categories.map(item => <option key={item}>{item}</option>)}</select></label>
      <label><span>TÌM THEO TÊN</span><input value={query} onChange={event => setQuery(event.target.value)} placeholder="Một nơi bạn muốn đến…"/></label><button className="btn">Tìm chuyến đi ↗</button></form></div>
    <section className="section container"><div className="section-heading"><div><div className="eyebrow">CHẲNG CẦN ĐI THẬT XA</div><h2>Mỗi vùng đất, một sắc màu.</h2></div><Link className="text-link" to="/Destinations">Tất cả điểm đến ↗</Link></div>
      <div className="region-grid">{regions.map(regionName => <Link className="region-tile" key={regionName} to={`/Destinations?region=${encodeURIComponent(regionName)}`}><img src={art(destinations.data?.find(item => item.region === regionName)?.illustration || 'city')} alt=""/><span>{regionName} <b>↗</b></span></Link>)}</div></section>
    <section className="section soft"><div className="container"><div className="section-heading"><div><div className="eyebrow">GỢI Ý CHO NGÀY NGHỈ TỚI</div><h2>Xách balô, có lịch trình rồi.</h2></div><Link className="text-link" to="/Tours">Khám phá các tour ↗</Link></div>
      {tours.loading ? <Loading/> : tours.error ? <Failure message={tours.error} retry={tours.retry}/> : <div className="card-grid">{tours.data?.slice(0, 3).map(tour => <TourCard key={tour.id} tour={tour}/>)}</div>}</div></section>
    <section className="section container"><div className="planner-banner"><div><div className="eyebrow">CHUYẾN ĐI MANG DẤU ẤN RIÊNG</div><h2>Bạn chọn điểm đến.<br/>Hành trình là của bạn.</h2><p>Gom những nơi bạn thích, sắp xếp thứ tự và lưu lại ý tưởng cho chuyến đi tiếp theo.</p><Link className="btn" to="/Planner">Bắt đầu lên lịch trình ↗</Link></div>
      <div className="route-preview"><span className="route-label">MỘT NGÀY Ở SÀI GÒN</span><div><b>01</b><span>Bưu điện Trung tâm<small>Ngắm kiến trúc, gửi bưu thiếp</small></span></div><div><b>02</b><span>Chợ Bến Thành<small>Khám phá hương vị địa phương</small></span></div><p>Đi chậm một chút, nhớ lâu một chút.</p></div></div></section>
    <section className="container principles"><div><b>01 / Chọn cách đi</b><p>Theo tour có sẵn hoặc tự chọn điểm đến.</p></div><div><b>02 / Xem thật rõ</b><p>Lịch trình và giá tham khảo dễ theo dõi.</p></div><div><b>03 / Giữ cảm hứng</b><p>Lưu ý tưởng để tiếp tục hoàn thiện chuyến đi.</p></div></section>
  </>;
}
