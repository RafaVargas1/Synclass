import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { solicitarCodigo } from '@/lib/api/auth';

import LoginScreen from './index';

jest.mock('@/lib/api/auth', () => ({
  solicitarCodigo: jest.fn(),
}));

const mockPush = jest.fn();
jest.mock('expo-router', () => ({
  useRouter: () => ({ push: mockPush }),
}));

const solicitarCodigoMock = solicitarCodigo as jest.Mock;

describe('LoginScreen', () => {
  beforeEach(() => {
    solicitarCodigoMock.mockReset();
    mockPush.mockReset();
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
});
