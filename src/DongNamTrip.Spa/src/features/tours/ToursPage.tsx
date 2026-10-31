import { useEffect, useMemo, useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router';
import { api } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Tour } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { categories, Failure, Loading, regions, TourCard } from '../../components/Catalog';
import { recordActivity } from '../account/recordActivity';

export function ToursPage() {
  const [params, setParams] = useSearchParams();
  const [form, setForm] = useState({ q: params.get('q') || '', region: params.get('region') || '', category: params.get('category') || '', sort: params.get('sort') || '' });
  const { user } = useAuth();
  const tours = useLoad('tours', () => api.get<Tour[]>('proxy/tours'));
  const q = params.get('q')?.trim() || '';
  const region = params.get('region') || '';
  const category = params.get('category') || '';
  const sort = params.get('sort') || '';
  const page = Math.max(1, Number(params.get('page') || 1) || 1);
  useEffect(() => { setForm({ q, region, category, sort }); }, [q, region, category, sort]);
  useEffect(() => { if (user && q) recordActivity('search', undefined, q); }, [user?.id, q]);
  const filtered = useMemo(() => {
    let items = (tours.data || []).filter(item => (!q || `${item.name} ${item.summary}`.toLocaleLowerCase('vi-VN').includes(q.toLocaleLowerCase('vi-VN')))
      && (!region || item.region === region) && (!category || item.category === category));
    items = [...items].sort((a, b) => sort === 'price-asc' ? a.referencePrice - b.referencePrice : sort === 'price-desc' ? b.referencePrice - a.referencePrice : a.id - b.id);
    return items;
  }, [tours.data, q, region, category, sort]);
  const totalPages = Math.max(1, Math.ceil(filtered.length / 4));
  const currentPage = Math.min(page, totalPages);
  function submit(event: FormEvent) { event.preventDefault(); const next = new URLSearchParams(); Object.entries(form).forEach(([key, value]) => { if (value.trim()) next.set(key, value.trim()); }); setParams(next); }
  function pageLink(number: number) { const next = new URLSearchParams(params); next.set('page', String(number)); return `/Tours?${next}`; }
  return <><section className="page-intro"><div className="container"><div className="eyebrow">ĐÔNG NAM BỘ, THEO CÁCH CỦA BẠN</div><h1>Tìm một hành trình đáng nhớ.</h1><p>Biển xanh, phố thị hay một ngày giữa thiên nhiên?</p></div></section>
    <div className="container listing-layout section"><aside className="filter-panel"><h3>Chọn chuyến đi</h3><form onSubmit={submit}>
      <label>Tìm kiếm<input value={form.q} onChange={event => setForm({ ...form, q: event.target.value })} placeholder="Tên tour, trải nghiệm…"/></label>
      <label>Khu vực<select value={form.region} onChange={event => setForm({ ...form, region: event.target.value })}><option value="">Tất cả khu vực</option>{regions.map(item => <option key={item}>{item}</option>)}</select></label>
      <label>Trải nghiệm<select value={form.category} onChange={event => setForm({ ...form, category: event.target.value })}><option value="">Tất cả trải nghiệm</option>{categories.map(item => <option key={item}>{item}</option>)}</select></label>
      <label>Sắp xếp<select value={form.sort} onChange={event => setForm({ ...form, sort: event.target.value })}><option value="">Gợi ý cho bạn</option><option value="price-asc">Giá tăng dần</option><option value="price-desc">Giá giảm dần</option></select></label>
      <button className="btn full">Áp dụng bộ lọc</button><Link className="reset-link" to="/Tours">Xóa bộ lọc</Link></form><div className="filter-tip">Muốn đi theo cách riêng?<Link to="/Planner">Tự tạo chuyến đi →</Link></div></aside>
      <section>{tours.loading ? <Loading/> : tours.error ? <Failure message={tours.error} retry={tours.retry}/> : <><div className="results-line"><b>{filtered.length} hành trình</b><span>Giá mẫu để trải nghiệm giao diện</span></div>
        <div className="card-grid two">{filtered.slice((currentPage - 1) * 4, currentPage * 4).map(item => <TourCard key={item.id} tour={item}/>)}</div>
        {!filtered.length && <div className="empty"><h2>Chưa tìm thấy tour phù hợp.</h2><p>Thử thay đổi từ khóa hoặc chọn khu vực khác.</p><Link className="btn" to="/Tours">Xem tất cả tour</Link></div>}
        <nav className="pagination" aria-label="Phân trang">{Array.from({ length: totalPages }, (_, index) => <Link className={index + 1 === currentPage ? 'current' : ''} key={index} to={pageLink(index + 1)}>{index + 1}</Link>)}</nav></>}</section></div></>;
}

