import { useEffect, useState, type FormEvent } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router';
import { api, messageFor } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Destination, Itinerary, ItineraryPreview, Paged, PlannerCatalog } from '../../api/types';
import { loadMeta, useMeta } from '../../api/meta';
import { useAuth } from '../../auth/AuthContext';
import { art, Failure, Loading } from '../../components/Catalog';
import { dateOnDay } from './plan';

type StopDetail = { day: number; note: string; slotIds: number[] };
type Draft = { name: string; startDate: string; guests: number; ids: number[]; departurePoint: string; transport: string; budget: string; details: Record<number, StopDetail> };
const draftKey = 'dongnamtrip.planner.v1';
// Ngày theo giờ máy người dùng (sv-SE cho dạng YYYY-MM-DD); toISOString() là giờ UTC nên lệch ngày trước 7h sáng ở Việt Nam.
const localDate = (days = 0) => new Date(Date.now() + days * 86400000).toLocaleDateString('sv-SE');
const nextWeek = () => localDate(7);
function savedDraft(): Draft {
  try {
    const source = JSON.parse(sessionStorage.getItem(draftKey) || '{}') as Partial<Draft> & { Name?: string; StartDate?: string; Guests?: number };
    return { name: source.name || source.Name || '', startDate: source.startDate || source.StartDate || nextWeek(), guests: source.guests || source.Guests || 2, ids: Array.isArray(source.ids) ? source.ids : [], departurePoint: source.departurePoint || '', transport: source.transport || '', budget: source.budget || '', details: source.details || {} };
  } catch { return { name: '', startDate: nextWeek(), guests: 2, ids: [], departurePoint: '', transport: '', budget: '', details: {} }; }
}

