import { render, screen } from '@testing-library/react-native';

import type { AlunoComHorarios } from '@/lib/agruparAlocacoesPorAluno';
import { TipoMarcacao } from '@/lib/api/horarios';

import { AlunoVinculadoCard } from './AlunoVinculadoCard';

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
};

describe('AlunoVinculadoCard (issue #160)', () => {
  it('shows the nome and identificador of the aluno', async () => {
    await render(<AlunoVinculadoCard aluno={aluno} />);

    expect(screen.getByText('Ana')).toBeTruthy();
    expect(screen.getByText('ana@x.com')).toBeTruthy();
  });

  it('shows "Sem horário" when the aluno has no horarios', async () => {
    await render(<AlunoVinculadoCard aluno={aluno} />);

    expect(screen.getByText('Sem horário')).toBeTruthy();
  });

  it('shows the formatted horarios when the aluno has horarios', async () => {
    const alunoComHorario = {
      ...aluno,
      horarios: [horario, { ...horario, id: 'h2', diaSemana: 4, horaInicio: '14:00:00' }],
    };

    await render(<AlunoVinculadoCard aluno={alunoComHorario} />);

    expect(screen.getByText('Terça · 10:00, Quinta · 14:00')).toBeTruthy();
    expect(screen.queryByText('Sem horário')).toBeNull();
  });
});
