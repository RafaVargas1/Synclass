/**
 * Máscara progressiva do campo de hora (issue #138): mesmo padrão de
 * `mascararContato` (validacaoContato.ts) — aplicada a cada tecla,
 * usando `valorAnterior` pra lidar com backspace sobre caractere de
 * máscara (o ":" não muda a contagem de dígitos).
 *
 * Restringe cada dígito à faixa válida no momento da digitação (não deixa
 * digitar hora > 23 nem minuto > 59) em vez de validar só no fim — mesmo
 * princípio de "prevenção de erro" já aplicado em outros campos do app.
 */
export function mascararHora(valor: string, valorAnterior = ''): string {
  let digitos = valor.replace(/\D/g, '').slice(0, 4);
  const digitosAnteriores = valorAnterior.replace(/\D/g, '');
  const apagouCaractere = valor.length < valorAnterior.length;
  if (apagouCaractere && digitos.length > 0 && digitos.length === digitosAnteriores.length) {
    digitos = digitos.slice(0, -1);
  }

  if (digitos.length >= 1) {
    const primeiroDigitoHora = Number(digitos[0]);
    if (primeiroDigitoHora > 2) {
      digitos = '2' + digitos.slice(1);
    }
  }
  if (digitos.length >= 2) {
    const hora = Number(digitos.slice(0, 2));
    if (hora > 23) {
      digitos = '23' + digitos.slice(2);
    }
  }
  if (digitos.length >= 3) {
    const terceiroDigito = Number(digitos[2]);
    if (terceiroDigito > 5) {
      digitos = digitos.slice(0, 2) + '5' + digitos.slice(3);
    }
  }

  return formatarHoraParcial(digitos);
}

function formatarHoraParcial(digitos: string): string {
  if (digitos.length === 0) return '';
  if (digitos.length <= 2) return digitos;
  return `${digitos.slice(0, 2)}:${digitos.slice(2)}`;
}

/** HH:mm completo (4 dígitos) — só então é um valor válido pra confirmar. */
export function horaEstaCompleta(valor: string): boolean {
  return /^\d{2}:\d{2}$/.test(valor);
}
