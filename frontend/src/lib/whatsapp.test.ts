import { montarLinkWhatsApp } from '@/lib/whatsapp';

describe('montarLinkWhatsApp', () => {
  it('gera link wa.me com número BR (código do país 55) quando o contato é telefone', () => {
    const link = montarLinkWhatsApp('11987654321', 'Você foi convidado para o Synclass!');

    expect(link).toBe(
      `https://wa.me/5511987654321?text=${encodeURIComponent('Você foi convidado para o Synclass!')}`,
    );
  });

  it('normaliza telefone com máscara antes de montar o link', () => {
    const link = montarLinkWhatsApp('(11) 98765-4321', 'Mensagem');

    expect(link).toBe(`https://wa.me/5511987654321?text=${encodeURIComponent('Mensagem')}`);
  });

  it('gera link genérico (sem destinatário) quando o contato é e-mail', () => {
    const link = montarLinkWhatsApp('maria@exemplo.com', 'Você foi convidado para o Synclass!');

    expect(link).toBe(
      `https://wa.me/?text=${encodeURIComponent('Você foi convidado para o Synclass!')}`,
    );
  });

  it('nunca inclui o e-mail do contato na URL do link genérico', () => {
    const link = montarLinkWhatsApp('maria@exemplo.com', 'Mensagem');

    expect(link).not.toContain('maria');
  });
});
