import { useState, type FormEvent } from 'react';
import { Link } from 'react-router';
import { api, messageFor } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Itinerary, Payment, PrivateTourRequest } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { dateTime, Failure, Loading, price } from '../../components/Catalog';
import { AccountLayout } from '../../layouts/AccountLayout';

export function TripsPage() {
  const { user } = useAuth();
  const [tab, setTab] = useState<'drafts' | 'bookings' | 'quotes'>('drafts');
  const [busy, setBusy] = useState<number | null>(null);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const trips = useLoad('my-trips-' + user?.id, () => api.get<Itinerary[]>('proxy/itineraries'));
  const payments = useLoad('my-trip-orders-' + user?.id, () => api.get<Payment[]>('proxy/account/payments'));
  const requests = useLoad('my-private-requests-' + user?.id, () => api.get<PrivateTourRequest[]>('proxy/private-tour-requests'));

  async function submitRequest(event: FormEvent<HTMLFormElement>, tripId: number) {
    event.preventDefault(); setMessage(''); setError(''); setBusy(tripId);
    const data = new FormData(event.currentTarget);
    try {
      const request = await api.post<PrivateTourRequest>('proxy/itineraries/' + tripId + '/requests', {
        contact: String(data.get('contact') || '').trim(), serviceNeeds: String(data.get('serviceNeeds') || '').trim(),
      });
      setMessage(`Đã gửi yêu cầu tour riêng #${request.id}.`); setTab('quotes'); requests.retry();
    } catch (caught) { setError(messageFor(caught)); }
    finally { setBusy(null); }
  }

  async function acceptQuote(requestId: number, quoteId: number) {
    setMessage(''); setError(''); setBusy(quoteId);
    try {
      const order = await api.post<{ id: number }>(`proxy/private-tour-requests/${requestId}/quotes/${quoteId}/accept`, {});
      setMessage(`Đã chấp nhận báo giá và tạo đơn #${order.id}.`);
      requests.retry(); payments.retry();
    } catch (caught) { setError(messageFor(caught)); }
    finally { setBusy(null); }
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
          {trip.warnings.map((warning, index) => <p className="hint" key={index}>{warning}</p>)}<Link className="btn outline" to={`/Planner?itineraryId=${trip.id}`}>Sửa lịch trình</Link>
          <form className="private-request-form" onSubmit={event => void submitRequest(event, trip.id)}><label>Thông tin liên hệ<input name="contact" required maxLength={250} placeholder="Số điện thoại hoặc email"/></label><label>Nhu cầu dịch vụ<textarea name="serviceNeeds" maxLength={2000} placeholder="Chỗ ở, ăn uống, phương tiện…"/></label><button className="btn" type="submit" disabled={busy !== null}>{busy === trip.id ? 'Đang gửi…' : 'Gửi yêu cầu tour riêng'}</button></form>
        </article>)}</div>)}
      {tab === 'bookings' && (payments.loading ? <Loading/> : payments.error ? <Failure message={payments.error} retry={payments.retry}/> : !payments.data?.length ? <div className="empty"><h2>Chưa có đơn nào.</h2><p>Đơn tour riêng xuất hiện sau khi bạn chấp nhận báo giá còn hiệu lực.</p><Link className="btn" to="/Planner">Tạo lịch trình</Link></div>
        : <div className="account-card"><ul className="account-list">{payments.data.map(order => <li key={order.id}><div><strong>Đơn #{order.id} · {order.status}</strong><p>{dateTime(order.placedAt)} · Giá chốt {price(order.agreedPrice)}</p></div><Link className="btn outline" to={`/Account/Payments/${order.id}`}>Xem giao dịch</Link></li>)}</ul></div>)}
      {tab === 'quotes' && (requests.loading ? <Loading/> : requests.error ? <Failure message={requests.error} retry={requests.retry}/> : !requests.data?.length ? <div className="empty"><h2>Chưa có yêu cầu tour riêng.</h2><p>Mở một lịch trình và gửi nhu cầu để nhân viên chuẩn bị báo giá.</p><button className="btn" onClick={() => setTab('drafts')}>Xem lịch trình</button></div>
        : <div className="card-grid">{requests.data.map(request => <article className="saved-trip" key={request.id}><span className="pill">Yêu cầu #{request.id} · {request.status}</span><h2>{request.snapshot.name}</h2><p>Phiên bản {request.version} · {request.snapshot.startDate} · {request.snapshot.guests} khách</p><p>{request.serviceNeeds || 'Chưa ghi nhu cầu bổ sung.'}</p>
          {request.verifications.map(check => <p className="hint" key={check.id}>{check.item}: {check.result}</p>)}
          {request.quotes.map(quote => <div className="private-quote" key={quote.id}><h3>Báo giá #{quote.id} · {quote.status}</h3><p>Tổng giá {price(quote.total)} · Cần thu {price(quote.amountDue)}</p><p>Hiệu lực đến {dateTime(quote.expiresAt)}</p><ul>{quote.lines.map(line => <li key={line.id}>{line.description}: {line.quantity} {line.unit} × {price(line.unitPrice)}</li>)}</ul><p>Điều kiện hủy: {quote.cancellationTerms}</p>{quote.status === 'Issued' && <button className="btn" type="button" disabled={busy !== null} onClick={() => void acceptQuote(request.id, quote.id)}>{busy === quote.id ? 'Đang chấp nhận…' : 'Chấp nhận báo giá'}</button>}{quote.orderId && <p className="notice">Đã tạo đơn #{quote.orderId}.</p>}</div>)}
          {!request.quotes.length && <p className="hint">Nhân viên chưa phát hành báo giá.</p>}</article>)}</div>)}
      <p className="hint">Suất trong lịch trình là mong muốn, chưa giữ chỗ. Khoản cần thu trên báo giá chưa phải giao dịch thanh toán.</p></div>
    </AccountLayout>;
}
