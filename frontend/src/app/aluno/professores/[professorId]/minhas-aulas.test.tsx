import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { cancelarAula, listarProximasAulas } from '@/lib/api/cancelamentos';
import { confirmarPresenca } from '@/lib/api/frequencias';

import MinhasAulasAlunoScreen from './minhas-aulas';

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
  useLocalSearchParams: () => ({ professorId: 'professor-1' }),
  useRouter: () => ({ back: jest.fn(), replace: jest.fn(), canGoBack: () => false }),
}));

jest.mock('@/lib/api/cancelamentos', () => ({
  listarProximasAulas: jest.fn(),
  cancelarAula: jest.fn(),
}));

jest.mock('@/lib/api/frequencias', () => ({
  confirmarPresenca: jest.fn(),
}));

const listarProximasAulasMock = listarProximasAulas as jest.Mock;
const cancelarAulaMock = cancelarAula as jest.Mock;
const confirmarPresencaMock = confirmarPresenca as jest.Mock;

const aulaProximaExistente = {
  horarioId: 'h1',
  data: '2026-08-20',
  diaSemana: 4,
  horaInicio: '18:00:00',
  duracaoMinutos: 60,
  podeCancelar: true,
  cancelavelAte: '2026-08-19T18:00:00Z',
  prazoCancelamentoMinutos: 1440,
};

describe('MinhasAulasAlunoScreen', () => {
  beforeEach(() => {
    listarProximasAulasMock.mockReset();
    cancelarAulaMock.mockReset();
    confirmarPresencaMock.mockReset();
    listarProximasAulasMock.mockResolvedValue({ sucesso: true, aulas: [aulaProximaExistente] });
  });

  it('loads and shows the próximas aulas on mount', async () => {
    await render(<MinhasAulasAlunoScreen />);

    await waitFor(() => expect(screen.getByText(/Quinta/)).toBeTruthy());
    expect(listarProximasAulasMock).toHaveBeenCalledWith('professor-1');
  });

  it('refetches the list when cancelar succeeds, dropping the canceled occurrence', async () => {
    cancelarAulaMock.mockResolvedValue({
      sucesso: true,
      cancelamento: { id: 'c1', aulaId: 'aula-1', matriculaId: 'matricula-1', canceladoEm: '2026-08-18T12:00:00Z' },
    });
    listarProximasAulasMock
      .mockResolvedValueOnce({ sucesso: true, aulas: [aulaProximaExistente] })
      .mockResolvedValueOnce({ sucesso: true, aulas: [] });
    await render(<MinhasAulasAlunoScreen />);
    await waitFor(() => expect(screen.getByText(/Quinta/)).toBeTruthy());

    await fireEvent.press(screen.getByText('Cancelar'));

    await waitFor(() => expect(screen.queryByText(/Quinta/)).toBeNull());
    expect(cancelarAulaMock).toHaveBeenCalledWith('professor-1', 'h1', '2026-08-20');
    expect(listarProximasAulasMock).toHaveBeenCalledTimes(2);
  });

  it('keeps showing the horário after cancelar when the next occurrence is still returned by the refetch', async () => {
    const proximaOcorrencia = { ...aulaProximaExistente, data: '2026-08-27' };
    cancelarAulaMock.mockResolvedValue({
      sucesso: true,
      cancelamento: { id: 'c1', aulaId: 'aula-1', matriculaId: 'matricula-1', canceladoEm: '2026-08-18T12:00:00Z' },
    });
    listarProximasAulasMock
      .mockResolvedValueOnce({ sucesso: true, aulas: [aulaProximaExistente] })
      .mockResolvedValueOnce({ sucesso: true, aulas: [proximaOcorrencia] });
    await render(<MinhasAulasAlunoScreen />);
    await waitFor(() => expect(screen.getByText(/Quinta/)).toBeTruthy());

    await fireEvent.press(screen.getByText('Cancelar'));

    await waitFor(() => expect(listarProximasAulasMock).toHaveBeenCalledTimes(2));
    expect(screen.getByText(/Quinta/)).toBeTruthy();
  });

  it('shows the Api error message and keeps the aula when cancelar fails', async () => {
    cancelarAulaMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'O prazo para cancelar esta aula expirou.',
    });
    await render(<MinhasAulasAlunoScreen />);
    await waitFor(() => expect(screen.getByText(/Quinta/)).toBeTruthy());

    await fireEvent.press(screen.getByText('Cancelar'));

    await waitFor(() =>
      expect(screen.getByText('O prazo para cancelar esta aula expirou.')).toBeTruthy(),
    );
    expect(screen.getByText(/Quinta/)).toBeTruthy();
  });

  it('shows the Api error message when loading fails', async () => {
    listarProximasAulasMock.mockResolvedValue({ sucesso: false, mensagem: 'Erro de conexão.' });

    await render(<MinhasAulasAlunoScreen />);

    await waitFor(() => expect(screen.getByText('Erro de conexão.')).toBeTruthy());
  });

  it('shows an empty state when there are no próximas aulas', async () => {
    listarProximasAulasMock.mockResolvedValue({ sucesso: true, aulas: [] });

    await render(<MinhasAulasAlunoScreen />);

    await waitFor(() => expect(screen.getByText(/Você ainda não tem nenhuma aula/)).toBeTruthy());
  });

  it('shows the confirmado indicator, optimistically, when confirmar presença succeeds', async () => {
    confirmarPresencaMock.mockResolvedValue({
      sucesso: true,
      confirmacao: { aulaId: 'aula-1', matriculaId: 'matricula-1', confirmadoPeloAluno: true },
    });
    await render(<MinhasAulasAlunoScreen />);
    await waitFor(() => expect(screen.getByText(/Quinta/)).toBeTruthy());

    await fireEvent.press(screen.getByText('Confirmar presença'));

    await waitFor(() => expect(screen.getByText(/Presença confirmada/)).toBeTruthy());
    expect(confirmarPresencaMock).toHaveBeenCalledWith('professor-1', 'h1', '2026-08-20');
    expect(listarProximasAulasMock).toHaveBeenCalledTimes(1);
  });

  it('shows the Api error message and keeps sem confirmar when confirmar presença fails', async () => {
    confirmarPresencaMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'A matrícula matricula-1 já cancelou a aula aula-1 e não pode confirmar presença nela.',
    });
    await render(<MinhasAulasAlunoScreen />);
    await waitFor(() => expect(screen.getByText(/Quinta/)).toBeTruthy());

    await fireEvent.press(screen.getByText('Confirmar presença'));

    await waitFor(() =>
      expect(
        screen.getByText('A matrícula matricula-1 já cancelou a aula aula-1 e não pode confirmar presença nela.'),
      ).toBeTruthy(),
    );
    expect(screen.getByText('Confirmar presença')).toBeTruthy();
  });
});
