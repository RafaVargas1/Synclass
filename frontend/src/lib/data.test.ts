import { formatarData } from '@/lib/data';

describe('formatarData', () => {
  it('converts a yyyy-MM-dd date to dd/mm/aaaa', () => {
    expect(formatarData('2026-08-20')).toBe('20/08/2026');
  });

  it('pads single-digit day and month correctly when already zero-padded', () => {
    expect(formatarData('2026-01-05')).toBe('05/01/2026');
  });
});
