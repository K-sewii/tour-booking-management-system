import { describe, expect, it } from 'vitest';
import { dateOnDay } from './plan';

describe('dateOnDay', () => {
  it('keeps the first day and crosses a month boundary', () => {
    expect(dateOnDay('2026-11-30', 1)).toBe('2026-11-30');
    expect(dateOnDay('2026-11-30', 2)).toBe('2026-12-01');
  });
});
