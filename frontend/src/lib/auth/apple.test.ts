import { Platform } from 'react-native';

import * as AppleAuthentication from 'expo-apple-authentication';

import { obterIdTokenApple } from '@/lib/auth/apple';

jest.mock('expo-apple-authentication', () => ({
  signInAsync: jest.fn(),
  AppleAuthenticationScope: {
    FULL_NAME: 0,
    EMAIL: 1,
  },
}));

const mockSignInAsync = AppleAuthentication.signInAsync as jest.Mock;

type JanelaComAppleFake = {
  AppleID?: {
    auth?: {
      init: jest.Mock;
      signIn: jest.Mock;
    };
  };
};

function definirAppleIdNaJanela(auth?: { init: jest.Mock; signIn: jest.Mock }) {
  (globalThis.window as unknown as JanelaComAppleFake).AppleID = auth ? { auth } : undefined;
}

describe('obterIdTokenApple', () => {
  const osOriginal = Platform.OS;

  afterEach(() => {
    Platform.OS = osOriginal;
    definirAppleIdNaJanela(undefined);
    mockSignInAsync.mockReset();
  });

  it('devolve null sem lançar no Android (sem Sign in with Apple)', async () => {
    Platform.OS = 'android';

    await expect(obterIdTokenApple()).resolves.toBeNull();
  });

  it('devolve null sem lançar quando o script do Sign in with Apple ainda não carregou', async () => {
    Platform.OS = 'web';
    definirAppleIdNaJanela(undefined);

    await expect(obterIdTokenApple()).resolves.toBeNull();
  });

  it('devolve o id_token quando o signIn do SDK resolve com sucesso (usePopup: true)', async () => {
    Platform.OS = 'web';
    const signIn = jest.fn().mockResolvedValue({
      authorization: { id_token: 'id-token-apple', code: 'codigo', state: 'estado' },
      user: { email: 'maria@exemplo.com' },
    });
    definirAppleIdNaJanela({ init: jest.fn(), signIn });

    await expect(obterIdTokenApple()).resolves.toBe('id-token-apple');
  });

  it('devolve null e não lança quando o usuário cancela o popup (signIn rejeita)', async () => {
    Platform.OS = 'web';
    const signIn = jest.fn().mockRejectedValue({ error: 'popup_closed_by_user' });
    definirAppleIdNaJanela({ init: jest.fn(), signIn });

    await expect(obterIdTokenApple()).resolves.toBeNull();
  });

  it('devolve null quando a resposta não traz id_token na claim de autorização', async () => {
    Platform.OS = 'web';
    const signIn = jest.fn().mockResolvedValue({ authorization: {} });
    definirAppleIdNaJanela({ init: jest.fn(), signIn });

    await expect(obterIdTokenApple()).resolves.toBeNull();
  });
});

describe('obterIdTokenNativoApple', () => {
  const osOriginal = Platform.OS;

  beforeEach(() => {
    Platform.OS = 'ios';
  });

  afterEach(() => {
    Platform.OS = osOriginal;
    mockSignInAsync.mockReset();
  });

  it('devolve o identityToken quando o signInAsync nativo tem sucesso', async () => {
    mockSignInAsync.mockResolvedValue({
      identityToken: 'id-token-nativo',
      email: 'maria@exemplo.com',
    });

    await expect(obterIdTokenApple()).resolves.toBe('id-token-nativo');
    // Pedido de scopes nome+email no primeiro acesso, conforme implementation.md.
    expect(mockSignInAsync).toHaveBeenCalledWith({
      requestedScopes: [
        AppleAuthentication.AppleAuthenticationScope.FULL_NAME,
        AppleAuthentication.AppleAuthenticationScope.EMAIL,
      ],
    });
  });

  it('devolve o identityToken mesmo com e-mail ausente (login que não é o primeiro)', async () => {
    // Edge point do implementation.md#edge-points: a Apple só devolve
    // email/fullName no PRIMEIRO signInAsync. Em logins seguintes o
    // credential.email vem null, mas o identityToken (que é o que o backend lê)
    // continua válido — o frontend não pode depender de credential.email.
    mockSignInAsync.mockResolvedValue({
      identityToken: 'id-token-nativo',
      email: null,
      fullName: null,
    });

    await expect(obterIdTokenApple()).resolves.toBe('id-token-nativo');
  });

  it('devolve null quando o signInAsync não traz identityToken', async () => {
    mockSignInAsync.mockResolvedValue({ email: 'maria@exemplo.com' });

    await expect(obterIdTokenApple()).resolves.toBeNull();
  });

  it('devolve null quando o usuário cancela (error.code ERR_REQUEST_CANCELED)', async () => {
    mockSignInAsync.mockRejectedValue({ code: 'ERR_REQUEST_CANCELED' });

    await expect(obterIdTokenApple()).resolves.toBeNull();
  });

  it('devolve null e não lança quando o signInAsync lança outro erro', async () => {
    mockSignInAsync.mockRejectedValue(new Error('falha no signInAsync'));

    await expect(obterIdTokenApple()).resolves.toBeNull();
  });
});
