/**
 * Nomes em português dos dias da semana, indexados igual ao enum
 * `DiaSemana` do backend (Domingo = 0 .. Sábado = 6) — ver
 * docs/specs/6-horarios-disponiveis/implementation.md#contrato-de-api.
 * Único lugar que conhece esse mapeamento no frontend, usado tanto pelo
 * seletor de dia (`HorarioForm`) quanto pela exibição (`HorarioCard`).
 */
export const NomesDiaSemana = [
  'Domingo',
  'Segunda',
  'Terça',
  'Quarta',
  'Quinta',
  'Sexta',
  'Sábado',
] as const;
