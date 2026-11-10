import type { ActivityInfo, Slot } from '../../api/types';

/** Giờ dạng HH:mm từ chuỗi "HH:mm:ss" của API. */
export const hhmm = (value: string | null) => (value ? value.slice(0, 5) : '');

export const dayLabel = (value: string) =>
  new Date(value + 'T00:00:00').toLocaleDateString('vi-VN', { weekday: 'long', day: '2-digit', month: '2-digit', year: 'numeric' });

/** Các ngày có suất, tăng dần, không trùng. Chỉ để hiển thị: lọc suất chưa qua ngày do backend quyết định. */
export const slotDays = (activities: ActivityInfo[]) => [...new Set(activities.flatMap(item => item.slots.map(slot => slot.date)))].sort();

/** Nhóm suất theo ngày để hiển thị; nếu chọn ngày thì chỉ giữ ngày đó. */
export function groupSlots(slots: Slot[], day: string): [string, Slot[]][] {
  const kept = day ? slots.filter(slot => slot.date === day) : slots;
  return [...new Set(kept.map(slot => slot.date))].map(date => [date, kept.filter(slot => slot.date === date)]);
}
