import { act, fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { botaoProps } from '@/components/molecules/BotaoLoginGoogle.test.helpers';
import { solicitarCodigo } from '@/lib/api/auth';
import { useSessao } from '@/lib/auth/contexto-sessao';

import LoginScreen from './index';

jest.mock('@/lib/api/auth', () => ({
  solicitarCodigo: jest.fn(),
}));

jest.mock('@/lib/auth/contexto-sessao', () => ({
  useSessao: jest.fn(),
}));

const mockPush = jest.fn();
const mockRouterReplace = jest.fn();
jest.mock('expo-router', () => ({
  useRouter: () => ({ push: mockPush, replace: mockRouterReplace, back: jest.fn(), canGoBack: () => false }),
}));

jest.mock('@/components/molecules/BotaoLoginGoogle', () => {
  const { BotaoLoginGoogleDeTeste } = jest.requireActual(
    '@/components/molecules/BotaoLoginGoogle.test.helpers',
  );
  return { BotaoLoginGoogle: BotaoLoginGoogleDeTeste };
});

const solicitarCodigoMock = solicitarCodigo as jest.Mock;
const useSessaoMock = useSessao as jest.Mock;
const definirSessaoMock = jest.fn();

describe('LoginScreen', () => {
  beforeEach(() => {
    solicitarCodigoMock.mockReset();
    mockPush.mockReset();
    mockRouterReplace.mockReset();
    definirSessaoMock.mockReset();
    definirSessaoMock.mockResolvedValue(undefined);
    useSessaoMock.mockReset();
    useSessaoMock.mockReturnValue({ definirSessao: definirSessaoMock });
  });

  it('navigates to the code verification screen when the Api sends the code', async () => {
    solicitarCodigoMock.mockResolvedValue({ sucesso: true });
    await render(<LoginScreen />);

    await fireEvent.changeText(
      screen.getByPlaceholderText('E-mail ou telefone cadastrado'),
      'maria@exemplo.com',
    );
    await fireEvent.press(screen.getByText('Enviar código'));

    await waitFor(() =>
      expect(mockPush).toHaveBeenCalledWith({
        pathname: '/login/verificar',
        params: { contato: 'maria@exemplo.com' },
      }),
    );
  });

  it('shows the Api error message without navigating when the contact has no account', async () => {
    solicitarCodigoMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Nenhuma conta encontrada para esse contato.',
    });
    await render(<LoginScreen />);

    await fireEvent.changeText(
      screen.getByPlaceholderText('E-mail ou telefone cadastrado'),
      'naoexiste@exemplo.com',
    );
    await fireEvent.press(screen.getByText('Enviar código'));

    await waitFor(() =>
      expect(screen.getByText('Nenhuma conta encontrada para esse contato.')).toBeTruthy(),
    );
    expect(mockPush).not.toHaveBeenCalled();
  });

  it('shows a client-side error and blocks the submit when the contato is neither an e-mail nor a phone', async () => {
    await render(<LoginScreen />);

    await fireEvent.changeText(
      screen.getByPlaceholderText('E-mail ou telefone cadastrado'),
      'não é um contato válido',
    );
    await fireEvent.press(screen.getByText('Enviar código'));

    await waitFor(() =>
      expect(screen.getByText('Informe um e-mail ou telefone válido.')).toBeTruthy(),
    );
    expect(solicitarCodigoMock).not.toHaveBeenCalled();
  });

  it('persists the session and navigates to /painel when the Google login succeeds', async () => {
    await render(<LoginScreen />);

    await act(async () => {
      botaoProps.onAutenticado({ token: 'token-google', nome: 'Maria Silva', papeis: ['Professor'] });
    });

    expect(definirSessaoMock).toHaveBeenCalledWith('token-google', ['Professor']);
    expect(mockRouterReplace).toHaveBeenCalledWith('/painel');
  });

  it('shows the two cadastro choices with the e-mail when the Google account has no user', async () => {
    await render(<LoginScreen />);

    await act(async () => {
      botaoProps.onCadastroPendente('nao.cadastrado@gmail.com');
    });

    expect(
      screen.getByText('Seu e-mail ainda não tem uma conta. Continue o cadastro como:'),
    ).toBeTruthy();
    await fireEvent.press(screen.getByText('sou Professor'));
    expect(mockPush).toHaveBeenCalledWith({
      pathname: '/professor/cadastro',
      params: { email: 'nao.cadastrado@gmail.com' },
    });
  });

  it('navigates to the Aluno cadastro with the e-mail when the user picks the Aluno cadastro choice', async () => {
    await render(<LoginScreen />);

    await act(async () => {
      botaoProps.onCadastroPendente('nao.cadastrado@gmail.com');
    });

    await fireEvent.press(screen.getByText('sou Aluno'));
    expect(mockPush).toHaveBeenCalledWith({
      pathname: '/aluno',
      params: { email: 'nao.cadastrado@gmail.com' },
    });
  });
});
