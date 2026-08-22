import type { Alocacao } from './api/alocacoes';
import type { AlunoProvisorio } from './api/alunosProvisorios';
import type { Horario } from './api/horarios';

import { agruparAlocacoesPorAluno } from './agruparAlocacoesPorAluno';

const aluno: AlunoProvisorio = { matriculaId: 'a1', nome: 'Ana', identificador: 'ana@x.com' };
const aluno2: AlunoProvisorio = { matriculaId: 'a2', nome: 'Bia', identificador: 'bia@x.com' };
const horarioTerça: Horario = {
  id: 'h1',
  diaSemana: 2,
  horaInicio: '10:00:00',
  duracaoMinutos: 60,
  limiteAlunos: 1,
  tipoMarcacao: 1,
};
const horarioQuinta: Horario = {
  id: 'h2',
  diaSemana: 4,
  horaInicio: '14:00:00',
  duracaoMinutos: 60,
  limiteAlunos: 1,
  tipoMarcacao: 1,
};

function alocacao(horarioId: string, matriculaId: string): Alocacao {
  return { id: `al-${horarioId}-${matriculaId}`, horarioId, matriculaId, createdAt: '2026-01-01T00:00:00Z' };
}

describe('agruparAlocacoesPorAluno (issue #160)', () => {
  it('returns the right horario for an aluno with one allocation', () => {
    const resultado = agruparAlocacoesPorAluno([aluno], [horarioTerça], {
      [horarioTerça.id]: [alocacao(horarioTerça.id, aluno.matriculaId)],
    });

    expect(resultado).toHaveLength(1);
    expect(resultado[0].horarios).toHaveLength(1);
    expect(resultado[0].horarios[0].id).toBe('h1');
  });

  it('returns both horarios for an aluno allocated to two', () => {
    const resultado = agruparAlocacoesPorAluno([aluno], [horarioTerça, horarioQuinta], {
      [horarioTerça.id]: [alocacao(horarioTerça.id, aluno.matriculaId)],
      [horarioQuinta.id]: [alocacao(horarioQuinta.id, aluno.matriculaId)],
    });

    expect(resultado).toHaveLength(1);
    expect(resultado[0].horarios).toHaveLength(2);
    expect(resultado[0].horarios.map((h) => h.id)).toEqual(['h1', 'h2']);
  });

  it('returns an empty horarios list (not undefined) for an aluno without allocation', () => {
    const resultado = agruparAlocacoesPorAluno([aluno], [horarioTerça], {});

    expect(resultado).toHaveLength(1);
    expect(resultado[0].horarios).toEqual([]);
  });

  it('returns an empty list when there are no alunos', () => {
    expect(agruparAlocacoesPorAluno([], [horarioTerça], {})).toEqual([]);
  });

  it('treats a missing alocacoesPorHorario entry as an empty list without throwing', () => {
    const resultado = agruparAlocacoesPorAluno([aluno], [horarioTerça], {});

    expect(resultado[0].horarios).toEqual([]);
  });

  it('only includes horarios where this aluno is allocated, not other alunos', () => {
    const resultado = agruparAlocacoesPorAluno([aluno, aluno2], [horarioTerça], {
      [horarioTerça.id]: [alocacao(horarioTerça.id, aluno2.matriculaId)],
    });

    expect(resultado[0].horarios).toEqual([]);
    expect(resultado[1].horarios.map((h) => h.id)).toEqual(['h1']);
  });
});
