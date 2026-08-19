import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { listarHistoricoFrequenciaDoAluno } from '@/lib/api/historicoFrequencia';

import HistoricoFrequenciaAlunoScreen from './historico-frequencia';

jest.mock('@/lib/api/historicoFrequencia', () => ({ listarHistoricoFrequenciaDoAluno: jest.fn() }));

const listarHistoricoFrequenciaDoAlunoMock = listarHistoricoFrequenciaDoAluno as jest.Mock;

const aulaPresente = {
  horarioId: 'h1',
  data: '2026-08-04',
  diaSemana: 2,
  horaInicio: '10:00:00',
  status: 'Presente' as const,
};

const historicoProfessorA = { professorId: 'p1', nomeProfessor: 'Professor A', aulas: [aulaPresente] };

describe('HistoricoFrequenciaAlunoScreen', () => {
  beforeEach(() => {
    listarHistoricoFrequenciaDoAlunoMock.mockReset();
  });

  it('shows a loading indicator while listarHistoricoFrequenciaDoAluno is pending', async () => {
    listarHistoricoFrequenciaDoAlunoMock.mockReturnValue(new Promise(() => {}));

    await render(<HistoricoFrequenciaAlunoScreen />);

    expect(screen.getByLabelText('Carregando')).toBeTruthy();
  });

  it('loads with the current month (no periodo) on mount, without any id parameter', async () => {
    listarHistoricoFrequenciaDoAlunoMock.mockResolvedValue({ sucesso: true, historico: [historicoProfessorA] });

    await render(<HistoricoFrequenciaAlunoScreen />);

    await waitFor(() => expect(listarHistoricoFrequenciaDoAlunoMock).toHaveBeenCalledWith(undefined));
  });

  it('shows an error message when listarHistoricoFrequenciaDoAluno fails', async () => {
    listarHistoricoFrequenciaDoAlunoMock.mockResolvedValue({ sucesso: false, mensagem: 'Erro de conexão.' });

    await render(<HistoricoFrequenciaAlunoScreen />);

    await waitFor(() => expect(screen.getByText('Erro de conexão.')).toBeTruthy());
  });

  it('shows the Professor name and each aula with its status', async () => {
    listarHistoricoFrequenciaDoAlunoMock.mockResolvedValue({ sucesso: true, historico: [historicoProfessorA] });

    await render(<HistoricoFrequenciaAlunoScreen />);

    await waitFor(() => expect(screen.getByText('Professor A')).toBeTruthy());
    expect(screen.getByText('Presente')).toBeTruthy();
  });

  it('shows the empty-state message when the Aluno has no matricula', async () => {
    listarHistoricoFrequenciaDoAlunoMock.mockResolvedValue({ sucesso: true, historico: [] });

    await render(<HistoricoFrequenciaAlunoScreen />);

    await waitFor(() => expect(screen.getByText('Nenhum histórico de frequência para este período.')).toBeTruthy());
  });

  it('re-queries with the informed periodo when the user fills inicio/fim and presses Consultar', async () => {
    listarHistoricoFrequenciaDoAlunoMock.mockResolvedValue({ sucesso: true, historico: [] });
    await render(<HistoricoFrequenciaAlunoScreen />);
    await waitFor(() => expect(listarHistoricoFrequenciaDoAlunoMock).toHaveBeenCalledWith(undefined));

    await fireEvent.changeText(screen.getByLabelText('Início do período'), '2026-08-01');
    await fireEvent.changeText(screen.getByLabelText('Fim do período'), '2026-09-01');
    await fireEvent.press(screen.getByText('Consultar'));

    await waitFor(() =>
      expect(listarHistoricoFrequenciaDoAlunoMock).toHaveBeenCalledWith({ inicio: '2026-08-01', fim: '2026-09-01' }),
    );
  });
});
