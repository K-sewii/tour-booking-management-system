import type { Tour } from '../api/types';
const art = (illustration: string) => `/images/${illustration}.svg`;

export function TourImageCollage({ tour, detail = false }: { tour: Tour; detail?: boolean }) {
  const urls = new Set<string>();
  const places = (tour.imagePlaces ?? []).filter(place => {
    if (!place.imageUrl || urls.has(place.imageUrl)) return false;
    urls.add(place.imageUrl);
    return true;
  }).slice(0, 4);
  return <div className={`tour-collage tour-collage--${places.length || 1}${detail ? ' detail-hero' : ''}`}>
    {places.length ? places.map(place => <img key={place.id} src={place.imageUrl!} alt={`Ảnh ${place.name}`} loading="lazy"
      onError={event => { const fallback = art(place.illustration); if (!event.currentTarget.src.endsWith(fallback)) event.currentTarget.src = fallback; }}/>)
      : <img src={art(tour.illustration)} alt={`Minh họa ${tour.region}`}/>}
  </div>;
}
