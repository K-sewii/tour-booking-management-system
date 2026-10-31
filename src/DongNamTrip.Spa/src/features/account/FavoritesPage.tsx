import { Link, useSearchParams } from 'react-router';
import { useState } from 'react';
import { api, messageFor } from '../../api/client';
import { useLoad } from '../../api/useLoad';
import type { Collections } from '../../api/types';
import { useAuth } from '../../auth/AuthContext';
import { dateTime, Failure, Loading } from '../../components/Catalog';
import { PageState } from '../../components/PageState';
import { AccountLayout } from '../../layouts/AccountLayout';

export function FavoritesPage() {
  const { user } = useAuth();
  const [params] = useSearchParams();
  const tab = params.get('tab') === 'search' ? 'search' : params.get('tab') === 'viewed' ? 'viewed' : 'favorites';
  const loaded = useLoad('collections-' + user?.id, () => api.get<Collections>('proxy/account/collections'));
  const [status, setStatus] = useState('');
  const [error, setError] = useState('');
  async function update(action: () => Promise<unknown>, message: string) {
    setError(''); setStatus('');
    try { await action(); setStatus(message); loaded.retry(); }
    catch (caught) { setError(messageFor(caught)); }
  }
  const entries = tab === 'search' ? loaded.data?.searches || [] : loaded.data?.viewed || [];
  return <AccountLayout><div className="account-heading"><div className="eyebrow">KHÁM PHÁ CỦA BẠN</div><h1 id="account-title">Yêu thích và lịch sử tra cứu</h1><p>Xem lại tour, địa điểm đã lưu và hoạt động gần đây.</p></div>
    {status && <PageState kind="success">{status}</PageState>}{error && <PageState>{error}</PageState>}
    {loaded.loading ? <Loading/> : loaded.error ? <Failure message={loaded.error} retry={loaded.retry}/> : loaded.data && <div className="account-card"><nav className="account-tabs" aria-label="Yêu thích và lịch sử"><Link to="/Account/Favorites" aria-current={tab === 'favorites' ? 'page' : undefined}>Yêu thích</Link><Link to="?tab=search" aria-current={tab === 'search' ? 'page' : undefined}>Tìm kiếm gần đây</Link><Link to="?tab=viewed" aria-current={tab === 'viewed' ? 'page' : undefined}>Đã xem gần đây</Link></nav>
      {tab === 'favorites' ? !loaded.data.favorites.length ? <p className="account-empty">Bạn chưa lưu tour hoặc địa điểm nào. Hãy mở một tour hoặc địa điểm để lưu.</p>
        : <ul className="account-list">{loaded.data.favorites.map(item => <li key={`${item.kind}-${item.itemId}`}><div><strong>{item.missing ? 'Mục không còn hiển thị' : item.name}</strong><p>{item.kind === 'tour' ? 'Tour' : 'Địa điểm'} · Lưu {dateTime(item.at)}</p></div>
          {!item.missing && <Link className="account-text-link" to={`/${item.kind === 'tour' ? 'Tours' : 'Destinations'}/Details/${item.itemId}`}>Xem lại</Link>}
          <button className="btn outline" onClick={() => void update(() => api.delete(`proxy/account/favorites/${item.kind}/${item.itemId}`), 'Đã bỏ khỏi yêu thích.')}>Bỏ lưu</button></li>)}</ul>
        : !entries.length ? <p className="account-empty">Chưa có lịch sử ở mục này.</p> : <><div className="account-clear"><button className="btn outline" onClick={() => void update(() => api.delete(`proxy/account/activity?kind=${tab === 'search' ? 'search' : 'viewed'}`), 'Đã xóa lịch sử đã chọn.')}>Xóa toàn bộ lịch sử mục này</button></div>
          <ul className="account-list">{entries.map(item => <li key={item.id}><div><strong>{tab === 'search' ? item.query : item.missing ? 'Mục không còn hiển thị' : item.name}</strong><p>{dateTime(item.at)}</p></div>
            {tab === 'search' && <Link className="account-text-link" to={`/Tours?q=${encodeURIComponent(item.query || '')}`}>Tìm lại</Link>}
            {tab === 'viewed' && !item.missing && <Link className="account-text-link" to={`/${item.kind === 'tour' ? 'Tours' : 'Destinations'}/Details/${item.itemId}`}>Xem lại</Link>}
            <button className="btn outline" onClick={() => void update(() => api.delete(`proxy/account/activity/${item.id}`), 'Đã xóa mục lịch sử.')}>Xóa</button></li>)}</ul></>}
    </div>}
  </AccountLayout>;
}
