import { useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import { api, messageFor } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Itinerary, ItineraryCheck, Payment, QuoteRequest, QuoteRequestDetail } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { dateTime, Failure, Loading, price } from '../../components/Catalog';
import { AccountLayout } from '../../layouts/AccountLayout';

// Nhãn hiển thị cho trạng thái do Backend quy định (QuoteRequestStatus, QuoteStatus); mã lạ hiển thị nguyên mã.
const requestNames: Record<string, string> = { Submitted: 'Chờ tiếp nhận', Verifying: 'Đang xác minh', Quoted: 'Đã báo giá', Accepted: 'Đã chốt', Cancelled: 'Đã hủy' };
const quoteNames: Record<string, string> = { Sent: 'Đã gửi', Accepted: 'Đã chấp nhận', Expired: 'Hết hạn', Superseded: 'Đã thay thế' };

function RequestCard({ request, onChanged }: { request: QuoteRequest; onChanged: () => void }) {
  const detail = useLoad('quote-request-' + request.id + '-' + request.status + '-' + request.currentQuoteId, () => api.get<QuoteRequestDetail>('proxy/account/quote-requests/' + request.id));
  const [busy, setBusy] = useState(false);
  const [note, setNote] = useState('');
  const [agreed, setAgreed] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const quote = detail.data?.quote ?? null;
  const open = request.status === 'Submitted' || request.status === 'Verifying' || request.status === 'Quoted';

  async function act(path: string, body: unknown, done: string) {
    setMessage(''); setError(''); setBusy(true);
    try { await api.post(path, body); setMessage(done); onChanged(); detail.retry(); }
    catch (caught) { setError(messageFor(caught)); }
    finally { setBusy(false); }
  }

  return <article className="saved-trip"><span className="pill">Yêu cầu #{request.id} · {requestNames[request.status] ?? request.status}</span>
    <h2>Lịch trình #{request.itineraryId}</h2><p>Phiên bản {request.versionNumber} · {dateTime(request.sentAt)} · {request.guests} khách</p>
    {detail.loading ? <Loading/> : detail.error ? <Failure message={detail.error} retry={detail.retry}/> : <>
      {quote ? <div className="private-quote"><h3>Báo giá #{quote.id} · {quoteNames[quote.status] ?? quote.status}</h3><p>Tổng giá {price(quote.total)} · Cần thu {price(quote.deposit)}</p><p>Hiệu lực đến {dateTime(quote.validUntil)}</p>
        <ul>{quote.items.map((item, index) => <li key={index}>{item.description}: {item.quantity} {item.unit} × {price(item.unitPrice)}{item.includedInPackage ? ' (đã trong giá trọn gói)' : ''}</li>)}</ul><p>Điều kiện hủy: {quote.cancelPolicy}</p>
        {quote.status === 'Sent' && request.status === 'Quoted' && <label><input type="checkbox" checked={agreed} onChange={event => setAgreed(event.target.checked)}/> Tôi đã đọc và đồng ý với điều kiện hủy của báo giá</label>}
        {quote.status === 'Sent' && request.status === 'Quoted' && <button className="btn" type="button" disabled={busy || !agreed} onClick={() => void act(`proxy/account/quotes/${quote.id}/accept`, {}, 'Đã chấp nhận báo giá và tạo đơn.')}>Chấp nhận báo giá</button>}
        {(quote.status === 'Sent' || quote.status === 'Expired') && request.status === 'Quoted' && <form className="private-request-form" onSubmit={(event: FormEvent) => { event.preventDefault(); void act(`proxy/account/quotes/${quote.id}/change-request`, { note: note.trim() }, 'Đã gửi đề nghị đổi báo giá.'); setNote(''); }}>
          <label>Đề nghị đổi báo giá<textarea value={note} onChange={event => setNote(event.target.value)} maxLength={1000} required/></label><button className="btn outline" type="submit" disabled={busy || !note.trim()}>Gửi đề nghị đổi</button></form>}
        {detail.data?.orderId && <p className="notice">Đã tạo đơn #{detail.data.orderId}. <Link to={`/Account/Bookings/${detail.data.orderId}`}>Xem đơn</Link></p>}
      </div> : <p className="hint">{request.status === 'Verifying' ? 'Nhân viên đang xác minh dịch vụ cho yêu cầu này.' : 'Nhân viên chưa phát hành báo giá.'}</p>}
      {open && <button className="btn outline" type="button" disabled={busy} onClick={() => void act(`proxy/account/quote-requests/${request.id}/cancel`, {}, 'Đã hủy yêu cầu.')}>Hủy yêu cầu</button>}
    </>}
    {message && <p role="status" className="notice">{message}</p>}{error && <p role="alert" className="account-message account-message--error">{error}</p>}
  </article>;
}

export function TripsPage() {
  const { user } = useAuth();
  const [tab, setTab] = useState<'drafts' | 'bookings' | 'quotes'>('drafts');
  const [busy, setBusy] = useState<number | null>(null);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [checks, setChecks] = useState<Record<number, ItineraryCheck>>({});
  const trips = useLoad('my-trips-' + user?.id, () => api.get<Itinerary[]>('proxy/itineraries'));
  const payments = useLoad('my-trip-orders-' + user?.id, () => api.get<Payment[]>('proxy/account/payments'));
  const requests = useLoad('my-private-requests-' + user?.id, () => api.get<QuoteRequest[]>('proxy/account/quote-requests'));

  async function submitRequest(event: FormEvent<HTMLFormElement>, trip: Itinerary) {
    event.preventDefault(); setMessage(''); setError(''); setBusy(trip.id);
    const data = new FormData(event.currentTarget);
    try {
      const request = await api.post<QuoteRequest>('proxy/account/quote-requests', {
        itineraryId: trip.id, guests: trip.guests, contact: String(data.get('contact') || '').trim(), needs: String(data.get('serviceNeeds') || '').trim() || null,
      });
      setMessage(`Đã gửi yêu cầu tour riêng #${request.id}.`); setTab('quotes'); requests.retry();
    } catch (caught) { setError(messageFor(caught)); }
    finally { setBusy(null); }
  }

  async function checkTrip(tripId: number) {
    setMessage(''); setError('');
    try { const result = await api.get<ItineraryCheck>(`proxy/itineraries/${tripId}/check`); setChecks(current => ({ ...current, [tripId]: result })); }
    catch (caught) { setError(messageFor(caught)); }
  }

  return <AccountLayout><div className="account-heading"><div className="eyebrow">CHÀO {user?.name}</div><h1 id="account-title">Chuyến đi của tôi</h1><p>Tiếp tục từ một ý tưởng bạn đã lưu.</p></div>
    <div className="account-trips"><div className="tabs" role="tablist" aria-label="Loại chuyến đi">
      <button role="tab" className={tab === 'drafts' ? 'current' : ''} aria-selected={tab === 'drafts'} onClick={() => setTab('drafts')}>Lịch trình ({trips.data?.length || 0})</button>
      <button role="tab" className={tab === 'bookings' ? 'current' : ''} aria-selected={tab === 'bookings'} onClick={() => setTab('bookings')}>Đơn tour riêng</button>
      <button role="tab" className={tab === 'quotes' ? 'current' : ''} aria-selected={tab === 'quotes'} onClick={() => setTab('quotes')}>Yêu cầu & báo giá ({requests.data?.length || 0})</button></div>
      {message && <p role="status" className="notice">{message}</p>}{error && <p role="alert" className="account-message account-message--error">{error}</p>}
      {tab === 'drafts' && (trips.loading ? <Loading/> : trips.error ? <Failure message={trips.error} retry={trips.retry}/> : !trips.data?.length ?
        <div className="empty"><span className="empty-icon">↗</span><h2>Chuyến đi tiếp theo bắt đầu ở đây.</h2><p>Bạn chưa lưu lịch trình nào. Hãy chọn vài điểm đến yêu thích.</p><Link className="btn" to="/Planner">Tạo lịch trình đầu tiên</Link></div>
        : <div className="card-grid">{trips.data.map(trip => <article className="saved-trip" key={trip.id}><span className="pill">Lịch trình #{trip.id}</span><h2>{trip.name}</h2><p>{trip.startDate} · {trip.guests} khách</p><ol>{trip.destinationNames.map((name, index) => <li key={index}>{name}</li>)}</ol>
          {trip.warnings.map((warning, index) => <p className="hint" key={index}>{warning}</p>)}
          {checks[trip.id] && <p className="hint">{checks[trip.id].fits ? 'Lịch phù hợp' : 'Lịch còn cảnh báo'}{checks[trip.id].complete ? '' : ' (chưa đủ dữ liệu để kết luận)'}: {checks[trip.id].warnings.length === 0 ? 'không có cảnh báo.' : checks[trip.id].warnings.map(warning => warning.message).join(' ')}</p>}
          <button className="btn outline" type="button" onClick={() => void checkTrip(trip.id)}>Kiểm tra lịch</button> <Link className="btn outline" to={`/Planner?itineraryId=${trip.id}`}>Sửa lịch trình</Link>
          <form className="private-request-form" onSubmit={event => void submitRequest(event, trip)}><label>Thông tin liên hệ<input name="contact" required maxLength={250} placeholder="Số điện thoại hoặc email"/></label><label>Nhu cầu dịch vụ<textarea name="serviceNeeds" maxLength={2000} placeholder="Chỗ ở, ăn uống, phương tiện…"/></label><button className="btn" type="submit" disabled={busy !== null}>{busy === trip.id ? 'Đang gửi…' : 'Gửi yêu cầu tour riêng'}</button></form>
        </article>)}</div>)}
      {tab === 'bookings' && (payments.loading ? <Loading/> : payments.error ? <Failure message={payments.error} retry={payments.retry}/> : !payments.data?.length ? <div className="empty"><h2>Chưa có đơn nào.</h2><p>Đơn tour riêng xuất hiện sau khi bạn chấp nhận báo giá còn hiệu lực.</p><Link className="btn" to="/Planner">Tạo lịch trình</Link></div>
        : <div className="account-card"><ul className="account-list">{payments.data.map(order => <li key={order.id}><div><strong>Đơn #{order.id} · {order.status}</strong><p>{dateTime(order.placedAt)} · Giá chốt {price(order.agreedPrice)}</p></div><Link className="btn outline" to={`/Account/Bookings/${order.id}`}>Xem đơn</Link></li>)}</ul></div>)}
      {tab === 'quotes' && (requests.loading ? <Loading/> : requests.error ? <Failure message={requests.error} retry={requests.retry}/> : !requests.data?.length ? <div className="empty"><h2>Chưa có yêu cầu tour riêng.</h2><p>Mở một lịch trình và gửi nhu cầu để nhân viên chuẩn bị báo giá.</p><button className="btn" onClick={() => setTab('drafts')}>Xem lịch trình</button></div>
        : <div className="card-grid">{requests.data.map(request => <RequestCard key={request.id} request={request} onChanged={() => { requests.retry(); payments.retry(); }}/>)}</div>)}
      <p className="hint">Suất trong lịch trình là mong muốn, chưa giữ chỗ. Khoản cần thu trên báo giá chưa phải giao dịch thanh toán.</p></div>
    </AccountLayout>;
}
