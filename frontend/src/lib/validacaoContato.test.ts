import { contatoEhValido, mascararContato } from '@/lib/validacaoContato';

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

describe('mascararContato', () => {
  it('aplica máscara de DDD conforme os dois primeiros dígitos são digitados', () => {
    expect(mascararContato('1')).toBe('(1');
    expect(mascararContato('11')).toBe('(11');
  });

  it('aplica máscara de fixo (99) 9999-9999 até 10 dígitos', () => {
    expect(mascararContato('1133224455')).toBe('(11) 3322-4455');
  });

  it('reformata para celular (99) 99999-9999 ao digitar o 11º dígito', () => {
    expect(mascararContato('11987654321')).toBe('(11) 98765-4321');
  });

  it('ignora dígitos além do 11º', () => {
    expect(mascararContato('119876543219999')).toBe('(11) 98765-4321');
  });

  it('não mascara quando o valor já parece um e-mail (contém letra)', () => {
    expect(mascararContato('maria@exemplo.com')).toBe('maria@exemplo.com');
  });

  it('não mascara um e-mail que começa com dígito antes de digitar a letra', () => {
    expect(mascararContato('3vargas@exemplo.com')).toBe('3vargas@exemplo.com');
  });

  it('mantém string vazia', () => {
    expect(mascararContato('')).toBe('');
  });
});
