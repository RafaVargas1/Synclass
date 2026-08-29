import { act, render, screen } from '@testing-library/react-native';

import { botaoProps } from '@/components/molecules/BotaoLoginGoogle.test.helpers';
import { botaoAppleProps } from '@/components/molecules/BotaoLoginApple.test.helpers';
import { useSessao } from '@/lib/auth/contexto-sessao';

import HomeScreen from './index';

jest.mock('@/components/molecules/BotaoLoginGoogle', () => {
  const { BotaoLoginGoogleDeTeste } = jest.requireActual(
    '@/components/molecules/BotaoLoginGoogle.test.helpers',
  );
  return { BotaoLoginGoogle: BotaoLoginGoogleDeTeste };
});

jest.mock('@/components/molecules/BotaoLoginApple', () => {
  const { BotaoLoginAppleDeTeste } = jest.requireActual(
    '@/components/molecules/BotaoLoginApple.test.helpers',
  );
  return { BotaoLoginApple: BotaoLoginAppleDeTeste };
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
    useSessaoMock.mockReturnValue({ carregando: false, token: null, definirSessao: definirSessaoMock });
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

  it('redirects to /painel and renders nothing when there is already a saved session (usabilidade)', async () => {
    useSessaoMock.mockReturnValue({ carregando: false, token: 'token-salvo', definirSessao: definirSessaoMock });

    await render(<HomeScreen />);

    expect(mockRouterReplace).toHaveBeenCalledWith('/painel');
    expect(screen.queryByText('Entrar com Google')).toBeNull();
    expect(screen.queryByText('Continuar com Apple')).toBeNull();
  });

  it('renders nothing while the saved session is still loading, avoiding a flash of the public Home', async () => {
    useSessaoMock.mockReturnValue({ carregando: true, token: null, definirSessao: definirSessaoMock });

    await render(<HomeScreen />);

    expect(mockRouterReplace).not.toHaveBeenCalled();
    expect(screen.queryByText('Entrar com Google')).toBeNull();
    expect(screen.queryByText('Continuar com Apple')).toBeNull();
  });

  it('persists the session and navigates to /painel when Apple login succeeds directly from Home (issue #212)', async () => {
    await render(<HomeScreen />);

    await act(async () => {
      botaoAppleProps.onAutenticado({ token: 'token-apple', nome: 'Maria Silva', papeis: ['Professor'] });
    });

    expect(definirSessaoMock).toHaveBeenCalledWith('token-apple', ['Professor']);
    expect(mockRouterReplace).toHaveBeenCalledWith('/painel');
  });

  it('shows an inline error and does not navigate when the Apple session persistence fails (issue #212)', async () => {
    definirSessaoMock.mockRejectedValue(new Error('falha ao gravar no dispositivo'));
    await render(<HomeScreen />);

    await act(async () => {
      botaoAppleProps.onAutenticado({ token: 'token-apple', nome: 'Maria Silva', papeis: ['Professor'] });
    });

    expect(
      screen.getByText('Não foi possível concluir o login neste dispositivo. Tente novamente.'),
    ).toBeTruthy();
    expect(mockRouterReplace).not.toHaveBeenCalled();
  });

  it('navigates to /login with the email when Apple finds no account (issue #212)', async () => {
    await render(<HomeScreen />);

    await act(async () => {
      botaoAppleProps.onCadastroPendente('novo.apple@gmail.com');
    });

    expect(mockPush).toHaveBeenCalledWith({
      pathname: '/login',
      params: { email: 'novo.apple@gmail.com' },
    });
  });
});
