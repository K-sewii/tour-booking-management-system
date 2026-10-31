import { useEffect, useState, type FormEvent } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router';
import { api, messageFor } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Destination, Itinerary, Tour } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { art, Failure, Loading } from '../../components/Catalog';

type Draft = { name: string; startDate: string; guests: number; ids: number[] };
const draftKey = 'dongnamtrip.planner.v1';
const nextWeek = () => new Date(Date.now() + 7 * 86400000).toISOString().slice(0, 10);
function savedDraft(): Draft {
  try {
    const source = JSON.parse(sessionStorage.getItem(draftKey) || '{}') as Partial<Draft> & { Name?: string; StartDate?: string; Guests?: number };
    return { name: source.name || source.Name || '', startDate: source.startDate || source.StartDate || nextWeek(), guests: source.guests || source.Guests || 2, ids: Array.isArray(source.ids) ? source.ids : [] };
  } catch { return { name: '', startDate: nextWeek(), guests: 2, ids: [] }; }
}

export function PlannerPage() {
  const [params] = useSearchParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const destinations = useLoad('planner-places', () => api.get<Destination[]>('proxy/destinations'));
  const tours = useLoad('planner-tours', () => api.get<Tour[]>('proxy/tours'));
  const [draft, setDraft] = useState<Draft>(savedDraft);
  const [search, setSearch] = useState('');
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const tourId = Number(params.get('tourId'));
  const destinationId = Number(params.get('destinationId'));
  useEffect(() => {
    if (!destinations.data || !tours.data) return;
    const selected = tourId ? tours.data.find(item => item.id === tourId)?.destinationIds : destinationId ? [destinationId] : null;
    if (selected?.length) setDraft(current => ({ ...current, ids: selected.filter(id => destinations.data!.some(item => item.id === id)).slice(0, 12) }));
  }, [tourId, destinationId, destinations.data, tours.data]);
  useEffect(() => { sessionStorage.setItem(draftKey, JSON.stringify(draft)); }, [draft]);
  const catalog = destinations.data || [];
  const visible = catalog.filter(place => `${place.name} ${place.region}`.toLocaleLowerCase('vi-VN').includes(search.toLocaleLowerCase('vi-VN')));
  const selected = draft.ids.map(id => catalog.find(item => item.id === id)).filter((item): item is Destination => Boolean(item));
  function updateIds(ids: number[]) { setDraft(current => ({ ...current, ids })); }
  function move(index: number, delta: number) { const ids = [...draft.ids]; [ids[index], ids[index + delta]] = [ids[index + delta], ids[index]]; updateIds(ids); }
  async function submit(event: FormEvent) {
    event.preventDefault(); setError('');
    if (!selected.length) { setError('Bạn cần chọn ít nhất một địa điểm.'); return; }
    if (!draft.name.trim() || draft.name.trim().length > 100 || !draft.startDate || draft.startDate < new Date().toISOString().slice(0, 10) || draft.guests < 1 || draft.guests > 40) {
      setError('Nhập tên, ngày đi hợp lệ và số khách từ 1 đến 40.'); return;
    }
    if (!user) { setError('Đăng nhập để lưu lịch trình. Bản nháp được giữ trong tab này.'); return; }
    setBusy(true);
    try {
      await api.post<Itinerary>('proxy/itineraries', { name: draft.name.trim(), startDate: draft.startDate, guests: draft.guests, destinationIds: draft.ids });
      sessionStorage.removeItem(draftKey); navigate('/Trips');
    } catch (caught) { setError(messageFor(caught)); }
    finally { setBusy(false); }
  }
  return <><section className="page-intro compact"><div className="container"><div className="eyebrow">LÊN Ý TƯỞNG, RỒI LÊN ĐƯỜNG</div><h1>Chuyến đi của riêng bạn.</h1><p>Chọn điểm đến, đổi thứ tự và lưu lại hành trình yêu thích.</p></div></section>
    <div className="container section" id="planner"><div className="planner-steps"><b>01 Chọn điểm đến</b><span>→</span><b>02 Sắp xếp lịch trình</b><span>→</span><b>03 Lưu ý tưởng</b></div>
      <div className="planner-layout"><section><div className="section-heading"><h2>Thêm nơi bạn muốn đến</h2><label className="sr-only" htmlFor="place-search">Tìm địa điểm</label><input id="place-search" value={search} onChange={event => setSearch(event.target.value)} placeholder="Tìm tên hoặc khu vực…"/></div>
        {destinations.loading ? <Loading/> : destinations.error ? <Failure message={destinations.error} retry={destinations.retry}/> : <div id="place-list">{visible.map(place => <article className="place-row" key={place.id}><img src={art(place.illustration)} alt=""/><div><small>{place.region} · {place.visitMinutes} phút</small><h3>{place.name}</h3><span>{place.category}</span></div>
          <button type="button" className="add-place" aria-label={`Thêm ${place.name}`} disabled={draft.ids.includes(place.id) || draft.ids.length >= 12} onClick={() => updateIds([...draft.ids, place.id])}>{draft.ids.includes(place.id) ? '✓' : '+'}</button></article>)}{!visible.length && <p>Không có địa điểm phù hợp với từ khóa.</p>}</div>}</section>
        <aside className="itinerary-panel"><div className="section-heading"><h2>Lịch trình của bạn</h2><span className="pill">{selected.length} điểm</span></div><form onSubmit={event => void submit(event)}>
          <label>Tên chuyến đi<input required maxLength={100} value={draft.name} onChange={event => setDraft({ ...draft, name: event.target.value })} placeholder="Cuối tuần đi đâu đó…"/></label>
          <div className="form-row"><label>Ngày dự kiến<input type="date" required min={new Date().toISOString().slice(0, 10)} value={draft.startDate} onChange={event => setDraft({ ...draft, startDate: event.target.value })}/></label>
            <label>Số khách<input type="number" min="1" max="40" required value={draft.guests} onChange={event => setDraft({ ...draft, guests: Number(event.target.value) })}/></label></div>
          <ol id="selected-places">{selected.map((place, index) => <li key={place.id}><span>{index + 1}. {place.name}<small>{place.visitMinutes} phút · {place.region}</small></span>
            <button type="button" aria-label={`Đưa ${place.name} lên`} disabled={index === 0} onClick={() => move(index, -1)}>↑</button><button type="button" aria-label={`Đưa ${place.name} xuống`} disabled={index === selected.length - 1} onClick={() => move(index, 1)}>↓</button><button type="button" aria-label={`Xóa ${place.name}`} onClick={() => updateIds(draft.ids.filter(id => id !== place.id))}>×</button></li>)}</ol>
          {!selected.length && <div className="planner-empty"><span>↖</span><b>Bắt đầu từ một nơi bạn thích.</b><p>Nhấn dấu + để thêm điểm đầu tiên.</p></div>}
          <p className="hint">Tổng tham quan: {selected.reduce((sum, place) => sum + place.visitMinutes, 0)} phút. Chưa bao gồm di chuyển.</p>
          <p className="notice small-notice">Đây là lịch trình nháp. Chưa kiểm tra tuyến đường, trùng giờ hoặc khả năng cung ứng dịch vụ.</p>
          <button type="submit" className="btn full" disabled={busy}>{busy ? 'Đang lưu lịch trình…' : 'Lưu lịch trình ↗'}</button><p id="planner-message" role="status" aria-live="polite">{error}</p>
          {!user && error && <Link className="text-link" to="/Account/Login?returnUrl=%2FPlanner">Đăng nhập để lưu →</Link>}
          <button type="button" className="reset-link" onClick={() => { setDraft({ name: '', startDate: nextWeek(), guests: 2, ids: [] }); setError(''); }}>Làm mới lịch trình</button>
        </form></aside></div></div></>;
}