export function PlannerPage() {
  const [params] = useSearchParams();
  const { user } = useAuth();
  const navigate = useNavigate();
  const meta = useMeta();
  const [draft, setDraft] = useState<Draft>(savedDraft);
  const [search, setSearch] = useState('');
  const catalog = useLoad('planner-catalog', () => api.get<PlannerCatalog>('proxy/planner/catalog'));
  const destinations = useLoad('planner-places-' + search, async () => api.get<Paged<Destination>>('proxy/destinations/search?' + new URLSearchParams({ q: search, pageSize: String((await loadMeta()).plannerListSize) })));
  const preview = useLoad('planner-preview-' + draft.ids.join(','), () => draft.ids.length
    ? api.get<ItineraryPreview>('proxy/itineraries/preview?' + new URLSearchParams(draft.ids.map(id => ['ids', String(id)])))
    : Promise.resolve<ItineraryPreview>({ places: [], totalVisitMinutes: 0 }));
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const tourId = Number(params.get('tourId'));
  const destinationId = Number(params.get('destinationId'));
  const itineraryId = Number(params.get('itineraryId'));
  useEffect(() => {
    if (itineraryId) api.get<Itinerary>('proxy/itineraries/' + itineraryId).then(found => setDraft({ name: found.name, startDate: found.startDate, guests: found.guests, ids: found.destinationIds, departurePoint: found.departurePoint || '', transport: found.transport || '', budget: found.budget?.toString() || '', details: Object.fromEntries(found.stops.map(stop => [stop.destinationId, { day: stop.day, note: stop.note || '', slotIds: stop.slotIds }])) })).catch(caught => setError(messageFor(caught)));
    else if (tourId) api.get<{ destinationId: number; day: number; order: number }[]>('proxy/planner/tours/' + tourId).then(stops => setDraft(current => ({ ...current, ids: stops.map(stop => stop.destinationId), details: Object.fromEntries(stops.map(stop => [stop.destinationId, { day: stop.day, note: '', slotIds: [] }])) }))).catch(caught => setError(messageFor(caught)));
    else if (destinationId) setDraft(current => ({ ...current, ids: [destinationId] }));
  }, [tourId, destinationId, itineraryId]);
  useEffect(() => { sessionStorage.setItem(draftKey, JSON.stringify(draft)); }, [draft]);
  const visible = destinations.data?.items || [];
  const selected = preview.data?.places || [];
  function detail(id: number): StopDetail { return draft.details[id] || { day: 1, note: '', slotIds: [] }; }
  function updateDetail(id: number, change: Partial<StopDetail>) { setDraft(current => ({ ...current, details: { ...current.details, [id]: { ...(current.details[id] || { day: 1, note: '', slotIds: [] }), ...change } } })); }
  function updateIds(ids: number[]) { setDraft(current => ({ ...current, ids })); }
  function move(index: number, delta: number) { const ids = [...draft.ids]; [ids[index], ids[index + delta]] = [ids[index + delta], ids[index]]; updateIds(ids); }
  async function submit(event: FormEvent) {
    event.preventDefault(); setError('');
    if (!draft.ids.length) { setError('Bạn cần chọn ít nhất một địa điểm.'); return; }
    const rules = meta.data;
    if (!rules || !draft.name.trim() || draft.name.trim().length > rules.itineraryNameMax || !draft.startDate || draft.startDate < localDate() || draft.guests < rules.guestsMin || draft.guests > rules.guestsMax) {
      setError(`Nhập tên, ngày đi hợp lệ và số khách từ ${rules?.guestsMin} đến ${rules?.guestsMax}.`); return;
    }
    if (!user) { setError('Đăng nhập để lưu lịch trình. Bản nháp được giữ trong tab này.'); return; }
    setBusy(true);
    try {
      const stops = draft.ids.map((id, index) => ({ destinationId: id, day: detail(id).day, order: index + 1, note: detail(id).note, slotIds: detail(id).slotIds }));
      const body = { name: draft.name.trim(), startDate: draft.startDate, guests: draft.guests, destinationIds: draft.ids, departurePoint: draft.departurePoint || null, transport: draft.transport || null, budget: draft.budget ? Number(draft.budget) : null, stops };
      if (itineraryId) await api.put<Itinerary>('proxy/itineraries/' + itineraryId, body);
      else await api.post<Itinerary>('proxy/itineraries', body);
      sessionStorage.removeItem(draftKey); navigate('/Trips');
    } catch (caught) { setError(messageFor(caught)); }
    finally { setBusy(false); }
  }
  return <><section className="page-intro compact"><div className="container"><div className="eyebrow">LÊN Ý TƯỞNG, RỒI LÊN ĐƯỜNG</div><h1>Chuyến đi của riêng bạn.</h1><p>Chọn điểm đến, đổi thứ tự và lưu lại hành trình yêu thích.</p></div></section>
    <div className="container section" id="planner"><div className="planner-steps"><b>01 Chọn điểm đến</b><span>→</span><b>02 Sắp xếp lịch trình</b><span>→</span><b>03 Lưu ý tưởng</b></div>
      <div className="planner-layout"><section><div className="section-heading"><h2>Thêm nơi bạn muốn đến</h2><label className="sr-only" htmlFor="place-search">Tìm địa điểm</label><input id="place-search" value={search} onChange={event => setSearch(event.target.value)} placeholder="Tìm tên hoặc khu vực…"/></div>
        {destinations.loading ? <Loading/> : destinations.error ? <Failure message={destinations.error} retry={destinations.retry}/> : <div id="place-list">{visible.map(place => <article className="place-row" key={place.id}><img src={art(place.illustration)} alt=""/><div><small>{place.region} · {place.visitMinutes} phút</small><h3>{place.name}</h3><span>{place.category}</span></div>
          <button type="button" className="add-place" aria-label={`Thêm ${place.name}`} disabled={draft.ids.includes(place.id) || draft.ids.length >= (meta.data?.maxPlaces ?? 0)} onClick={() => updateIds([...draft.ids, place.id])}>{draft.ids.includes(place.id) ? '✓' : '+'}</button></article>)}{!visible.length && <p>Không có địa điểm phù hợp với từ khóa.</p>}</div>}</section>
        <aside className="itinerary-panel"><div className="section-heading"><h2>Lịch trình của bạn</h2><span className="pill">{selected.length} điểm</span></div><form onSubmit={event => void submit(event)}>
          <label>Tên chuyến đi<input required maxLength={meta.data?.itineraryNameMax} value={draft.name} onChange={event => setDraft({ ...draft, name: event.target.value })} placeholder="Cuối tuần đi đâu đó…"/></label>
          <div className="form-row"><label>Ngày dự kiến<input type="date" required min={localDate()} value={draft.startDate} onChange={event => setDraft({ ...draft, startDate: event.target.value, details: Object.fromEntries(Object.entries(draft.details).map(([id, value]) => [id, { ...value, slotIds: [] }])) })}/></label>
            <label>Số khách<input type="number" min={meta.data?.guestsMin} max={meta.data?.guestsMax} required value={draft.guests} onChange={event => setDraft({ ...draft, guests: Number(event.target.value) })}/></label></div>
          <div className="form-row"><label>Nơi xuất phát<input maxLength={250} value={draft.departurePoint} onChange={event => setDraft({ ...draft, departurePoint: event.target.value })}/></label><label>Phương tiện<input maxLength={50} value={draft.transport} onChange={event => setDraft({ ...draft, transport: event.target.value })}/></label></div>
          <label>Ngân sách dự kiến (VNĐ)<input type="number" min="0" value={draft.budget} onChange={event => setDraft({ ...draft, budget: event.target.value })}/></label>
          {catalog.error && <Failure message={catalog.error} retry={catalog.retry}/>}
          <ol id="selected-places">{selected.map((place, index) => <li key={place.id}><span>{index + 1}. {place.name}<small>{place.visitMinutes} phút · {place.region}</small></span>
            <button type="button" aria-label={`Đưa ${place.name} lên`} disabled={index === 0} onClick={() => move(index, -1)}>↑</button><button type="button" aria-label={`Đưa ${place.name} xuống`} disabled={index === selected.length - 1} onClick={() => move(index, 1)}>↓</button><button type="button" aria-label={`Xóa ${place.name}`} onClick={() => updateIds(draft.ids.filter(id => id !== place.id))}>×</button>
            <div className="stop-options"><label>Ngày thứ<input type="number" min="1" max="30" value={detail(place.id).day} onChange={event => updateDetail(place.id, { day: Number(event.target.value), slotIds: [] })}/></label><label>Ghi chú<input maxLength={500} value={detail(place.id).note} onChange={event => updateDetail(place.id, { note: event.target.value })}/></label>
              <fieldset><legend>Suất mong muốn</legend>{catalog.data?.slots.filter(slot => slot.destinationId === place.id && slot.date === dateOnDay(draft.startDate, detail(place.id).day)).map(slot => <label key={slot.id}><input type="checkbox" checked={detail(place.id).slotIds.includes(slot.id)} onChange={event => updateDetail(place.id, { slotIds: event.target.checked ? [...detail(place.id).slotIds, slot.id] : detail(place.id).slotIds.filter(id => id !== slot.id) })}/>{catalog.data?.activities.find(activity => activity.id === slot.activityId)?.name || `Hoạt động #${slot.activityId}`} · {slot.startsAt}–{slot.endsAt}</label>)}{!catalog.data?.slots.some(slot => slot.destinationId === place.id && slot.date === dateOnDay(draft.startDate, detail(place.id).day)) && <small>Chưa có suất công bố cho ngày này.</small>}</fieldset>
            </div></li>)}</ol>
          {!selected.length && <div className="planner-empty"><span>↖</span><b>Bắt đầu từ một nơi bạn thích.</b><p>Nhấn dấu + để thêm điểm đầu tiên.</p></div>}
          <p className="hint">Tổng tham quan: {preview.data?.totalVisitMinutes ?? 0} phút. Chưa bao gồm di chuyển.</p>
          <p className="notice small-notice">Suất là mong muốn, chưa giữ chỗ. Điều hành sẽ xác minh trước khi báo giá.</p>
          <button type="submit" className="btn full" disabled={busy}>{busy ? 'Đang lưu lịch trình…' : itineraryId ? 'Cập nhật lịch trình ↗' : 'Lưu lịch trình ↗'}</button><p id="planner-message" role="status" aria-live="polite">{error}</p>
          {!user && error && <Link className="text-link" to="/Account/Login?returnUrl=%2FPlanner">Đăng nhập để lưu →</Link>}
          <button type="button" className="reset-link" onClick={() => { setDraft({ name: '', startDate: nextWeek(), guests: 2, ids: [], departurePoint: '', transport: '', budget: '', details: {} }); setError(''); }}>Làm mới lịch trình</button>
        </form></aside></div></div></>;
}

