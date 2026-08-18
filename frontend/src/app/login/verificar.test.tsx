import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { confirmarCodigo, solicitarCodigo } from '@/lib/api/auth';
import { useSessao } from '@/lib/auth/contexto-sessao';

import VerificarCodigoScreen from './verificar';

jest.mock('@/lib/api/auth', () => ({
  confirmarCodigo: jest.fn(),
  solicitarCodigo: jest.fn(),
}));

const definirSessaoMock = jest.fn();
jest.mock('@/lib/auth/contexto-sessao', () => ({
  useSessao: jest.fn(),
}));

const mockRouterReplace = jest.fn();
jest.mock('expo-router', () => ({
  useLocalSearchParams: () => ({ contato: 'maria@exemplo.com' }),
  useRouter: () => ({ replace: mockRouterReplace }),
}));

const confirmarCodigoMock = confirmarCodigo as jest.Mock;
const solicitarCodigoMock = solicitarCodigo as jest.Mock;
const useSessaoMock = useSessao as jest.Mock;

describe('VerificarCodigoScreen', () => {
  beforeEach(() => {
    confirmarCodigoMock.mockReset();
    solicitarCodigoMock.mockReset();
    definirSessaoMock.mockReset();
    definirSessaoMock.mockResolvedValue(undefined);
    useSessaoMock.mockReset();
    useSessaoMock.mockReturnValue({ definirSessao: definirSessaoMock });
    mockRouterReplace.mockReset();
  });

  it('persists the session (token + papeis) via useSessao and navigates to /painel when the code is correct', async () => {
    confirmarCodigoMock.mockResolvedValue({
      sucesso: true,
      token: 'token-jwt',
      nome: 'Maria Silva',
      papeis: ['Professor'],
    });
    await render(<VerificarCodigoScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('000000'), '123456');
    await fireEvent.press(screen.getByText('Confirmar'));

    await waitFor(() => expect(mockRouterReplace).toHaveBeenCalledWith('/painel'));
    expect(definirSessaoMock).toHaveBeenCalledWith('token-jwt', ['Professor']);
    expect(confirmarCodigoMock).toHaveBeenCalledWith({
      contato: 'maria@exemplo.com',
      codigo: '123456',
    });
  });

  it('shows the Api error message without saving a token when the code is wrong', async () => {
    confirmarCodigoMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'Código inválido ou expirado. Solicite um novo código.',
    });
    await render(<VerificarCodigoScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('000000'), '000000');
    await fireEvent.press(screen.getByText('Confirmar'));

    await waitFor(() =>
      expect(
        screen.getByText('Código inválido ou expirado. Solicite um novo código.'),
      ).toBeTruthy(),
    );
    expect(definirSessaoMock).not.toHaveBeenCalled();
  });

  it('shows an error and stops loading when the device cannot store the session token', async () => {
    confirmarCodigoMock.mockResolvedValue({
      sucesso: true,
      token: 'token-jwt',
      nome: 'Maria Silva',
      papeis: ['Professor'],
    });
    definirSessaoMock.mockRejectedValue(new Error('setValueWithKeyAsync is not a function'));
    await render(<VerificarCodigoScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('000000'), '123456');
    await fireEvent.press(screen.getByText('Confirmar'));

    await waitFor(() =>
      expect(
        screen.getByText('Não foi possível concluir o login neste dispositivo. Tente novamente.'),
      ).toBeTruthy(),
    );
    expect(screen.getByText('Confirmar')).toBeTruthy();
  });

  it('requests a new code when the resend action is pressed', async () => {
    solicitarCodigoMock.mockResolvedValue({ sucesso: true });
    await render(<VerificarCodigoScreen />);

    await fireEvent.press(screen.getByText('Reenviar código'));

    await waitFor(() =>
      expect(solicitarCodigoMock).toHaveBeenCalledWith({ contato: 'maria@exemplo.com' }),
    );
  });
});
