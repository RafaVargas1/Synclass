import { luminanciaRelativa, razaoDeContraste } from './contraste';

describe('luminanciaRelativa', () => {
  it('retorna 1 para branco puro', () => {
    expect(luminanciaRelativa('#FFFFFF')).toBe(1);
  });

  it('retorna 0 para preto puro', () => {
    expect(luminanciaRelativa('#000000')).toBe(0);
  });
});

describe('razaoDeContraste', () => {
  it('retorna 21 entre preto e branco (contraste máximo)', () => {
    expect(razaoDeContraste('#000000', '#FFFFFF')).toBe(21);
  });

  it('retorna 1 para duas cores iguais (sem contraste)', () => {
    expect(razaoDeContraste('#F9FAFB', '#F9FAFB')).toBe(1);
  });

  it('é simétrica em relação à ordem dos argumentos', () => {
    expect(razaoDeContraste('#000000', '#FFFFFF')).toBe(razaoDeContraste('#FFFFFF', '#000000'));
  });
});
