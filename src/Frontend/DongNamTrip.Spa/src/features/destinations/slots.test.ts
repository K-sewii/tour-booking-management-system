import { describe, expect, it } from 'vitest';
import type { ActivityInfo, Slot } from '../../api/types';
import { groupSlots, hhmm, slotDays } from './slots';

const slot = (id: number, date: string, start: string): Slot => ({ id, activityId: 1, date, start, end: '10:00:00', status: 'Published' });
const activity = (slots: Slot[]): ActivityInfo => ({ id: 1, destinationId: 1, name: 'A', description: null, durationMinutes: 60, conditions: null, referenceFee: null, status: 'Published', slots });

describe('slots', () => {
  it('cắt giờ về HH:mm', () => {
    expect(hhmm('09:30:00')).toBe('09:30');
    expect(hhmm(null)).toBe('');
  });

  it('gom các ngày có suất, không trùng và đã sắp xếp', () => {
    const days = slotDays([activity([slot(1, '2026-12-02', '09:00:00'), slot(2, '2026-12-01', '09:00:00')]), activity([slot(3, '2026-12-02', '13:00:00')])]);
    expect(days).toEqual(['2026-12-01', '2026-12-02']);
  });

  it('nhóm suất theo ngày và lọc theo ngày đã chọn', () => {
    const slots = [slot(1, '2026-12-01', '09:00:00'), slot(2, '2026-12-01', '13:00:00'), slot(3, '2026-12-02', '09:00:00')];
    expect(groupSlots(slots, '').map(([date, items]) => [date, items.length])).toEqual([['2026-12-01', 2], ['2026-12-02', 1]]);
    expect(groupSlots(slots, '2026-12-02').map(([date]) => date)).toEqual(['2026-12-02']);
    expect(groupSlots(slots, '2027-01-01')).toEqual([]);
  });
});
