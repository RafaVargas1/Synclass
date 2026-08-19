import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { cancelarAula, listarProximasAulas } from '@/lib/api/cancelamentos';

import MinhasAulasAlunoScreen from './minhas-aulas';

jest.mock('expo-router', () => ({
  useLocalSearchParams: () => ({ professorId: 'professor-1' }),
}));

jest.mock('@/lib/api/cancelamentos', () => ({
  listarProximasAulas: jest.fn(),
  cancelarAula: jest.fn(),
}));

const listarProximasAulasMock = listarProximasAulas as jest.Mock;
const cancelarAulaMock = cancelarAula as jest.Mock;

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
});
