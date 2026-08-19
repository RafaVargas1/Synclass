import { contatoEhValido } from '@/lib/validacaoContato';

describe('contatoEhValido', () => {
  it('aceita e-mail em formato válido', () => {
    expect(contatoEhValido('maria@exemplo.com')).toBe(true);
  });

  it('aceita telefone BR com máscara (99) 99999-9999', () => {
    expect(contatoEhValido('(11) 98765-4321')).toBe(true);
  });

  it('aceita telefone BR só com dígitos (11 dígitos com DDD)', () => {
    expect(contatoEhValido('11987654321')).toBe(true);
  });

  it('aceita telefone BR com espaços e traços em outras posições', () => {
    expect(contatoEhValido('11 98765-4321')).toBe(true);
  });

  it('aceita telefone fixo BR com 10 dígitos (DDD + fixo, sem o 9 do celular)', () => {
    expect(contatoEhValido('1133224455')).toBe(true);
  });

  it('aceita contato com espaço em branco no início/fim (autocorrect do teclado)', () => {
    expect(contatoEhValido(' maria@exemplo.com ')).toBe(true);
  });

  it('rejeita e-mail sem @', () => {
    expect(contatoEhValido('mariaexemplo.com')).toBe(false);
  });

  it('rejeita e-mail sem domínio', () => {
    expect(contatoEhValido('maria@')).toBe(false);
  });

  it('rejeita telefone com menos de 10 dígitos', () => {
    expect(contatoEhValido('119876543')).toBe(false);
  });

  it('rejeita telefone com mais de 11 dígitos', () => {
    expect(contatoEhValido('119876543210')).toBe(false);
  });

  it('rejeita string vazia', () => {
    expect(contatoEhValido('')).toBe(false);
  });

  it('rejeita texto que não é nem e-mail nem telefone', () => {
    expect(contatoEhValido('não é contato válido')).toBe(false);
  });
});
