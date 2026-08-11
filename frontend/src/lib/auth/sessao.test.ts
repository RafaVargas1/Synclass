import * as SecureStore from 'expo-secure-store';

import { lerToken, limparToken, salvarToken } from '@/lib/auth/sessao';

jest.mock('expo-secure-store', () => ({
  setItemAsync: jest.fn(),
  getItemAsync: jest.fn(),
  deleteItemAsync: jest.fn(),
}));

describe('sessao', () => {
  beforeEach(() => {
    jest.clearAllMocks();
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
});
