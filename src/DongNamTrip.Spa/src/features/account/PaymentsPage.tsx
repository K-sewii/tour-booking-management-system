import { useEffect, useRef, useState, type FormEvent } from 'react';
import { Link, useParams, useSearchParams } from 'react-router';
import { api, messageFor } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Payment, PaymentChoice } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { dateTime, Failure, Loading, price } from '../../components/Catalog';
import { PageState } from '../../components/PageState';
import { AccountLayout } from '../../layouts/AccountLayout';

const methodLabel = (code: string | null) => code === 'bank_transfer' ? 'Ưu tiên chuyển khoản ngân hàng' : code === 'cash' ? 'Ưu tiên trao đổi về tiền mặt' : 'Chưa có lựa chọn ưu tiên';

export function PaymentsPage() {
  const { user } = useAuth();
  const { id } = useParams();
  const [params, setParams] = useSearchParams();
  const payments = useLoad('payments-' + user?.id, () => api.get<Payment[]>('proxy/account/payments'));
  const choice = useLoad('payment-choice-' + user?.id, () => api.get<PaymentChoice>('proxy/account/payment-choice'));
  const dialog = useRef<HTMLDialogElement>(null);
  const openButton = useRef<HTMLButtonElement>(null);
  const select = useRef<HTMLSelectElement>(null);
  const [method, setMethod] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [busy, setBusy] = useState(false);
  const [statusFilter, setStatusFilter] = useState(params.get('status') || '');
  useEffect(() => { setMethod(choice.data?.methodCode || ''); }, [choice.data]);
  useEffect(() => { setStatusFilter(params.get('status') || ''); }, [params]);
  const selected = id ? payments.data?.find(item => item.id === Number(id)) : undefined;
  const filtered = (payments.data || []).filter(item => !params.get('status') || item.status === params.get('status'));
  function open() { setError(''); setSuccess(''); dialog.current?.showModal(); select.current?.focus(); }
  function close() { dialog.current?.close(); }
  async function save(event: FormEvent) {
    event.preventDefault(); setError('');
    if (!method) { setError('Vui lòng chọn một phương thức ưu tiên.'); select.current?.focus(); return; }
    setBusy(true);
    try {
      const result = await api.put<PaymentChoice>('proxy/account/payment-choice', { methodCode: method });
      choice.setData(result); setSuccess('Đã lưu lựa chọn ưu tiên. Chưa có khoản thanh toán nào được thực hiện.'); close();
    } catch (caught) { setError(messageFor(caught)); }
    finally { setBusy(false); }
  }
  return <AccountLayout><div className="account-heading"><div className="eyebrow">ĐƠN VÀ GIAO DỊCH</div><h1 id="account-title">Thanh toán và hoàn tiền</h1><p>Đơn, khoản thu và hoàn tiền thực được ghi nhận cho tài khoản của bạn.</p></div>
    {success && <PageState kind="success">{success}</PageState>}
    <div className="account-card account-payment-choice"><div className="account-card-heading"><h2>Phương thức thanh toán</h2><p>Chỉ lưu lựa chọn ưu tiên của bạn. Chưa liên kết ngân hàng, chưa lưu thẻ và chưa thực hiện thanh toán.</p></div>
      {choice.loading ? <Loading/> : choice.error ? <Failure message={choice.error} retry={choice.retry}/> : <div className="account-payment-summary"><div><strong>{methodLabel(choice.data?.methodCode || null)}</strong><p>Lựa chọn này không tạo đơn hoặc giao dịch.</p></div><button ref={openButton} className="btn outline" type="button" onClick={open}>{choice.data?.methodCode ? 'Đổi' : 'Thêm phương thức'}</button></div>}</div>
    <dialog ref={dialog} className="account-dialog" aria-labelledby="payment-dialog-title" aria-describedby="payment-dialog-description" onClose={() => openButton.current?.focus()}>
      <div className="account-dialog-head"><h2 id="payment-dialog-title">Phương thức thanh toán ưu tiên</h2><button className="account-dialog-close" type="button" aria-label="Đóng hộp thoại" onClick={close}>×</button></div>
      <form onSubmit={event => void save(event)} noValidate><div className="account-dialog-body"><label htmlFor="payment-method">Phương thức ưu tiên <span className="account-required">*</span></label>
        <select ref={select} id="payment-method" value={method} onChange={event => setMethod(event.target.value)} required><option value="">Chọn phương thức</option><option value="bank_transfer">Chuyển khoản ngân hàng</option><option value="cash">Trao đổi về tiền mặt</option></select>
        <p id="payment-dialog-description">Đây là lựa chọn để trao đổi khi có nghiệp vụ đặt tour. Hệ thống chưa nhận hoặc xử lý thanh toán.</p>{error && <p className="account-message account-message--error account-long-message" role="alert">{error}</p>}</div>
        <div className="account-actions"><button className="btn outline" type="button" onClick={close}>Hủy</button><button className="btn" type="submit" disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu'}</button></div></form>
    </dialog>
    <div className="account-card"><div className="account-card-heading"><h2>Đơn đặt của bạn</h2><p>Giá tham khảo ở trang tour không phải khoản đã thanh toán.</p></div>
      {payments.loading ? <Loading/> : payments.error ? <Failure message={payments.error} retry={payments.retry}/> : <><form className="account-filter" onSubmit={event => { event.preventDefault(); setParams(statusFilter ? { status: statusFilter } : {}); }}><label htmlFor="payment-status">Lọc trạng thái</label><select id="payment-status" value={statusFilter} onChange={event => setStatusFilter(event.target.value)}><option value="">Tất cả</option>{[...new Set(payments.data?.map(item => item.status) || [])].map(item => <option key={item}>{item}</option>)}</select><button className="btn" type="submit">Lọc</button></form>
        {!filtered.length ? <p className="account-empty">Không có đơn hoặc giao dịch phù hợp.</p> : <ul className="account-list">{filtered.map(item => <li key={item.id}><div><strong>Đơn #{item.id} · {item.status}</strong><p>Ngày đặt {dateTime(item.placedAt)} · {item.guests} khách · Giá chốt {price(item.agreedPrice)}</p></div><Link className="btn outline" to={`/Account/Payments/${item.id}`}>Chi tiết</Link></li>)}</ul>}</>}
    </div>
    {id && payments.data && !selected && <PageState>Đơn này không tồn tại hoặc không thuộc tài khoản của bạn.</PageState>}
    {selected && <div className="account-card account-secondary"><div className="account-card-heading"><h2>Chi tiết đơn #{selected.id}</h2><p>Trạng thái: {selected.status} · Giá chốt {price(selected.agreedPrice)}</p></div>
      {!selected.transactions.length ? <p className="account-empty">Đơn này chưa có khoản thu hoặc hoàn tiền được ghi nhận.</p> : <ul className="account-list">{selected.transactions.map(transaction => <li key={transaction.id}><div><strong>{transaction.kind} · {price(transaction.amount)}</strong><p>{dateTime(transaction.at)} · Mã tham chiếu {transaction.reference}</p></div></li>)}</ul>}</div>}
  </AccountLayout>;
}
