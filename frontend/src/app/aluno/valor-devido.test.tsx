import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { listarValorDevidoDoAluno } from '@/lib/api/valorDevido';

import ValorDevidoAlunoScreen from './valor-devido';

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

jest.mock('@/lib/api/valorDevido', () => ({ listarValorDevidoDoAluno: jest.fn() }));

const listarValorDevidoDoAlunoMock = listarValorDevidoDoAluno as jest.Mock;

const valorDevidoProfessorA = {
  matriculaId: 'm1',
  alunoUsuarioId: 'aluno-1',
  nome: 'Professor A',
  valor: 300,
  semRegraDefinida: false,
};

const valorDevidoProfessorB = {
  matriculaId: 'm2',
  alunoUsuarioId: 'aluno-1',
  nome: 'Professor B',
  valor: null,
  semRegraDefinida: true,
};

describe('ValorDevidoAlunoScreen', () => {
  beforeEach(() => {
    listarValorDevidoDoAlunoMock.mockReset();
  });

  it('shows a loading indicator while listarValorDevidoDoAluno is pending', async () => {
    listarValorDevidoDoAlunoMock.mockReturnValue(new Promise(() => {}));

    await render(<ValorDevidoAlunoScreen />);

    expect(screen.getByLabelText('Carregando')).toBeTruthy();
  });

  it('loads with the current month (no periodo) on mount, without any id parameter', async () => {
    listarValorDevidoDoAlunoMock.mockResolvedValue({ sucesso: true, valoresDevidos: [valorDevidoProfessorA] });

    await render(<ValorDevidoAlunoScreen />);

    await waitFor(() => expect(listarValorDevidoDoAlunoMock).toHaveBeenCalledWith(undefined));
  });

  it('shows an error message with the professores hidden when listarValorDevidoDoAluno fails', async () => {
    listarValorDevidoDoAlunoMock.mockResolvedValue({ sucesso: false, mensagem: 'Erro de conexão.' });

    await render(<ValorDevidoAlunoScreen />);

    await waitFor(() => expect(screen.getByText('Erro de conexão.')).toBeTruthy());
    expect(screen.queryByText('Nenhuma cobrança ativa ou pendente para este período.')).toBeNull();
  });

  it('shows one entry per Professor, each with its own formatted valor devido, without a summed total', async () => {
    listarValorDevidoDoAlunoMock.mockResolvedValue({
      sucesso: true,
      valoresDevidos: [valorDevidoProfessorA, valorDevidoProfessorB],
    });

    await render(<ValorDevidoAlunoScreen />);

    await waitFor(() => expect(screen.getByText('Professor A')).toBeTruthy());
    expect(screen.getByText(/300,00/)).toBeTruthy();
    expect(screen.getByText('Professor B')).toBeTruthy();
    expect(screen.getByText('Sem regra de cobrança definida')).toBeTruthy();
    expect(screen.queryByText(/Total/)).toBeNull();
  });

  it('shows the empty-state message when the Aluno has no active cobrança (e.g. Aluno provisório sem vínculo pleno)', async () => {
    listarValorDevidoDoAlunoMock.mockResolvedValue({ sucesso: true, valoresDevidos: [] });

    await render(<ValorDevidoAlunoScreen />);

    await waitFor(() => expect(screen.getByText('Nenhuma cobrança ativa ou pendente para este período.')).toBeTruthy());
  });

  it('re-queries with the informed periodo when the user fills inicio/fim and presses Consultar', async () => {
    listarValorDevidoDoAlunoMock.mockResolvedValue({ sucesso: true, valoresDevidos: [] });
    await render(<ValorDevidoAlunoScreen />);
    await waitFor(() => expect(listarValorDevidoDoAlunoMock).toHaveBeenCalledWith(undefined));

    await fireEvent.changeText(screen.getByLabelText('Início do período'), '2026-08-01');
    await fireEvent.changeText(screen.getByLabelText('Fim do período'), '2026-09-01');
    await fireEvent.press(screen.getByText('Consultar'));

    await waitFor(() =>
      expect(listarValorDevidoDoAlunoMock).toHaveBeenCalledWith({ inicio: '2026-08-01', fim: '2026-09-01' }),
    );
  });
});
