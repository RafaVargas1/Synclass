import { periodoDoDia, saudacaoPorPeriodo } from './periodoDoDia';

describe('periodoDoDia', () => {
  it('retorna manha para 0-11 (limites 0 e 11)', () => {
    expect(periodoDoDia(0)).toBe('manha');
    expect(periodoDoDia(11)).toBe('manha');
  });

  it('retorna tarde para 12-17 (limites 12 e 17)', () => {
    expect(periodoDoDia(12)).toBe('tarde');
    expect(periodoDoDia(17)).toBe('tarde');
  });

  it('retorna noite para 18-23 (limites 18 e 23)', () => {
    expect(periodoDoDia(18)).toBe('noite');
    expect(periodoDoDia(23)).toBe('noite');
  });
});

describe('saudacaoPorPeriodo', () => {
  it('mapeia cada periodo para a saudacao correspondente', () => {
    expect(saudacaoPorPeriodo.manha).toBe('Bom dia');
    expect(saudacaoPorPeriodo.tarde).toBe('Boa tarde');
    expect(saudacaoPorPeriodo.noite).toBe('Boa noite');
  });
});
