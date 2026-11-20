import { useState, type FormEvent } from 'react';
import { Link, useParams } from 'react-router';
import { api, messageFor } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Booking, BookingDetail, ProgramStop } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { dateTime, Failure, Loading, price } from '../../components/Catalog';
import { AccountLayout } from '../../layouts/AccountLayout';

// Nhãn hiển thị cho trạng thái đơn do Backend quy định (OrderStatus); mã lạ hiển thị nguyên mã.
const statusNames: Record<string, string> = {
  Pending: 'Chờ xử lý', Confirmed: 'Đã xác nhận', CancelRequested: 'Chờ hủy', Cancelled: 'Đã hủy', Expired: 'Hết hạn', Completed: 'Hoàn thành',
};
const sourceNames: Record<string, string> = { Departure: 'Tour có sẵn', Quote: 'Tour riêng' };
const kindNames: Record<string, string> = { Receipt: 'Thu', Refund: 'Hoàn' };
export const orderStatus = (status: string) => statusNames[status] ?? status;

// Đơn tour riêng: lịch trình chốt nằm ở phiên bản gắn với báo giá, không nằm trong chi tiết đơn.
function Program({ stops, quoteId }: { stops: ProgramStop[] | null; quoteId: number | null }) {
  const quoted = useLoad('quote-program-' + quoteId, () => quoteId ? api.get<ProgramStop[]>(`proxy/account/quotes/${quoteId}/program`) : Promise.resolve([] as ProgramStop[]));
  const list = quoteId ? quoted.data : stops;
  if (quoteId && quoted.loading) return <Loading/>;
  if (quoteId && quoted.error) return <Failure message={quoted.error} retry={quoted.retry}/>;
  if (!list?.length) return null;
  return <section className="booking-section"><h3>Lịch trình đã chốt</h3><ol className="booking-itinerary">{list.map((stop, index) => <li key={index}><span className="booking-day">Ngày {stop.day}</span><strong>{stop.place}</strong></li>)}</ol></section>;
}

