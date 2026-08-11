import * as SecureStore from 'expo-secure-store';

/**
 * Envolve `expo-secure-store` atrás de uma interface fina do projeto (ver
 * docs/spec/code-style.md#dependências), persistindo o token JWT de sessão
 * (issue #18) no armazenamento seguro do dispositivo.
 */
const ChaveToken = 'synclass.sessao.token';

export async function salvarToken(token: string): Promise<void> {
  await SecureStore.setItemAsync(ChaveToken, token);
}

export async function lerToken(): Promise<string | null> {
  return SecureStore.getItemAsync(ChaveToken);
}

export async function limparToken(): Promise<void> {
  await SecureStore.deleteItemAsync(ChaveToken);
}
