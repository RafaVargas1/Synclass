import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { calcularPeriodoTodos, listarValorDevido } from '@/lib/api/valorDevido';

import ValorDevidoScreen from './valor-devido';

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

jest.mock('@/lib/api/valorDevido', () => ({
  ...jest.requireActual('@/lib/api/valorDevido'),
  listarValorDevido: jest.fn(),
}));

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

  it('loads every aluno by default (modo Todos), not just the current month', async () => {
    listarValorDevidoMock.mockResolvedValue({ sucesso: true, valoresDevidos: [valorDevidoComRegra] });

    await render(<ValorDevidoScreen />);

    const periodoTodos = calcularPeriodoTodos(new Date());
    await waitFor(() => expect(listarValorDevidoMock).toHaveBeenCalledWith('professor-1', periodoTodos));
    expect(screen.getByRole('button', { name: 'Todos', selected: true })).toBeTruthy();
  });

  it('re-queries with periodo undefined (mês corrente) when Este mês is selected', async () => {
    listarValorDevidoMock.mockResolvedValue({ sucesso: true, valoresDevidos: [] });
    await render(<ValorDevidoScreen />);
    await waitFor(() => expect(listarValorDevidoMock).toHaveBeenCalledTimes(1));

    await fireEvent.press(screen.getByText('Este mês'));

    await waitFor(() => expect(listarValorDevidoMock).toHaveBeenCalledWith('professor-1', undefined));
  });

  it('shows an error message with the alunos hidden when listarValorDevido fails', async () => {
    listarValorDevidoMock.mockResolvedValue({ sucesso: false, mensagem: 'Erro de conexão.' });

    await render(<ValorDevidoScreen />);

    await waitFor(() => expect(screen.getByText('Erro de conexão.')).toBeTruthy());
    expect(screen.queryByText('Nenhum Aluno encontrado para este período.')).toBeNull();
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

  it('shows a prompt instead of querying when Personalizado is selected but the dates are incomplete', async () => {
    listarValorDevidoMock.mockResolvedValue({ sucesso: true, valoresDevidos: [] });
    await render(<ValorDevidoScreen />);
    await waitFor(() => expect(listarValorDevidoMock).toHaveBeenCalledTimes(1));

    await fireEvent.press(screen.getByText('Personalizado'));

    expect(screen.getByText('Selecione o início e o fim do período.')).toBeTruthy();
    expect(listarValorDevidoMock).toHaveBeenCalledTimes(1);
  });

  it('queries with the picked inicio/fim (fim exclusive = day after the picked day) once both dates are chosen', async () => {
    listarValorDevidoMock.mockResolvedValue({ sucesso: true, valoresDevidos: [] });
    await render(<ValorDevidoScreen />);
    await waitFor(() => expect(listarValorDevidoMock).toHaveBeenCalledTimes(1));

    await fireEvent.press(screen.getByText('Personalizado'));
    await fireEvent.press(screen.getAllByText('Selecionar data')[0]);
    await fireEvent.press(screen.getByText('1'));
    await fireEvent.press(screen.getAllByText('Selecionar data')[0]);
    await fireEvent.press(screen.getByText('5'));

    await waitFor(() => expect(listarValorDevidoMock).toHaveBeenCalledTimes(2));
    const [, ultimaChamada] = listarValorDevidoMock.mock.calls;
    expect(ultimaChamada[0]).toBe('professor-1');
    expect(ultimaChamada[1].inicio.endsWith('-01')).toBe(true);
    expect(ultimaChamada[1].fim.endsWith('-06')).toBe(true);
  });
});
