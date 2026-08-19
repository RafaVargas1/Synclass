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

/**
 * Aceita o mesmo formato validado pelo backend (`Contato.NormalizarTelefone`,
 * `backend/src/Synclass.Domain/Usuarios/Contato.cs`): 10 ou 11 dígitos com
 * DDD (fixo ou celular) — não só 11, para não bloquear no cliente um contato
 * que a Api aceitaria.
 */
export function contatoEhValido(contatoBruto: string): boolean {
  const contato = contatoBruto.trim();
  return PadraoEmail.test(contato) || ehTelefoneValido(contato);
}

function ehTelefoneValido(contato: string): boolean {
  const digitos = contato.replace(/\D/g, '');
  return (digitos.length === 10 || digitos.length === 11) && /^[\d\s()-]+$/.test(contato);
}

/**
 * Máscara progressiva do campo Contato, aplicada a cada tecla (issue #45
 * revisitada — o campo aceita e-mail OU telefone, mas o caso de telefone
 * digitado sem máscara nenhuma era a lacuna real de usabilidade). Enquanto o
 * valor só tem dígitos, formata como telefone BR — `(99) 9999-9999` até 10
 * dígitos (fixo), reformatando para `(99) 99999-9999` ao 11º dígito
 * (celular). Assim que aparece uma letra (sinal de e-mail em digitação),
 * para de mascarar e devolve o valor cru — inclusive já digitado antes da
 * letra aparecer (ex: contato começando com dígito), então não sobra
 * pontuação de máscara colada num e-mail.
 */
export function mascararContato(valor: string): string {
  if (/[a-zA-Z]/.test(valor)) {
    return valor;
  }

  const digitos = valor.replace(/\D/g, '').slice(0, 11);
  if (digitos.length === 0) {
    return valor;
  }
  if (digitos.length <= 2) {
    return `(${digitos}`;
  }
  if (digitos.length <= 6) {
    return `(${digitos.slice(0, 2)}) ${digitos.slice(2)}`;
  }
  if (digitos.length <= 10) {
    return `(${digitos.slice(0, 2)}) ${digitos.slice(2, 6)}-${digitos.slice(6)}`;
  }
  return `(${digitos.slice(0, 2)}) ${digitos.slice(2, 7)}-${digitos.slice(7)}`;
}
