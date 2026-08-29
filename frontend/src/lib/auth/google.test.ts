import { Platform } from 'react-native';

import { GoogleSignin as GoogleSigninLib } from '@react-native-google-signin/google-signin';

import {
  extrairIdTokenWeb,
  obterIdTokenGoogle,
  type RespostaIdTokenWeb,
} from '@/lib/auth/google';

jest.mock('@react-native-google-signin/google-signin', () => ({
  GoogleSignin: {
    configure: jest.fn(),
    signIn: jest.fn(),
  },
  statusCodes: { SIGN_IN_CANCELLED: 'SIGN_IN_CANCELLED' },
}));

const GoogleSignin = GoogleSigninLib;

describe('extrairIdTokenWeb', () => {
  it('devolve o credential quando a resposta do Google traz um idToken', () => {
    const resposta: RespostaIdTokenWeb = {
      credential: 'id-token-google',
      select_by: 'user',
    };

    expect(extrairIdTokenWeb(resposta)).toBe('id-token-google');
  });

  it('devolve null quando o usuário cancelou o fluxo (select_by == canceled)', () => {
    const resposta: RespostaIdTokenWeb = {
      credential: 'id-token-google',
      select_by: 'canceled',
    };

    expect(extrairIdTokenWeb(resposta)).toBeNull();
  });

  it('devolve null e não lança quando a resposta não traz credential', () => {
    const resposta: RespostaIdTokenWeb = { select_by: 'user' };

    expect(() => extrairIdTokenWeb(resposta)).not.toThrow();
    expect(extrairIdTokenWeb(resposta)).toBeNull();
  });
});

describe('obterIdTokenGoogle', () => {
  const osOriginal = Platform.OS;
  afterEach(() => {
    Platform.OS = osOriginal;
  });

  it('devolve null sem lançar quando não há navegador (sem Google Identity Services)', async () => {
    Platform.OS = 'web';

    await expect(obterIdTokenGoogle()).resolves.toBeNull();
  });

  it('devolve null sem lançar quando não há provider nativo configurado no celular', async () => {
    Platform.OS = 'ios';

    await expect(obterIdTokenGoogle()).resolves.toBeNull();
  });
});

describe('obterIdTokenNativo', () => {
  const osOriginal = Platform.OS;
  beforeEach(() => {
    Platform.OS = 'ios';
    (GoogleSignin.signIn as jest.Mock).mockReset();
  });
  afterEach(() => {
    Platform.OS = osOriginal;
  });

  it('configura o SDK com o webClientId e o iosClientId uma única vez', () => {
    expect(GoogleSignin.configure).toHaveBeenCalledTimes(1);
    expect(GoogleSignin.configure).toHaveBeenCalledWith(
      expect.objectContaining({
        webClientId: process.env.EXPO_PUBLIC_GOOGLE_CLIENT_ID ?? '',
        iosClientId: process.env.EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID ?? '',
      }),
    );
  });

  it('devolve o idToken quando o signIn do SDK tem sucesso', async () => {
    (GoogleSignin.signIn as jest.Mock).mockResolvedValue({
      type: 'success',
      data: { idToken: 'id-token-nativo', user: { id: 'u1', name: 'Ana' } },
    });

    await expect(obterIdTokenGoogle()).resolves.toBe('id-token-nativo');
  });

  it('devolve null quando o usuário cancela o fluxo (resposta type cancelled)', async () => {
    (GoogleSignin.signIn as jest.Mock).mockResolvedValue({ type: 'cancelled', data: null });

    await expect(obterIdTokenGoogle()).resolves.toBeNull();
  });

  it('devolve null e não lança quando o signIn do SDK lança um erro', async () => {
    (GoogleSignin.signIn as jest.Mock).mockRejectedValue(new Error('falha no signIn'));

    await expect(obterIdTokenGoogle()).resolves.toBeNull();
  });
});
