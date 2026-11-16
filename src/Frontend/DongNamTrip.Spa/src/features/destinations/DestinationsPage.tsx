import { useEffect } from 'react';
import { Link } from 'react-router';
import { api } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Destination, Paged } from '../../api/types';
import { useMeta } from '../../api/meta';
import { useAuth } from '../../auth/AuthContext';
import { DestinationCard, Failure, Loading, Pagination } from '../../components/Catalog';
import { recordActivity } from '../account/recordActivity';
import { useFilterForm } from '../useFilterForm';

const FILTERS = ['q', 'region', 'category'] as const;

export function DestinationsPage() {
  const { params, applied: { q, region, category }, form, setForm, page, submit } = useFilterForm(FILTERS);
  const { user } = useAuth();
  const meta = useMeta();
  const destinations = useLoad('destinations?' + params.toString(), () => api.get<Paged<Destination>>('proxy/destinations/search?' + new URLSearchParams({ q, region, category, page: String(page) })));
  const userId = user?.id;
  useEffect(() => { if (userId && q) recordActivity('search', undefined, q); }, [userId, q]);
  return <><section className="page-intro"><div className="container"><div className="eyebrow">CÓ MỘT NƠI ĐANG ĐỢI BẠN</div><h1>Điểm đến, nguồn cảm hứng.</h1><p>Khám phá năm khu vực du lịch trong phạm vi Đông Nam Bộ của đề tài.</p></div></section>
    <div className="container section"><form className="inline-filters" onSubmit={submit}><label>Tên địa điểm<input value={form.q} onChange={event => setForm({ ...form, q: event.target.value })} placeholder="Bạn đang tìm nơi nào?"/></label>
      <label>Khu vực<select value={form.region} onChange={event => setForm({ ...form, region: event.target.value })}><option value="">Tất cả khu vực</option>{(meta.data?.regions || []).map(item => <option key={item.name}>{item.name}</option>)}</select></label>
      <label>Loại hình<select value={form.category} onChange={event => setForm({ ...form, category: event.target.value })}><option value="">Mọi loại hình</option>{(meta.data?.categories || []).map(item => <option key={item}>{item}</option>)}</select></label><button className="btn">Tìm điểm đến</button><Link to="/Destinations">Xóa lọc</Link></form>
      {destinations.loading ? <Loading/> : destinations.error ? <Failure message={destinations.error} retry={destinations.retry}/> : <><div className="results-line"><b>{destinations.data?.total ?? 0} điểm đến</b><span>Hình ảnh điểm đến</span></div>
        <div className="card-grid">{(destinations.data?.items || []).map(item => <DestinationCard destination={item} key={item.id}/>)}</div>{destinations.data && <Pagination page={destinations.data.page} total={destinations.data.total} pageSize={destinations.data.pageSize} params={params} path="/Destinations"/>}{!destinations.data?.items.length && <div className="empty"><h2>Chưa có điểm đến phù hợp.</h2><Link className="text-link" to="/Destinations">Xem tất cả điểm đến →</Link></div>}</>}</div></>;
}

