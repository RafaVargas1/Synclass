import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { listarHistoricoFrequenciaDoAluno } from '@/lib/api/historicoFrequencia';

import HistoricoFrequenciaAlunoScreen from './historico-frequencia';

// TopbarAutenticada (#77) monta o MenuNavegacao real, que já tem sua
// própria suíte (MenuNavegacao.test.tsx). Mockado aqui pra manter este
// arquivo focado no contrato da própria tela, sem precisar mockar
// usePathname/useIsTelaLarga/usePerfilLogado só por causa do menu.
jest.mock('@/components/organisms/TopbarAutenticada', () => {
  const { View } = jest.requireActual('react-native');
  return {
    TopbarAutenticada: ({ children }: { children?: React.ReactNode }) => <View>{children}</View>,
  };
});

jest.mock('expo-router', () => ({
  useRouter: () => ({ back: jest.fn(), replace: jest.fn(), canGoBack: () => false }),
}));

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

  it('re-queries with the picked inicio/fim (fim exclusive = day after the picked day) once both dates are chosen via the calendar', async () => {
    listarHistoricoFrequenciaDoAlunoMock.mockResolvedValue({ sucesso: true, historico: [] });
    await render(<HistoricoFrequenciaAlunoScreen />);
    await waitFor(() => expect(listarHistoricoFrequenciaDoAlunoMock).toHaveBeenCalledWith(undefined));

    await fireEvent.press(screen.getAllByText('Selecionar data')[0]);
    await fireEvent.press(screen.getByText('1'));

    await fireEvent.press(screen.getAllByText('Selecionar data')[0]);
    await fireEvent.press(screen.getByText('5'));

    await waitFor(() => expect(listarHistoricoFrequenciaDoAlunoMock).toHaveBeenCalledTimes(2));
    const [, ultimaChamada] = listarHistoricoFrequenciaDoAlunoMock.mock.calls;
    expect(ultimaChamada[0].inicio.endsWith('-01')).toBe(true);
    expect(ultimaChamada[0].fim.endsWith('-06')).toBe(true);
  });
});
