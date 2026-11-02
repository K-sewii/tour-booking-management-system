import { useEffect } from 'react';
import { Link } from 'react-router';
import { api } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Paged, Tour } from '../../api/types';
import { useMeta } from '../../api/meta';
import { useAuth } from '../../auth/AuthContext';
import { Failure, Loading, Pagination, TourCard } from '../../components/Catalog';
import { recordActivity } from '../account/recordActivity';
import { useFilterForm } from '../useFilterForm';

const FILTERS = ['q', 'region', 'category', 'sort'] as const;

export function ToursPage() {
  const { params, applied: { q, region, category, sort }, form, setForm, page, submit } = useFilterForm(FILTERS);
  const { user } = useAuth();
  const meta = useMeta();
  const tours = useLoad('tours?' + params.toString(), () => api.get<Paged<Tour>>('proxy/tours/search?' + new URLSearchParams({ q, region, category, sort, page: String(page) })));
  const userId = user?.id;
  useEffect(() => { if (userId && q) recordActivity('search', undefined, q); }, [userId, q]);
  const items = tours.data?.items || [];
  const total = tours.data?.total ?? 0;
  return <><section className="page-intro"><div className="container"><div className="eyebrow">ĐÔNG NAM BỘ, THEO CÁCH CỦA BẠN</div><h1>Tìm một hành trình đáng nhớ.</h1><p>Biển xanh, phố thị hay một ngày giữa thiên nhiên?</p></div></section>
    <div className="container listing-layout section"><aside className="filter-panel"><h3>Chọn chuyến đi</h3><form onSubmit={submit}>
      <label>Tìm kiếm<input value={form.q} onChange={event => setForm({ ...form, q: event.target.value })} placeholder="Tên tour, trải nghiệm…"/></label>
      <label>Khu vực<select value={form.region} onChange={event => setForm({ ...form, region: event.target.value })}><option value="">Tất cả khu vực</option>{(meta.data?.regions || []).map(item => <option key={item.name}>{item.name}</option>)}</select></label>
      <label>Trải nghiệm<select value={form.category} onChange={event => setForm({ ...form, category: event.target.value })}><option value="">Tất cả trải nghiệm</option>{(meta.data?.categories || []).map(item => <option key={item}>{item}</option>)}</select></label>
      <label>Sắp xếp<select value={form.sort} onChange={event => setForm({ ...form, sort: event.target.value })}><option value="">Gợi ý cho bạn</option><option value="price-asc">Giá tăng dần</option><option value="price-desc">Giá giảm dần</option></select></label>
      <button className="btn full">Áp dụng bộ lọc</button><Link className="reset-link" to="/Tours">Xóa bộ lọc</Link></form><div className="filter-tip">Muốn đi theo cách riêng?<Link to="/Planner">Tự tạo chuyến đi →</Link></div></aside>
      <section>{tours.loading ? <Loading/> : tours.error ? <Failure message={tours.error} retry={tours.retry}/> : <><div className="results-line"><b>{total} hành trình</b><span>Giá mẫu để trải nghiệm giao diện</span></div>
        <div className="card-grid two">{items.map(item => <TourCard key={item.id} tour={item}/>)}</div>
        {!items.length && <div className="empty"><h2>Chưa tìm thấy tour phù hợp.</h2><p>Thử thay đổi từ khóa hoặc chọn khu vực khác.</p><Link className="btn" to="/Tours">Xem tất cả tour</Link></div>}
        {tours.data && <Pagination page={tours.data.page} total={tours.data.total} pageSize={tours.data.pageSize} params={params} path="/Tours"/>}</>}</section></div></>;
}

