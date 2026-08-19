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
 * (celular).
 *
 * `valorAnterior` (o texto antes desta tecla) resolve dois bugs achados em
 * revisão:
 * 1. Ao aparecer uma letra (sinal de e-mail em digitação), só o trecho ANTES
 *    da letra é limpo de pontuação de máscara — o resto do texto (a partir
 *    da letra) é preservado intacto, então um e-mail que começa com dígito
 *    (ex: "3vargas@exemplo.com") não fica com parêntese/traço grudado.
 * 2. Apagar um caractere de máscara (parêntese/espaço/traço) sozinho não
 *    muda a contagem de dígitos, e a máscara reconstruída ficaria idêntica à
 *    anterior — o que trava o backspace visualmente. Quando isso acontece,
 *    remove também o último dígito, para o backspace sempre ter efeito.
 */
export function mascararContato(valor: string, valorAnterior = ''): string {
  const indiceLetra = valor.search(/[a-zA-Z]/);
  if (indiceLetra !== -1) {
    const prefixoNumerico = valor.slice(0, indiceLetra).replace(/\D/g, '');
    return prefixoNumerico + valor.slice(indiceLetra);
  }

  let digitos = valor.replace(/\D/g, '').slice(0, 11);
  const digitosAnteriores = valorAnterior.replace(/\D/g, '');
  const apagouCaractere = valor.length < valorAnterior.length;
  if (apagouCaractere && digitos.length > 0 && digitos.length === digitosAnteriores.length) {
    digitos = digitos.slice(0, -1);
  }

  return formatarTelefoneParcial(digitos);
}

function formatarTelefoneParcial(digitos: string): string {
  if (digitos.length === 0) {
    return '';
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