function BookingDetailView({ id, onChanged }: { id: number; onChanged: () => void }) {
  const detail = useLoad('booking-' + id, () => api.get<BookingDetail>('proxy/account/bookings/' + id));
  const [reason, setReason] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  if (detail.loading) return <Loading/>;
  if (detail.error || !detail.data) return <Failure message={detail.error || 'Không tìm thấy đơn.'} retry={detail.retry}/>;
  const { booking } = detail.data;
  const canCancel = booking.status === 'Pending' || booking.status === 'Confirmed';

  async function cancel(event: FormEvent) {
    event.preventDefault(); setError(''); setMessage(''); setBusy(true);
    try {
      await api.post<Booking>(`proxy/account/bookings/${id}/cancel`, { reason: reason.trim() });
      setMessage('Đã gửi yêu cầu hủy. Nhân viên sẽ xử lý và thông báo cho bạn.'); setReason(''); detail.retry(); onChanged();
    } catch (caught) { setError(messageFor(caught)); }
    finally { setBusy(false); }
  }

  const d = detail.data;
  return <article className="account-card booking-detail" aria-labelledby="booking-detail-title">
    <header className="booking-detail-header">
      <div className="booking-reference"><span>Chi tiết đơn #{booking.id}</span><span className="booking-status" data-status={booking.status}>{orderStatus(booking.status)}</span></div>
      <h2 id="booking-detail-title">{booking.title}</h2>
      <dl className="booking-meta"><div><dt>Loại tour</dt><dd>{sourceNames[booking.source] ?? booking.source}</dd></div><div><dt>Số khách</dt><dd>{booking.guests} khách</dd></div>{booking.tripDate && <div><dt>Ngày khởi hành</dt><dd>{booking.tripDate}</dd></div>}</dl>
    </header>
    <div className="booking-detail-body">
      {booking.holdUntil && booking.status === 'Pending' && <p className="booking-hold"><strong>Giữ chỗ đến {dateTime(booking.holdUntil)}</strong><span>Quá hạn mà chưa đủ khoản cần thu, đơn sẽ hết hạn và chỗ được trả lại.</span></p>}
      <section className="booking-payment" aria-labelledby="booking-payment-title"><h3 id="booking-payment-title">Thông tin thanh toán</h3>
        <dl className="booking-price"><div><dt>Giá chốt</dt><dd>{price(booking.agreedPrice)}</dd></div><div><dt>Khoản cần thu để xác nhận</dt><dd>{price(booking.requiredDeposit)}</dd></div></dl>
        <dl className="booking-balance"><div><dt>Đã thu</dt><dd>{price(d.paid)}</dd></div><div><dt>Đã hoàn</dt><dd>{price(d.refunded)}</dd></div><div><dt>Còn phải thu</dt><dd>{price(d.outstanding)}</dd></div></dl>
      </section>
      <Program stops={d.program} quoteId={booking.quoteId}/>
      <section className="booking-section booking-policy"><h3>Điều kiện hủy</h3><p>{d.cancelPolicy}</p>{d.cancelReason && <p><strong>Lý do hủy:</strong> {d.cancelReason}</p>}</section>
      {d.adjustments && d.adjustments.length > 0 && <section className="booking-section"><h3>Lịch sử điều chỉnh</h3><ul className="account-list">{d.adjustments.map((item, index) => <li key={index}><div><strong>{dateTime(item.at)}</strong><p>{item.change}{item.price !== null ? ` · Giá chốt mới ${price(item.price)}` : ''}{item.reason ? ` · Lý do: ${item.reason}` : ''}</p></div></li>)}</ul></section>}
      {d.transactions.length > 0 && <section className="booking-section"><h3>Lịch sử thu và hoàn tiền</h3><ul className="account-list">{d.transactions.map(entry => <li key={entry.id}><div><strong>{kindNames[entry.kind] ?? entry.kind} {price(entry.amount)}</strong><p>{dateTime(entry.at)} · {entry.reference}</p></div></li>)}</ul></section>}
      {message && <p role="status" className="notice">{message}</p>}{error && <p role="alert" className="account-message account-message--error">{error}</p>}
      {canCancel && <section className="booking-section booking-cancellation"><h3>Yêu cầu hủy đơn</h3><form className="private-request-form" onSubmit={event => void cancel(event)}>
        <label>Lý do hủy<textarea value={reason} onChange={event => setReason(event.target.value)} required maxLength={1000}/></label>
        <button className="btn outline" type="submit" disabled={busy || !reason.trim()}>{busy ? 'Đang gửi…' : 'Gửi yêu cầu hủy'}</button></form></section>}
    </div>
  </article>;
}

export function BookingsPage() {
  const { user } = useAuth();
  const { id } = useParams();
  const bookings = useLoad('bookings-' + user?.id, () => api.get<Booking[]>('proxy/account/bookings'));
  const selected = id ? Number(id) : null;
  return <AccountLayout className="account-bookings"><div className="account-heading"><div className="eyebrow">CHÀO {user?.name}</div><h1 id="account-title">Đơn đặt tour</h1><p>Theo dõi trạng thái, khoản đã thu và gửi yêu cầu hủy.</p></div>
    {bookings.loading ? <Loading/> : bookings.error ? <Failure message={bookings.error} retry={bookings.retry}/> : !bookings.data?.length
      ? <div className="empty"><h2>Chưa có đơn nào.</h2><p>Chọn một tour và lịch khởi hành để đặt chỗ.</p><Link className="btn" to="/Tours">Xem các tour</Link></div>
      : <section className="account-card booking-orders" aria-label="Danh sách đơn đặt tour"><ul className="account-list">{bookings.data.map(order => <li key={order.id} className={selected === order.id ? 'booking-order is-selected' : 'booking-order'}><div className="booking-order-info"><div className="booking-reference"><span>Đơn #{order.id}</span><span className="booking-status" data-status={order.status}>{orderStatus(order.status)}</span></div><h2>{order.title}</h2><p>Ngày đặt: {dateTime(order.placedAt)}</p></div><div className="booking-order-action"><strong>{price(order.agreedPrice)}</strong><Link className="btn outline" aria-current={selected === order.id ? 'page' : undefined} to={`/Account/Bookings/${order.id}`}>Chi tiết</Link></div></li>)}</ul></section>}
    {selected !== null && Number.isFinite(selected) && <BookingDetailView id={selected} onChanged={bookings.retry}/>}
  </AccountLayout>;
}
