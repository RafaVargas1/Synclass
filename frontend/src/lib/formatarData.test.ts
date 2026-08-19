import { deDataISO, formatarData, paraDataISO, proximoDia } from '@/lib/formatarData';

describe('formatarData', () => {
  it('converts a yyyy-MM-dd date to dd/mm/aaaa', () => {
    expect(formatarData('2026-08-20')).toBe('20/08/2026');
  });

  it('pads single-digit day and month correctly when already zero-padded', () => {
    expect(formatarData('2026-01-05')).toBe('05/01/2026');
  });
});

describe('paraDataISO', () => {
  it('converts a local Date to yyyy-MM-dd', () => {
    expect(paraDataISO(new Date(2026, 7, 20))).toBe('2026-08-20');
  });

  it('zero-pads single-digit day and month', () => {
    expect(paraDataISO(new Date(2026, 0, 5))).toBe('2026-01-05');
  });
});

describe('deDataISO', () => {
  it('converts yyyy-MM-dd to a local Date at midnight', () => {
    const data = deDataISO('2026-08-20');
    expect(data.getFullYear()).toBe(2026);
    expect(data.getMonth()).toBe(7);
    expect(data.getDate()).toBe(20);
  });

  it('round-trips through paraDataISO', () => {
    expect(paraDataISO(deDataISO('2026-01-05'))).toBe('2026-01-05');
  });
});

describe('proximoDia', () => {
  it('returns the following day in yyyy-MM-dd', () => {
    expect(proximoDia('2026-08-20')).toBe('2026-08-21');
  });

  it('rolls over to the next month at the month boundary', () => {
    expect(proximoDia('2026-08-31')).toBe('2026-09-01');
  });

  it('rolls over to the next year at the year boundary', () => {
    expect(proximoDia('2026-12-31')).toBe('2027-01-01');
  });
});
