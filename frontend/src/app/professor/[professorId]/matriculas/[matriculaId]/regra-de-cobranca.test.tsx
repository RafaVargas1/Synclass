import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { definirRegraDeCobranca, obterRegraDeCobranca } from '@/lib/api/regraDeCobranca';

import RegraDeCobrancaScreen from './regra-de-cobranca';

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

jest.mock('@/lib/api/regraDeCobranca', () => ({
  definirRegraDeCobranca: jest.fn(),
  obterRegraDeCobranca: jest.fn(),
}));

jest.mock('expo-router', () => ({
  useLocalSearchParams: () => ({ professorId: 'professor-1', matriculaId: 'matricula-1' }),
  useRouter: () => ({ back: jest.fn(), replace: jest.fn(), canGoBack: () => false }),
}));

const definirRegraDeCobrancaMock = definirRegraDeCobranca as jest.Mock;
const obterRegraDeCobrancaMock = obterRegraDeCobranca as jest.Mock;

describe('RegraDeCobrancaScreen', () => {
  beforeEach(() => {
    definirRegraDeCobrancaMock.mockReset();
    obterRegraDeCobrancaMock.mockReset();
    obterRegraDeCobrancaMock.mockResolvedValue({ sucesso: true, definida: false });
  });

  it('shows a loading indicator while the existing regra is being fetched', async () => {
    obterRegraDeCobrancaMock.mockReturnValue(new Promise(() => {}));

    await render(<RegraDeCobrancaScreen />);

    expect(screen.getByLabelText('Carregando')).toBeTruthy();
  });

  it('shows an inline confirmation when the Api responds with success', async () => {
    definirRegraDeCobrancaMock.mockResolvedValue({
      sucesso: true,
      regra: { matriculaId: 'matricula-1', tipo: 'FixoMensal', valor: 300, frequenciaSemanalContratada: null },
    });
    await render(<RegraDeCobrancaScreen />);
    await waitFor(() => expect(screen.getByText('Salvar regra de cobrança')).toBeTruthy());

    await fireEvent.press(screen.getByText('Fixo mensal'));
    await fireEvent.changeText(screen.getByPlaceholderText('50.00'), '300');
    await fireEvent.press(screen.getByText('Salvar regra de cobrança'));

    await waitFor(() => expect(screen.getByText('Regra de cobrança salva!')).toBeTruthy());
    expect(definirRegraDeCobrancaMock).toHaveBeenCalledWith('professor-1', 'matricula-1', {
      tipo: 'FixoMensal',
      valor: 300,
      frequenciaSemanalContratada: null,
    });
  });

  it('shows the Api error message without crashing when the Api rejects', async () => {
    definirRegraDeCobrancaMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Matrícula não encontrada: matricula-1.',
    });
    await render(<RegraDeCobrancaScreen />);
    await waitFor(() => expect(screen.getByText('Salvar regra de cobrança')).toBeTruthy());

    await fireEvent.changeText(screen.getByPlaceholderText('50.00'), '50');
    await fireEvent.changeText(screen.getByPlaceholderText('3'), '3');
    await fireEvent.press(screen.getByText('Salvar regra de cobrança'));

    await waitFor(() =>
      expect(screen.getByText('Matrícula não encontrada: matricula-1.')).toBeTruthy(),
    );
    expect(screen.getByText('Salvar regra de cobrança')).toBeTruthy();
  });

  it('pre-fills the form when the matrícula already has a regra configured', async () => {
    obterRegraDeCobrancaMock.mockResolvedValue({
      sucesso: true,
      definida: true,
      regra: { matriculaId: 'matricula-1', tipo: 'FixoPorAula', valor: 45, frequenciaSemanalContratada: null },
    });
    await render(<RegraDeCobrancaScreen />);

    await waitFor(() => expect(screen.getByDisplayValue('45')).toBeTruthy());
    expect(obterRegraDeCobrancaMock).toHaveBeenCalledWith('professor-1', 'matricula-1');
  });

  it('shows an error with retry when fetching the existing regra fails, and recovers on retry', async () => {
    obterRegraDeCobrancaMock
      .mockResolvedValueOnce({ sucesso: false, mensagem: 'Erro de conexão.' })
      .mockResolvedValueOnce({ sucesso: true, definida: false });
    await render(<RegraDeCobrancaScreen />);

    await waitFor(() => expect(screen.getByText('Erro de conexão.')).toBeTruthy());

    await fireEvent.press(screen.getByText('Tentar novamente'));

    await waitFor(() => expect(screen.getByText('Salvar regra de cobrança')).toBeTruthy());
    expect(obterRegraDeCobrancaMock).toHaveBeenCalledTimes(2);
  });
});
