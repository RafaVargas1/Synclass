import { Platform } from 'react-native';

import { obterIdTokenApple } from '@/lib/auth/apple';

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
  });

  it('devolve null sem lançar quando a plataforma não é web (nativo é a task #213)', async () => {
    Platform.OS = 'ios';

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
