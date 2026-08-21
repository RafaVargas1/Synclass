/**
 * Classifica uma hora (0-23) em período do dia, para a saudação do Painel
 * (issue #69). Horário fora de 0-23: tratado como noite (fallback seguro;
 * `Date.getHours()` nunca devolve fora disso).
 */
export function periodoDoDia(hora: number): 'manha' | 'tarde' | 'noite' {
  if (hora >= 0 && hora <= 11) return 'manha';
  if (hora >= 12 && hora <= 17) return 'tarde';
  return 'noite';
}

/** Texto da saudação por período, para `"{Saudação}, {nome}"` do Painel. */
export const saudacaoPorPeriodo: Record<'manha' | 'tarde' | 'noite', string> = {
  manha: 'Bom dia',
  tarde: 'Boa tarde',
  noite: 'Boa noite',
};
