import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { listarValorDevido } from '@/lib/api/valorDevido';

import ValorDevidoScreen from './valor-devido';

jest.mock('expo-router', () => ({
  useLocalSearchParams: () => ({ professorId: 'professor-1' }),
}));

jest.mock('@/lib/api/valorDevido', () => ({ listarValorDevido: jest.fn() }));

const listarValorDevidoMock = listarValorDevido as jest.Mock;

const valorDevidoComRegra = {
  matriculaId: 'm1',
  alunoUsuarioId: null,
  nome: 'Ana',
  valor: 300,
  semRegraDefinida: false,
};

const valorDevidoSemRegra = {
  matriculaId: 'm2',
  alunoUsuarioId: null,
  nome: 'Bruno',
  valor: null,
  semRegraDefinida: true,
};

describe('ValorDevidoScreen', () => {
  beforeEach(() => {
    listarValorDevidoMock.mockReset();
  });

  it('shows a loading indicator while listarValorDevido is pending', async () => {
    listarValorDevidoMock.mockReturnValue(new Promise(() => {}));

    await render(<ValorDevidoScreen />);

    expect(screen.getByLabelText('Carregando')).toBeTruthy();
  });

  it('loads with the current month (no periodo) on mount', async () => {
    listarValorDevidoMock.mockResolvedValue({ sucesso: true, valoresDevidos: [valorDevidoComRegra] });

    await render(<ValorDevidoScreen />);

    await waitFor(() => expect(listarValorDevidoMock).toHaveBeenCalledWith('professor-1', undefined));
  });

  it('shows an error message with the alunos hidden when listarValorDevido fails', async () => {
    listarValorDevidoMock.mockResolvedValue({ sucesso: false, mensagem: 'Erro de conexão.' });

    await render(<ValorDevidoScreen />);

    await waitFor(() => expect(screen.getByText('Erro de conexão.')).toBeTruthy());
  });

  it('shows each aluno with its formatted valor devido', async () => {
    listarValorDevidoMock.mockResolvedValue({
      sucesso: true,
      valoresDevidos: [valorDevidoComRegra, valorDevidoSemRegra],
    });

    await render(<ValorDevidoScreen />);

    await waitFor(() => expect(screen.getByText('Ana')).toBeTruthy());
    expect(screen.getByText(/300,00/)).toBeTruthy();
    expect(screen.getByText('Bruno')).toBeTruthy();
    expect(screen.getByText('Sem regra de cobrança definida')).toBeTruthy();
  });

  it('re-queries with the informed periodo when the user fills inicio/fim and presses Consultar', async () => {
    listarValorDevidoMock.mockResolvedValue({ sucesso: true, valoresDevidos: [] });
    await render(<ValorDevidoScreen />);
    await waitFor(() => expect(listarValorDevidoMock).toHaveBeenCalledWith('professor-1', undefined));

    await fireEvent.changeText(screen.getByLabelText('Início do período'), '2026-08-01');
    await fireEvent.changeText(screen.getByLabelText('Fim do período'), '2026-09-01');
    await fireEvent.press(screen.getByText('Consultar'));

    await waitFor(() =>
      expect(listarValorDevidoMock).toHaveBeenCalledWith('professor-1', { inicio: '2026-08-01', fim: '2026-09-01' }),
    );
  });
});
