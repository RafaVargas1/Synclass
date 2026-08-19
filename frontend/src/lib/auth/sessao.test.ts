import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

import {
  lerPapeis,
  lerToken,
  limparPapeis,
  limparToken,
  salvarPapeis,
  salvarToken,
} from '@/lib/auth/sessao';

jest.mock('expo-secure-store', () => ({
  setItemAsync: jest.fn(),
  getItemAsync: jest.fn(),
  deleteItemAsync: jest.fn(),
}));

describe('sessao', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    Platform.OS = 'ios';
  });

  it('salvarToken stores the token under the session key', async () => {
    await salvarToken('token-jwt');

    expect(SecureStore.setItemAsync).toHaveBeenCalledWith(
      expect.stringContaining('sessao'),
      'token-jwt',
    );
  });

  it('lerToken reads the token from the same key used by salvarToken', async () => {
    (SecureStore.getItemAsync as jest.Mock).mockResolvedValue('token-jwt');

    const token = await lerToken();

    expect(token).toBe('token-jwt');
  });

  it('lerToken returns null when there is no stored session', async () => {
    (SecureStore.getItemAsync as jest.Mock).mockResolvedValue(null);

    const token = await lerToken();

    expect(token).toBeNull();
  });

  it('limparToken removes the stored session', async () => {
    await limparToken();

    expect(SecureStore.deleteItemAsync).toHaveBeenCalledWith(expect.stringContaining('sessao'));
  });

  it('limparPapeis removes the stored papeis', async () => {
    await limparPapeis();

    expect(SecureStore.deleteItemAsync).toHaveBeenCalledWith(expect.stringContaining('papeis'));
  });

  it('salvarPapeis stores the papeis (JSON-encoded) under the session key', async () => {
    await salvarPapeis(['Professor', 'Aluno']);

    expect(SecureStore.setItemAsync).toHaveBeenCalledWith(
      expect.stringContaining('sessao'),
      JSON.stringify(['Professor', 'Aluno']),
    );
  });

  it('lerPapeis reads the papeis from the same key used by salvarPapeis', async () => {
    (SecureStore.getItemAsync as jest.Mock).mockResolvedValue(JSON.stringify(['Professor']));

    const papeis = await lerPapeis();

    expect(papeis).toEqual(['Professor']);
  });

  it('lerPapeis returns null when there is no stored session', async () => {
    (SecureStore.getItemAsync as jest.Mock).mockResolvedValue(null);

    const papeis = await lerPapeis();

    expect(papeis).toBeNull();
  });

  describe('on web', () => {
    beforeEach(() => {
      Platform.OS = 'web';
      // O ambiente de teste (preset RN/node) não tem `localStorage` global
      // como um browser real teria — simula com um Map em memória.
      const armazenamento = new Map<string, string>();
      globalThis.localStorage = {
        getItem: (chave: string) => armazenamento.get(chave) ?? null,
        setItem: (chave: string, valor: string) => {
          armazenamento.set(chave, valor);
        },
        removeItem: (chave: string) => {
          armazenamento.delete(chave);
        },
        clear: () => armazenamento.clear(),
        key: () => null,
        get length() {
          return armazenamento.size;
        },
      } as Storage;
    });

    it('salvarToken stores the token in localStorage instead of SecureStore', async () => {
      await salvarToken('token-jwt');

      expect(localStorage.getItem('synclass.sessao.token')).toBe('token-jwt');
      expect(SecureStore.setItemAsync).not.toHaveBeenCalled();
    });

    it('lerToken reads the token from localStorage', async () => {
      localStorage.setItem('synclass.sessao.token', 'token-jwt');

      const token = await lerToken();

      expect(token).toBe('token-jwt');
      expect(SecureStore.getItemAsync).not.toHaveBeenCalled();
    });

    it('limparToken removes the token from localStorage', async () => {
      localStorage.setItem('synclass.sessao.token', 'token-jwt');

      await limparToken();

      expect(localStorage.getItem('synclass.sessao.token')).toBeNull();
      expect(SecureStore.deleteItemAsync).not.toHaveBeenCalled();
    });

    it('limparPapeis removes the papeis from localStorage', async () => {
      localStorage.setItem('synclass.sessao.papeis', JSON.stringify(['Professor']));

      await limparPapeis();

      expect(localStorage.getItem('synclass.sessao.papeis')).toBeNull();
      expect(SecureStore.deleteItemAsync).not.toHaveBeenCalled();
    });

    it('salvarPapeis stores the papeis in localStorage instead of SecureStore', async () => {
      await salvarPapeis(['Professor']);

      expect(localStorage.getItem('synclass.sessao.papeis')).toBe(JSON.stringify(['Professor']));
      expect(SecureStore.setItemAsync).not.toHaveBeenCalled();
    });

    it('lerPapeis reads the papeis from localStorage', async () => {
      localStorage.setItem('synclass.sessao.papeis', JSON.stringify(['Aluno']));

      const papeis = await lerPapeis();

      expect(papeis).toEqual(['Aluno']);
      expect(SecureStore.getItemAsync).not.toHaveBeenCalled();
    });
  });
});
