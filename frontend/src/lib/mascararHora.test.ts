import { horaEstaCompleta, mascararHora } from '@/lib/mascararHora';

describe('mascararHora', () => {
  it('manter string vazia', () => {
    expect(mascararHora('')).toBe('');
  });

  it('mantém o primeiro dígito quando ainda não fecha a hora', () => {
    expect(mascararHora('1')).toBe('1');
  });

  it('clampa o 1º dígito de hora para 2 quando acima do limite (nunca >2)', () => {
    expect(mascararHora('9')).toBe('2');
  });

  it('formata os 4 dígitos digitados de uma vez como HH:mm', () => {
    expect(mascararHora('0930')).toBe('09:30');
  });

  it('formata 13:30 digitado de uma vez', () => {
    expect(mascararHora('1330')).toBe('13:30');
  });

  it('clampa o 2º dígito de hora: 25 vira 23', () => {
    expect(mascararHora('2530')).toBe('23:30');
  });

  it('clampa o 3º dígito de minuto: 9 vira 5', () => {
    expect(mascararHora('1099')).toBe('10:59');
  });

  it('remove um dígito ao apagar o ":" de máscara (backspace não pode travar)', () => {
    const anterior = '10:30';
    const apagouSoOPonto = '1030';
    expect(mascararHora(apagouSoOPonto, anterior)).toBe('10:3');
  });
});

describe('horaEstaCompleta', () => {
  it('reconhece HH:mm completo', () => {
    expect(horaEstaCompleta('10:30')).toBe(true);
  });

  it('rejeita valor parcial', () => {
    expect(horaEstaCompleta('10:3')).toBe(false);
  });

  it('rejeita string vazia', () => {
    expect(horaEstaCompleta('')).toBe(false);
  });
});
