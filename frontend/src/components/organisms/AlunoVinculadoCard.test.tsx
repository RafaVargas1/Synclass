import { render, screen } from '@testing-library/react-native';

import type { AlunoComHorarios } from '@/lib/agruparAlocacoesPorAluno';
import { TipoMarcacao } from '@/lib/api/horarios';

import { AlunoVinculadoCard } from './AlunoVinculadoCard';

jest.mock('expo-router', () => {
  const React = jest.requireActual('react');
  return {
    Link: ({ href, children }: { href: string; children: React.ReactElement }) =>
      React.cloneElement(children, { accessibilityHint: href }),
  };
});

const aluno: AlunoComHorarios = {
  matriculaId: 'a1',
  nome: 'Ana',
  identificador: 'ana@x.com',
  horarios: [],
};

const horario = {
  id: 'h1',
  diaSemana: 2,
  horaInicio: '10:00:00',
  duracaoMinutos: 60,
  limiteAlunos: 1,
  tipoMarcacao: TipoMarcacao.Fixo,
  prazoCancelamentoMinutos: 0,
};

describe('AlunoVinculadoCard (issue #160)', () => {
  it('shows the nome and identificador of the aluno', async () => {
    await render(<AlunoVinculadoCard professorId="prof-1" aluno={aluno} />);

    expect(screen.getByText('Ana')).toBeTruthy();
    expect(screen.getByText('ana@x.com')).toBeTruthy();
  });

  it('shows "Sem horário" when the aluno has no horarios', async () => {
    await render(<AlunoVinculadoCard professorId="prof-1" aluno={aluno} />);

    expect(screen.getByText('Sem horário')).toBeTruthy();
  });

  it('shows the formatted horarios when the aluno has horarios', async () => {
    const alunoComHorario = {
      ...aluno,
      horarios: [horario, { ...horario, id: 'h2', diaSemana: 4, horaInicio: '14:00:00' }],
    };

    await render(<AlunoVinculadoCard professorId="prof-1" aluno={alunoComHorario} />);

    expect(screen.getByText('Terça · 10:00, Quinta · 14:00')).toBeTruthy();
    expect(screen.queryByText('Sem horário')).toBeNull();
  });

  it('links to the regra de cobrança screen of this matrícula (issue #185)', async () => {
    await render(<AlunoVinculadoCard professorId="prof-1" aluno={aluno} />);

    expect(screen.getByText('Regra de cobrança').parent).toHaveProp(
      'accessibilityHint',
      '/professor/prof-1/matriculas/a1/regra-de-cobranca',
    );
  });

  it('gives the Regra de cobrança link a touch target of at least 44x44 (Fitts/HIG)', async () => {
    await render(<AlunoVinculadoCard professorId="prof-1" aluno={aluno} />);

    expect(screen.getByText('Regra de cobrança').parent).toHaveStyle({
      minWidth: 44,
      minHeight: 44,
    });
  });
});
