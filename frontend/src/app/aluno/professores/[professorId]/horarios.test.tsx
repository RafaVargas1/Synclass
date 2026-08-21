import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { listarHorariosVagos, marcarHorario } from '@/lib/api/marcacoes';

import HorariosVagosAlunoScreen from './horarios';

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

jest.mock('@/lib/api/marcacoes', () => ({
  listarHorariosVagos: jest.fn(),
  marcarHorario: jest.fn(),
}));

const listarHorariosVagosMock = listarHorariosVagos as jest.Mock;
const marcarHorarioMock = marcarHorario as jest.Mock;

const horarioVagoExistente = {
  id: 'h1',
  diaSemana: 2,
  horaInicio: '10:00:00',
  duracaoMinutos: 60,
  vagasRestantes: 1,
};

describe('HorariosVagosAlunoScreen', () => {
  beforeEach(() => {
    listarHorariosVagosMock.mockReset();
    marcarHorarioMock.mockReset();
    listarHorariosVagosMock.mockResolvedValue({ sucesso: true, horarios: [horarioVagoExistente] });
  });

  it('loads and shows the horários vagos on mount', async () => {
    await render(<HorariosVagosAlunoScreen />);

    await waitFor(() => expect(screen.getByText(/Terça/)).toBeTruthy());
    expect(listarHorariosVagosMock).toHaveBeenCalledWith('professor-1');
  });

  it('removes the horario from the list when marcar succeeds', async () => {
    marcarHorarioMock.mockResolvedValue({
      sucesso: true,
      alocacao: {
        id: 'a1',
        horarioId: 'h1',
        matriculaId: 'matricula-1',
        createdAt: '2026-08-18T10:00:00Z',
      },
    });
    await render(<HorariosVagosAlunoScreen />);
    await waitFor(() => expect(screen.getByText(/Terça/)).toBeTruthy());

    await fireEvent.press(screen.getByText('Marcar'));

    await waitFor(() => expect(screen.queryByText(/Terça/)).toBeNull());
    expect(marcarHorarioMock).toHaveBeenCalledWith('professor-1', 'h1');
  });

  it('shows the Api error message and keeps the horario when marcar fails', async () => {
    marcarHorarioMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'O horário já atingiu o limite de Alunos.',
    });
    await render(<HorariosVagosAlunoScreen />);
    await waitFor(() => expect(screen.getByText(/Terça/)).toBeTruthy());

    await fireEvent.press(screen.getByText('Marcar'));

    await waitFor(() =>
      expect(screen.getByText('O horário já atingiu o limite de Alunos.')).toBeTruthy(),
    );
    expect(screen.getByText(/Terça/)).toBeTruthy();
  });

  it('shows the Api error message when loading fails', async () => {
    listarHorariosVagosMock.mockResolvedValue({ sucesso: false, mensagem: 'Erro de conexão.' });

    await render(<HorariosVagosAlunoScreen />);

    await waitFor(() => expect(screen.getByText('Erro de conexão.')).toBeTruthy());
  });

  it('shows an empty state when there are no horários vagos', async () => {
    listarHorariosVagosMock.mockResolvedValue({ sucesso: true, horarios: [] });

    await render(<HorariosVagosAlunoScreen />);

    await waitFor(() => expect(screen.getByText(/Nenhum horário/)).toBeTruthy());
  });
});
