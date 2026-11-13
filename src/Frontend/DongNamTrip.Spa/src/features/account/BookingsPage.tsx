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
  return <><h3>Lịch trình đã chốt</h3><ol className="timeline">{list.map((stop, index) => <li key={index}><h3>Ngày {stop.day}: {stop.place}</h3></li>)}</ol></>;
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
  return <div className="account-card">
    <h2>Đơn #{booking.id} · {orderStatus(booking.status)}</h2>
    <p>{booking.title} · {sourceNames[booking.source] ?? booking.source} · {booking.guests} khách{booking.tripDate ? ` · Ngày đi ${booking.tripDate}` : ''}</p>
    <p>Giá chốt {price(booking.agreedPrice)} · Khoản cần thu để xác nhận {price(booking.requiredDeposit)}</p>
    {booking.holdUntil && booking.status === 'Pending' && <p className="hint">Chỗ được giữ đến {dateTime(booking.holdUntil)}. Quá hạn mà chưa đủ khoản cần thu, đơn sẽ hết hạn và chỗ được trả lại.</p>}
    <dl className="facts"><div><small>Đã thu</small><b>{price(d.paid)}</b></div><div><small>Đã hoàn</small><b>{price(d.refunded)}</b></div><div><small>Còn phải thu</small><b>{price(d.outstanding)}</b></div></dl>
    <Program stops={d.program} quoteId={booking.quoteId}/>
    <p>Điều kiện hủy: {d.cancelPolicy}</p>
    {d.adjustments && d.adjustments.length > 0 && <><h3>Lịch sử điều chỉnh</h3><ul className="account-list">{d.adjustments.map((item, index) => <li key={index}><div><strong>{dateTime(item.at)}</strong><p>{item.change}{item.price !== null ? ` · Giá chốt mới ${price(item.price)}` : ''}{item.reason ? ` · Lý do: ${item.reason}` : ''}</p></div></li>)}</ul></>}
    {d.cancelReason && <p>Lý do hủy: {d.cancelReason}</p>}
    {d.transactions.length > 0 && <ul className="account-list">{d.transactions.map(entry => <li key={entry.id}><div><strong>{kindNames[entry.kind] ?? entry.kind} {price(entry.amount)}</strong><p>{dateTime(entry.at)} · {entry.reference}</p></div></li>)}</ul>}
    {message && <p role="status" className="notice">{message}</p>}{error && <p role="alert" className="account-message account-message--error">{error}</p>}
    {canCancel && <form className="private-request-form" onSubmit={event => void cancel(event)}>
      <label>Lý do hủy<textarea value={reason} onChange={event => setReason(event.target.value)} required maxLength={1000}/></label>
      <button className="btn outline" type="submit" disabled={busy || !reason.trim()}>{busy ? 'Đang gửi…' : 'Gửi yêu cầu hủy'}</button></form>}
  </div>;
}

export function BookingsPage() {
  const { user } = useAuth();
  const { id } = useParams();
  const bookings = useLoad('bookings-' + user?.id, () => api.get<Booking[]>('proxy/account/bookings'));
  const selected = id ? Number(id) : null;
  return <AccountLayout><div className="account-heading"><div className="eyebrow">CHÀO {user?.name}</div><h1 id="account-title">Đơn đặt tour</h1><p>Theo dõi trạng thái, khoản đã thu và gửi yêu cầu hủy.</p></div>
    {bookings.loading ? <Loading/> : bookings.error ? <Failure message={bookings.error} retry={bookings.retry}/> : !bookings.data?.length
      ? <div className="empty"><h2>Chưa có đơn nào.</h2><p>Chọn một tour và lịch khởi hành để đặt chỗ.</p><Link className="btn" to="/Tours">Xem các tour</Link></div>
      : <div className="account-card"><ul className="account-list">{bookings.data.map(order => <li key={order.id}><div><strong>Đơn #{order.id} · {orderStatus(order.status)}</strong><p>{order.title} · {dateTime(order.placedAt)} · {price(order.agreedPrice)}</p></div><Link className="btn outline" to={`/Account/Bookings/${order.id}`}>Chi tiết</Link></li>)}</ul></div>}
    {selected !== null && Number.isFinite(selected) && <BookingDetailView id={selected} onChanged={bookings.retry}/>}
  </AccountLayout>;
}
