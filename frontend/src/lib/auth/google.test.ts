import { Platform } from 'react-native';

import {
  extrairIdTokenWeb,
  obterIdTokenGoogle,
  type RespostaIdTokenWeb,
} from '@/lib/auth/google';

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
