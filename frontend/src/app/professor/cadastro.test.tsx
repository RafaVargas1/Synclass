import { act, fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { botaoProps } from '@/components/molecules/BotaoLoginGoogle.test.helpers';
import { botaoAppleProps } from '@/components/molecules/BotaoLoginApple.test.helpers';
import { useSessao } from '@/lib/auth/contexto-sessao';
import { cadastrarProfessor, verificarContatoProfessor } from '@/lib/api/professores';

import CadastroProfessorScreen from './cadastro';

jest.mock('expo-router', () => {
  const { Text } = jest.requireActual('react-native');
  return {
    useRouter: () => ({ back: jest.fn(), replace: mockReplace, canGoBack: () => false }),
    useLocalSearchParams: () => ({ ...mockRouteParams }),
    useNavigation: () => ({ setOptions: jest.fn() }),
    Link: ({ href, children }: { href: string; children: React.ReactNode }) => (
      <Text testID={`link-${href}`}>{children}</Text>
    ),
  };
});

jest.mock('@/lib/auth/contexto-sessao', () => ({
  useSessao: jest.fn(),
}));

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

jest.mock('@/lib/api/professores', () => ({
  cadastrarProfessor: jest.fn(),
  verificarContatoProfessor: jest.fn(),
}));

const mockReplace = jest.fn();
const mockRouteParams: Record<string, string> = {};
const definirSessaoMock = jest.fn();
const useSessaoMock = useSessao as jest.Mock;
const cadastrarProfessorMock = cadastrarProfessor as jest.Mock;
const verificarContatoProfessorMock = verificarContatoProfessor as jest.Mock;

describe('CadastroProfessorScreen', () => {
  beforeEach(() => {
    cadastrarProfessorMock.mockReset();
    verificarContatoProfessorMock.mockReset();
    verificarContatoProfessorMock.mockResolvedValue({ identidadeExistente: false, nome: null });
    mockReplace.mockReset();
    mockRouteParams.email = '';
    definirSessaoMock.mockReset();
    definirSessaoMock.mockResolvedValue(undefined);
    useSessaoMock.mockReset();
    useSessaoMock.mockReturnValue({ definirSessao: definirSessaoMock });
  });

  it('shows an inline confirmation when the Api responds with success', async () => {
    cadastrarProfessorMock.mockResolvedValue({ sucesso: true, nome: 'Maria Silva' });
    await render(<CadastroProfessorScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'Maria Silva');
    await fireEvent.changeText(
      screen.getByPlaceholderText('E-mail ou telefone'),
      'maria@exemplo.com',
    );
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() => expect(screen.getByText('Cadastro concluído!')).toBeTruthy());
  });

  it('shows the Api error message without crashing when the Api rejects the cadastro', async () => {
    cadastrarProfessorMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'O contato já está cadastrado como Professor.',
    });
    await render(<CadastroProfessorScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'Maria Silva');
    await fireEvent.changeText(
      screen.getByPlaceholderText('E-mail ou telefone'),
      'maria@exemplo.com',
    );
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() =>
      expect(screen.getByText('O contato já está cadastrado como Professor.')).toBeTruthy(),
    );
    expect(screen.getByText('Cadastrar')).toBeTruthy();
  });

  it('locks the Nome field with the existing nome when the contato already has an identidade', async () => {
    verificarContatoProfessorMock.mockResolvedValue({
      identidadeExistente: true,
      nome: 'Maria Silva',
    });
    await render(<CadastroProfessorScreen />);

    const campoContato = screen.getByPlaceholderText('E-mail ou telefone');
    await fireEvent.changeText(campoContato, 'maria@exemplo.com');
    await fireEvent(campoContato, 'blur');

    await waitFor(() => expect(screen.getByDisplayValue('Maria Silva')).toBeTruthy());
    expect(screen.getByDisplayValue('Maria Silva').props.editable).toBe(false);
    expect(
      screen.getByText('Contato já cadastrado. Para corrigir o nome, edite pelo perfil depois de logado.'),
    ).toBeTruthy();
  });

  it('keeps the Nome field editable when the contato is new', async () => {
    await render(<CadastroProfessorScreen />);

    const campoContato = screen.getByPlaceholderText('E-mail ou telefone');
    await fireEvent.changeText(campoContato, 'novo@exemplo.com');
    await fireEvent(campoContato, 'blur');

    await waitFor(() => expect(verificarContatoProfessorMock).toHaveBeenCalledWith('novo@exemplo.com'));
    expect(screen.getByPlaceholderText('Seu nome completo').props.editable).not.toBe(false);
  });

  it('shows a client-side error and blocks the submit when the contato is neither an e-mail nor a phone', async () => {
    await render(<CadastroProfessorScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('Seu nome completo'), 'Maria Silva');
    await fireEvent.changeText(screen.getByPlaceholderText('E-mail ou telefone'), 'contato invalido');
    await fireEvent.press(screen.getByText('Cadastrar'));

    await waitFor(() =>
      expect(screen.getByText('Informe um e-mail ou telefone válido.')).toBeTruthy(),
    );
    expect(cadastrarProfessorMock).not.toHaveBeenCalled();
  });

  it('pre-fills the contact field from the ?email= route param', async () => {
    mockRouteParams.email = 'pediu.google@gmail.com';
    await render(<CadastroProfessorScreen />);

    expect(screen.getByDisplayValue('pediu.google@gmail.com')).toBeTruthy();
  });

  it('persists the session and navigates to /painel when the Google login succeeds', async () => {
    await render(<CadastroProfessorScreen />);

    await act(async () => {
      botaoProps.onAutenticado({ token: 'token-google', nome: 'Maria Silva', papeis: ['Professor'] });
    });

    expect(definirSessaoMock).toHaveBeenCalledWith('token-google', ['Professor']);
    expect(mockReplace).toHaveBeenCalledWith('/painel');
  });

  it('shows an inline error and does not navigate when persisting the session fails (issue #121)', async () => {
    definirSessaoMock.mockRejectedValue(new Error('falha ao gravar no dispositivo'));
    await render(<CadastroProfessorScreen />);

    await act(async () => {
      botaoProps.onAutenticado({ token: 'token-google', nome: 'Maria Silva', papeis: ['Professor'] });
    });

    expect(
      screen.getByText('Não foi possível concluir o login neste dispositivo. Tente novamente.'),
    ).toBeTruthy();
    expect(mockReplace).not.toHaveBeenCalled();
  });

  it('pre-fills the contact field with the Google e-mail when the account has no user yet', async () => {
    await render(<CadastroProfessorScreen />);

    await act(async () => {
      botaoProps.onCadastroPendente('novo.google@gmail.com');
    });

    expect(screen.getByDisplayValue('novo.google@gmail.com')).toBeTruthy();
  });

  it('persists the session and navigates to /painel when the Apple login succeeds (issue #212)', async () => {
    await render(<CadastroProfessorScreen />);

    await act(async () => {
      botaoAppleProps.onAutenticado({ token: 'token-apple', nome: 'Maria Silva', papeis: ['Professor'] });
    });

    expect(definirSessaoMock).toHaveBeenCalledWith('token-apple', ['Professor']);
    expect(mockReplace).toHaveBeenCalledWith('/painel');
  });

  it('shows an inline error and does not navigate when persisting the session fails on the Apple login (issue #212)', async () => {
    definirSessaoMock.mockRejectedValue(new Error('falha ao gravar no dispositivo'));
    await render(<CadastroProfessorScreen />);

    await act(async () => {
      botaoAppleProps.onAutenticado({ token: 'token-apple', nome: 'Maria Silva', papeis: ['Professor'] });
    });

    expect(
      screen.getByText('Não foi possível concluir o login neste dispositivo. Tente novamente.'),
    ).toBeTruthy();
    expect(mockReplace).not.toHaveBeenCalled();
  });

  it('pre-fills the contact field with the Apple e-mail when the account has no user yet (issue #212)', async () => {
    await render(<CadastroProfessorScreen />);

    await act(async () => {
      botaoAppleProps.onCadastroPendente('novo.apple@gmail.com');
    });

    expect(screen.getByDisplayValue('novo.apple@gmail.com')).toBeTruthy();
  });
});
