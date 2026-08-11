import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { confirmarCodigo, solicitarCodigo } from '@/lib/api/auth';
import { salvarToken } from '@/lib/auth/sessao';

import VerificarCodigoScreen from './verificar';

jest.mock('@/lib/api/auth', () => ({
  confirmarCodigo: jest.fn(),
  solicitarCodigo: jest.fn(),
}));

jest.mock('@/lib/auth/sessao', () => ({
  salvarToken: jest.fn(),
}));

jest.mock('expo-router', () => ({
  useLocalSearchParams: () => ({ contato: 'maria@exemplo.com' }),
}));

const confirmarCodigoMock = confirmarCodigo as jest.Mock;
const solicitarCodigoMock = solicitarCodigo as jest.Mock;
const salvarTokenMock = salvarToken as jest.Mock;

describe('VerificarCodigoScreen', () => {
  beforeEach(() => {
    confirmarCodigoMock.mockReset();
    solicitarCodigoMock.mockReset();
    salvarTokenMock.mockReset();
  });

  it('saves the session token and shows the confirmation when the code is correct', async () => {
    confirmarCodigoMock.mockResolvedValue({
      sucesso: true,
      token: 'token-jwt',
      nome: 'Maria Silva',
      papeis: ['Professor'],
    });
    await render(<VerificarCodigoScreen />);

    await fireEvent.changeText(screen.getByPlaceholderText('000000'), '123456');
    await fireEvent.press(screen.getByText('Confirmar'));

    await waitFor(() => expect(screen.getByText('Login realizado!')).toBeTruthy());
    expect(salvarTokenMock).toHaveBeenCalledWith('token-jwt');
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
    expect(salvarTokenMock).not.toHaveBeenCalled();
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
