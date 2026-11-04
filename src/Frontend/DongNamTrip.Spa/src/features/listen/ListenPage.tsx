import { useMemo, useRef, useState } from 'react';
import { Link, useSearchParams } from 'react-router';
import { api } from '../../api/client';
import type { ListenCatalog } from '../../api/types';
import { useLoad } from '../../api/useLoad';
import { dateTime, Failure, Loading } from '../../components/Catalog';
import '../../styles/content.css';

const PLAY_ERROR = 'Trình duyệt chưa phát được tệp này. Vui lòng thử lại.';
const FILE_ERROR = 'Chưa tải được tệp thuyết minh. Quyền nghe có thể đã hết hạn hoặc tệp không còn tồn tại.';

/** SCR11: chọn điểm/ngôn ngữ, phát/dừng/nghe lại, mở bằng liên kết QR (?destinationId=&lang=). Tệp được tải qua BFF bằng cookie phiên. */
export function ListenPage() {
  const [params, setParams] = useSearchParams();
  const catalog = useLoad('listen', () => api.get<ListenCatalog>('proxy/listen'));
  const tracks = useMemo(() => catalog.data?.tracks ?? [], [catalog.data]);
  const places = useMemo(() => [...new Map(tracks.map(track => [track.destinationId, track.destinationName])).entries()], [tracks]);
  const wantedPlace = Number(params.get('destinationId')) || 0;
  const place = places.some(([id]) => id === wantedPlace) ? wantedPlace : places[0]?.[0] ?? 0;
  const languages = tracks.filter(track => track.destinationId === place);
  const wantedLang = params.get('lang') || '';
  const track = languages.find(item => item.languageCode === wantedLang) || languages[0];
  const audio = useRef<HTMLAudioElement>(null);
  const [playing, setPlaying] = useState(false);
  const [problem, setProblem] = useState('');
  const audioId = track?.audioId;

  function choose(next: { destinationId?: number; lang?: string }) {
    const merged = new URLSearchParams(params);
    if (next.destinationId !== undefined) { merged.set('destinationId', String(next.destinationId)); merged.delete('lang'); }
    if (next.lang !== undefined) merged.set('lang', next.lang);
    setProblem(''); setPlaying(false);
    setParams(merged, { replace: true });
  }
  const play = () => { void audio.current?.play().catch(() => setProblem(PLAY_ERROR)); };
  const toggle = () => { if (audio.current?.paused) play(); else audio.current?.pause(); };
  const stop = () => { const element = audio.current; if (element) { element.pause(); element.currentTime = 0; } };
  const replay = () => { const element = audio.current; if (element) { element.currentTime = 0; play(); } };

  if (catalog.loading) return <div className="container section"><Loading/></div>;
  if (catalog.error) return <div className="container section"><Failure message={catalog.error} retry={catalog.retry}/></div>;
  const data = catalog.data!;
  return <><section className="page-intro"><div className="container"><div className="eyebrow">NGHE KỂ VỀ NƠI BẠN ĐẾN</div><h1>Nghe thuyết minh.</h1>
    <p>Thuyết minh dành cho các địa điểm trong chuyến đi đã được xác nhận của bạn.</p></div></section>
    <div className="container section cx-listen">
      {!data.hasAccess && <div className="notice" role="status"><b>{data.message}</b><p><Link className="text-link" to="/Tours">Khám phá tour →</Link> · <Link className="text-link" to="/Trips">Chuyến đi của tôi →</Link></p></div>}
      {data.hasAccess && !tracks.length && <div className="notice" role="status">{data.message}</div>}
      {data.hasAccess && !!tracks.length && <div className="cx-listen-panel">
        <label>Địa điểm<select value={place} onChange={event => choose({ destinationId: Number(event.target.value) })}>{places.map(([id, name]) => <option key={id} value={id}>{name}</option>)}</select></label>
        <label>Ngôn ngữ<select value={track?.languageCode || ''} onChange={event => choose({ lang: event.target.value })}>{languages.map(item => <option key={item.audioId} value={item.languageCode}>{item.languageName}</option>)}</select></label>
        {track && <><h2>{track.title}</h2>
          {problem && <div className="account-message account-message--error" role="alert">{problem}</div>}
          <audio key={audioId} ref={audio} src={`/spa-api/proxy/listen/audio/${audioId}`} controls preload="metadata" style={{ width: '100%' }}
            onPlay={() => setPlaying(true)} onPause={() => setPlaying(false)} onEnded={() => setPlaying(false)} onError={() => setProblem(FILE_ERROR)}/>
          <div className="cx-controls"><button className="btn" onClick={toggle}>{playing ? 'Tạm dừng' : 'Phát'}</button>
            <button className="btn outline" onClick={stop}>Dừng</button><button className="btn outline" onClick={replay}>Nghe lại</button></div>
          <details><summary>Xem nội dung văn bản</summary><div className="cx-transcript">{track.transcript}</div></details></>}
        {data.validUntil && <p className="cx-note">Quyền nghe còn hiệu lực đến {dateTime(data.validUntil)}.</p>}
      </div>}
    </div></>;
}
