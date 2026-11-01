import { useEffect, useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router';
import { api } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Destination } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { categories, DestinationCard, Failure, Loading, regions } from '../../components/Catalog';
import { recordActivity } from '../account/recordActivity';

export function DestinationsPage() {
  const [params, setParams] = useSearchParams();
  const [form, setForm] = useState({ q: params.get('q') || '', region: params.get('region') || '', category: params.get('category') || '' });
  const { user } = useAuth();
  const destinations = useLoad('destinations', () => api.get<Destination[]>('proxy/destinations'));
  const q = params.get('q')?.trim() || '';
  const region = params.get('region') || '';
  const category = params.get('category') || '';
  useEffect(() => { setForm({ q, region, category }); }, [q, region, category]);
  useEffect(() => { if (user && q) recordActivity('search', undefined, q); }, [user?.id, q]);
  const filtered = (destinations.data || []).filter(item => (!q || item.name.toLocaleLowerCase('vi-VN').includes(q.toLocaleLowerCase('vi-VN')))
    && (!region || item.region === region) && (!category || item.category === category));
  function submit(event: FormEvent) { event.preventDefault(); const next = new URLSearchParams(); Object.entries(form).forEach(([key, value]) => { if (value.trim()) next.set(key, value.trim()); }); setParams(next); }
  return <><section className="page-intro"><div className="container"><div className="eyebrow">CÓ MỘT NƠI ĐANG ĐỢI BẠN</div><h1>Điểm đến, nguồn cảm hứng.</h1><p>Khám phá năm khu vực du lịch trong phạm vi Đông Nam Bộ của đề tài.</p></div></section>
    <div className="container section"><form className="inline-filters" onSubmit={submit}><label>Tên địa điểm<input value={form.q} onChange={event => setForm({ ...form, q: event.target.value })} placeholder="Bạn đang tìm nơi nào?"/></label>
      <label>Khu vực<select value={form.region} onChange={event => setForm({ ...form, region: event.target.value })}><option value="">Tất cả khu vực</option>{regions.map(item => <option key={item}>{item}</option>)}</select></label>
      <label>Loại hình<select value={form.category} onChange={event => setForm({ ...form, category: event.target.value })}><option value="">Mọi loại hình</option>{categories.map(item => <option key={item}>{item}</option>)}</select></label><button className="btn">Tìm điểm đến</button><Link to="/Destinations">Xóa lọc</Link></form>
      {destinations.loading ? <Loading/> : destinations.error ? <Failure message={destinations.error} retry={destinations.retry}/> : <><div className="results-line"><b>{filtered.length} điểm đến</b><span>Hình minh họa theo khu vực</span></div>
        <div className="card-grid">{filtered.map(item => <DestinationCard destination={item} key={item.id}/>)}</div>{!filtered.length && <div className="empty"><h2>Chưa có điểm đến phù hợp.</h2><Link className="text-link" to="/Destinations">Xem tất cả điểm đến →</Link></div>}</>}</div></>;
}

