/**
 * Réplica no frontend de `Horario.Sobrepoe` (backend, ver
 * docs/specs/6-horarios-disponiveis/implementation.md) — usada pelo
 * `HorarioForm` para validar conflito no cliente antes de submeter (feedback
 * imediato, sem esperar round-trip para casos óbvios, conforme Critérios
 * técnicos da issue #6). A Api continua sendo a fonte de verdade: este
 * cálculo só evita o caso comum, a Api ainda rejeita se algo escapar.
 */
export type HorarioIntervalo = {
  diaSemana: number;
  horaInicio: string;
  duracaoMinutos: number;
};

export function horariosSeSobrepoe(a: HorarioIntervalo, b: HorarioIntervalo): boolean {
  if (a.diaSemana !== b.diaSemana) {
    return false;
  }

  const inicioA = paraMinutos(a.horaInicio);
  const fimA = inicioA + a.duracaoMinutos;
  const inicioB = paraMinutos(b.horaInicio);
  const fimB = inicioB + b.duracaoMinutos;
  return inicioA < fimB && inicioB < fimA;
}

function paraMinutos(horaInicio: string): number {
  const [horas, minutos] = horaInicio.split(':').map(Number);
  return horas * 60 + minutos;
}
