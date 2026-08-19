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
