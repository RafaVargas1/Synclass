/**
 * Formata uma data no formato `yyyy-MM-dd` (contrato de API) para o padrão
 * BR `dd/mm/aaaa` exibido ao usuário. Extraído de `HistoricoFrequenciaCard`
 * (issue #16) para reaproveitar em `AulaProximaCard` (issue #49) sem
 * duplicar a lógica, ver docs/spec/code-style.md#sem-duplicação-de-código.
 */
export function formatarData(data: string): string {
  const [ano, mes, dia] = data.split('-');
  return `${dia}/${mes}/${ano}`;
}

/**
 * Converte um `Date` local para `yyyy-MM-dd` (contrato de API) — usado pelo
 * seletor de calendário, que trabalha com `Date` internamente. `toISOString`
 * sozinho converteria para UTC primeiro, o que pode virar o dia; monta a
 * string a partir dos componentes locais em vez disso.
 */
export function paraDataISO(data: Date): string {
  const ano = data.getFullYear();
  const mes = String(data.getMonth() + 1).padStart(2, '0');
  const dia = String(data.getDate()).padStart(2, '0');
  return `${ano}-${mes}-${dia}`;
}

/**
 * Dia seguinte a `dataISO` (`yyyy-MM-dd`) — usado para transformar o
 * último dia INCLUSIVE escolhido no calendário no `fim` EXCLUSIVO que o
 * contrato de período da Api espera (`[inicio, fim)`).
 */
export function proximoDia(dataISO: string): string {
  const [ano, mes, dia] = dataISO.split('-').map(Number);
  const data = new Date(ano, mes - 1, dia + 1);
  return paraDataISO(data);
}
