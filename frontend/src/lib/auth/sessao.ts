import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';

/**
 * Envolve `expo-secure-store` atrás de uma interface fina do projeto (ver
 * docs/spec/code-style.md#dependências), persistindo o token JWT de sessão
 * (issue #18) no armazenamento seguro do dispositivo. `expo-secure-store`
 * não tem implementação no alvo web (o pacote expõe um stub vazio), então
 * caímos em `localStorage` nesse caso — ver
 * https://docs.expo.dev/versions/latest/sdk/securestore/#web-support.
 */
const ChaveToken = 'synclass.sessao.token';
const ChavePapeis = 'synclass.sessao.papeis';

export async function salvarToken(token: string): Promise<void> {
  if (Platform.OS === 'web') {
    localStorage.setItem(ChaveToken, token);
    return;
  }
  await SecureStore.setItemAsync(ChaveToken, token);
}

export async function lerToken(): Promise<string | null> {
  if (Platform.OS === 'web') {
    return localStorage.getItem(ChaveToken);
  }
  return SecureStore.getItemAsync(ChaveToken);
}

export async function limparToken(): Promise<void> {
  if (Platform.OS === 'web') {
    localStorage.removeItem(ChaveToken);
    return;
  }
  await SecureStore.deleteItemAsync(ChaveToken);
}

/**
 * Persiste os papéis do usuário (issue #4) junto do token — mesmo wrapper
 * fino de `salvarToken`/`lerToken`, serializado como JSON já que
 * `SecureStore`/`localStorage` só guardam string.
 */
export async function salvarPapeis(papeis: string[]): Promise<void> {
  const valor = JSON.stringify(papeis);
  if (Platform.OS === 'web') {
    localStorage.setItem(ChavePapeis, valor);
    return;
  }
  await SecureStore.setItemAsync(ChavePapeis, valor);
}

export async function lerPapeis(): Promise<string[] | null> {
  const valor =
    Platform.OS === 'web' ? localStorage.getItem(ChavePapeis) : await SecureStore.getItemAsync(ChavePapeis);
  return valor ? (JSON.parse(valor) as string[]) : null;
}

export async function limparPapeis(): Promise<void> {
  if (Platform.OS === 'web') {
    localStorage.removeItem(ChavePapeis);
    return;
  }
  await SecureStore.deleteItemAsync(ChavePapeis);
}
