import { normalizarCodigoConvite } from './normalizarCodigoConvite';

describe('normalizarCodigoConvite', () => {
  it('keeps a code that already has only digits unchanged', () => {
    expect(normalizarCodigoConvite('12345')).toBe('12345');
  });

  it('strips spaces', () => {
    expect(normalizarCodigoConvite('12 345')).toBe('12345');
  });

  it('strips mask characters like hyphens', () => {
    expect(normalizarCodigoConvite('1-2-3-4-5')).toBe('12345');
  });

  it('truncates to 5 digits', () => {
    expect(normalizarCodigoConvite('123456789')).toBe('12345');
  });
});
