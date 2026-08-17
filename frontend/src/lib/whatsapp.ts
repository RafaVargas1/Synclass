/**
 * Código de país do Brasil, usado para montar o número completo exigido
 * pelo link `wa.me` — a RN da issue #2 assume contato de telefone no
 * formato brasileiro (mesmo escopo de `Contato.Normalizar` no backend,
 * ver docs/specs/2-convite-whatsapp/implementation.md).
 */
const CodigoPaisBrasil = '55';

/**
 * Monta o link universal do WhatsApp (`wa.me`) para o convite (issue #2):
 * sem integração de envio real, é só um link aberto pelo dispositivo do
 * Professor (ver decisão documentada em
 * docs/specs/2-convite-whatsapp/implementation.md). Quando o contato é
 * telefone, o link já preenche o destinatário (código do país + número);
 * quando é e-mail, o WhatsApp não aceita e-mail como destinatário, então o
 * link fica genérico (sem destinatário) — o Professor escolhe para quem
 * enviar dentro do próprio app.
 */
export function montarLinkWhatsApp(contato: string, mensagem: string): string {
  const mensagemCodificada = encodeURIComponent(mensagem);
  const destinatario = ehTelefone(contato) ? CodigoPaisBrasil + apenasDigitos(contato) : '';
  return `https://wa.me/${destinatario}?text=${mensagemCodificada}`;
}

function ehTelefone(contato: string): boolean {
  return !contato.includes('@');
}

function apenasDigitos(contato: string): string {
  return contato.replace(/\D/g, '');
}
