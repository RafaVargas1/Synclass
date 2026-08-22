import { act, render, screen } from '@testing-library/react-native';

import { botaoProps } from '@/components/molecules/BotaoLoginGoogle.test.helpers';
import { useSessao } from '@/lib/auth/contexto-sessao';

import HomeScreen from './index';

jest.mock('@/components/molecules/BotaoLoginGoogle', () => {
  const { BotaoLoginGoogleDeTeste } = jest.requireActual(
    '@/components/molecules/BotaoLoginGoogle.test.helpers',
  );
  return { BotaoLoginGoogle: BotaoLoginGoogleDeTeste };
});

jest.mock('@/lib/auth/contexto-sessao', () => ({
  useSessao: jest.fn(),
}));

const mockTituloDaAba = jest.fn();
jest.mock('@/lib/TituloDaAba', () => ({
  TituloDaAba: (props: { titulo: string }) => {
    mockTituloDaAba(props);
    return null;
  },
}));

const mockPush = jest.fn();
const mockRouterReplace = jest.fn();
jest.mock('expo-router', () => ({
  useRouter: () => ({ push: mockPush, replace: mockRouterReplace, back: jest.fn(), canGoBack: () => false }),
  useNavigation: () => ({ setOptions: jest.fn() }),
  Link: ({ href, children }: { href: string; children: React.ReactNode }) => children,
}));

const useSessaoMock = useSessao as jest.Mock;
const definirSessaoMock = jest.fn();

describe('HomeScreen', () => {
  beforeEach(() => {
    mockPush.mockReset();
    mockRouterReplace.mockReset();
    definirSessaoMock.mockReset();
    definirSessaoMock.mockResolvedValue(undefined);
    useSessaoMock.mockReset();
    useSessaoMock.mockReturnValue({ definirSessao: definirSessaoMock });
    mockTituloDaAba.mockReset();
  });

  it('sets the tab title to Início (issue #133)', async () => {
    await render(<HomeScreen />);

    expect(mockTituloDaAba).toHaveBeenCalledWith({ titulo: 'Início' });
  });

  it('persists the session and navigates to /painel when Google login succeeds directly from Home (issue #114)', async () => {
    await render(<HomeScreen />);

    await act(async () => {
      botaoProps.onAutenticado({ token: 'token-google', nome: 'Maria Silva', papeis: ['Professor'] });
    });

    expect(definirSessaoMock).toHaveBeenCalledWith('token-google', ['Professor']);
    expect(mockRouterReplace).toHaveBeenCalledWith('/painel');
  });

  it('shows an inline error and does not navigate when persisting the session fails (achado de dev-review, PR #118)', async () => {
    definirSessaoMock.mockRejectedValue(new Error('falha ao gravar no dispositivo'));
    await render(<HomeScreen />);

    await act(async () => {
      botaoProps.onAutenticado({ token: 'token-google', nome: 'Maria Silva', papeis: ['Professor'] });
    });

    expect(
      screen.getByText('Não foi possível concluir o login neste dispositivo. Tente novamente.'),
    ).toBeTruthy();
    expect(mockRouterReplace).not.toHaveBeenCalled();
  });

  it('navigates to /login with the email when Google finds no account (issue #114)', async () => {
    await render(<HomeScreen />);

    await act(async () => {
      botaoProps.onCadastroPendente('novo@exemplo.com');
    });

    expect(mockPush).toHaveBeenCalledWith({
      pathname: '/login',
      params: { email: 'novo@exemplo.com' },
    });
  });
});
