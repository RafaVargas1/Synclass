import { fireEvent, render, screen, waitFor } from '@testing-library/react-native';

import { BotaoLoginGoogle } from '@/components/molecules/BotaoLoginGoogle';
import { loginComGoogle } from '@/lib/api/auth';
import { obterIdTokenGoogle } from '@/lib/auth/google';

jest.mock('@/lib/auth/google', () => ({
  obterIdTokenGoogle: jest.fn(),
}));

jest.mock('@/lib/api/auth', () => ({
  loginComGoogle: jest.fn(),
}));

const obterIdTokenMock = obterIdTokenGoogle as jest.Mock;
const loginComGoogleMock = loginComGoogle as jest.Mock;

describe('BotaoLoginGoogle', () => {
  const onAutenticado = jest.fn();
  const onCadastroPendente = jest.fn();

  beforeEach(() => {
    obterIdTokenMock.mockReset();
    loginComGoogleMock.mockReset();
    onAutenticado.mockReset();
    onCadastroPendente.mockReset();
  });

  it('chama onAutenticado quando o login Google tem usuário existente', async () => {
    obterIdTokenMock.mockResolvedValue('idToken-valido');
    loginComGoogleMock.mockResolvedValue({
      sucesso: true,
      token: 'token-jwt',
      nome: 'Maria Silva',
      papeis: ['Professor'],
    });

    await render(<BotaoLoginGoogle onAutenticado={onAutenticado} onCadastroPendente={onCadastroPendente} />);

    await fireEvent.press(screen.getByText('Entrar com Google'));

    await waitFor(() =>
      expect(onAutenticado).toHaveBeenCalledWith({
        token: 'token-jwt',
        nome: 'Maria Silva',
        papeis: ['Professor'],
      }),
    );
    expect(onCadastroPendente).not.toHaveBeenCalled();
  });

  it('chama onCadastroPendente com o e-mail quando não existe usuário', async () => {
    obterIdTokenMock.mockResolvedValue('idToken-valido');
    loginComGoogleMock.mockResolvedValue({
      sucesso: false,
      cadastroPendente: true,
      email: 'naoexiste@exemplo.com',
    });

    await render(<BotaoLoginGoogle onAutenticado={onAutenticado} onCadastroPendente={onCadastroPendente} />);

    await fireEvent.press(screen.getByText('Entrar com Google'));

    await waitFor(() => expect(onCadastroPendente).toHaveBeenCalledWith('naoexiste@exemplo.com'));
    expect(onAutenticado).not.toHaveBeenCalled();
  });

  it('exibe a mensagem de erro quando o e-mail do Google não é verificado', async () => {
    obterIdTokenMock.mockResolvedValue('idToken-valido');
    loginComGoogleMock.mockResolvedValue({
      sucesso: false,
      mensagem: 'O e-mail da conta do Google não foi verificado. Use uma conta com e-mail verificado.',
    });

    await render(<BotaoLoginGoogle onAutenticado={onAutenticado} onCadastroPendente={onCadastroPendente} />);

    await fireEvent.press(screen.getByText('Entrar com Google'));

    await waitFor(() =>
      expect(
        screen.getByText('O e-mail da conta do Google não foi verificado. Use uma conta com e-mail verificado.'),
      ).toBeTruthy(),
    );
    expect(onAutenticado).not.toHaveBeenCalled();
    expect(onCadastroPendente).not.toHaveBeenCalled();
  });

  it('renders the official Google G icon next to the label (issue #113)', async () => {
    await render(<BotaoLoginGoogle onAutenticado={onAutenticado} onCadastroPendente={onCadastroPendente} />);

    expect(screen.getByTestId('icone-google', { includeHiddenElements: true })).toBeTruthy();
  });

  it('não emite callback quando o usuário cancela o fluxo do Google (idToken null)', async () => {
    obterIdTokenMock.mockResolvedValue(null);

    await render(<BotaoLoginGoogle onAutenticado={onAutenticado} onCadastroPendente={onCadastroPendente} />);

    await fireEvent.press(screen.getByText('Entrar com Google'));

    await waitFor(() => expect(obterIdTokenMock).toHaveBeenCalled());
    expect(loginComGoogleMock).not.toHaveBeenCalled();
    expect(onAutenticado).not.toHaveBeenCalled();
    expect(onCadastroPendente).not.toHaveBeenCalled();
  });
});
