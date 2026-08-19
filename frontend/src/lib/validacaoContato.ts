/**
 * Validação leve, só no cliente, para o campo "Contato" (aceita e-mail OU
 * telefone no mesmo input, por decisão de design — issue #45). Como o campo
 * não distingue os dois tipos de antemão, não dá pra aplicar máscara fixa de
 * digitação; o mínimo viável é barrar no submit/blur um valor que não bate
 * com nenhum dos dois formatos, em vez de depender só da mensagem genérica
 * da Api.
 */
const PadraoEmail = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/**
 * Mensagem exibida no campo Contato dos três formulários (`SolicitarCodigoForm`,
 * `CadastroProfessorForm`, `GerarConviteForm`) quando o valor não bate com
 * nenhum dos dois formatos aceitos.
 */
export const MensagemContatoInvalido = 'Informe um e-mail ou telefone válido.';

export function contatoEhValido(contato: string): boolean {
  return PadraoEmail.test(contato) || ehTelefoneValido(contato);
}

function ehTelefoneValido(contato: string): boolean {
  const digitos = contato.replace(/\D/g, '');
  return digitos.length === 11 && /^[\d\s()-]+$/.test(contato);
}
