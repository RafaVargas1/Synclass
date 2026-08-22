import type { Alocacao } from './api/alocacoes';
import type { AlunoProvisorio } from './api/alunosProvisorios';
import type { Horario } from './api/horarios';

export type AlunoComHorarios = {
  matriculaId: string;
  nome: string;
  identificador: string;
  horarios: Horario[];
};

/**
 * Cruza a lista de Alunos do Professor com os horários em que cada um está
 * alocado (issue #160) — um Aluno pode estar em zero, um, ou mais
 * horários; `alocacoesPorHorario` já vem carregado (uma chamada de
 * `listarAlocacoes` por horário, feita por quem chama esta função).
 */
export function agruparAlocacoesPorAluno(
  alunos: AlunoProvisorio[],
  horarios: Horario[],
  alocacoesPorHorario: Record<string, Alocacao[]>,
): AlunoComHorarios[] {
  return alunos.map((aluno) => ({
    matriculaId: aluno.matriculaId,
    nome: aluno.nome,
    identificador: aluno.identificador,
    horarios: horarios.filter((horario) =>
      (alocacoesPorHorario[horario.id] ?? []).some((alocacao) => alocacao.matriculaId === aluno.matriculaId),
    ),
  }));
}
