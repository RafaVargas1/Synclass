import { GoogleSignin } from '@react-native-google-signin/google-signin';
import { Platform } from 'react-native';

/**
 * Google Sign-In nativo (issue #192) — SDK configurado uma única vez por
 * processo do app, no escopo do módulo, como a própria lib recomenda. O
 * `webClientId` precisa ser o MESMO Client ID "Web application" do backend
 * (`GOOGLE_CLIENT_ID`) para o `aud` do idToken bater com
 * `ValidadorDeIdTokenGoogle`; só o `iosClientId` nativo é adicional, e o
 * `androidClientId` não é passado porque o Android usa o `webClientId`
 * para o handshake e o `aud` do idToken (ver implementation.md).
 */
GoogleSignin.configure({
  webClientId: process.env.EXPO_PUBLIC_GOOGLE_CLIENT_ID ?? '',
  iosClientId: process.env.EXPO_PUBLIC_GOOGLE_IOS_CLIENT_ID ?? '',
});

/**
 * idToken do Google obtido via Google Identity Services (web). O formato é
 * o payload e `select_by` que o SDK do Google devolve no callback de
 * `google.accounts.id.prompt()` — documentado em
 * https://developers.google.com/identity/gsi/web/reference/js-reference.
 */
export type RespostaIdTokenWeb = {
  credential?: string;
  select_by?: string;
};

/**
 * Forma mínima do Google Identity Services injetado no `window` pela página
 * web — declaração local porque a lib de tipos do Google não faz parte do
 * projeto (ver docs/spec/code-style.md#dependências: API de terceiro é
 * envolvida com uma interface fina própria).
 */
type GoogleIdentityServicesWeb = {
  google?: {
    accounts?: {
      initialize: (config: { client_id: string; callback: (resposta: RespostaIdTokenWeb) => void }) => void;
      prompt: () => void;
    };
  };
};

/**
 * Extrai o idToken da resposta do Google Identity Services (web). Quando o
 * usuário cancela o fluxo, o `select_by` vem como `canceled` (ou a resposta
 * chega sem `credential`) — devolve `null` em vez de lançar, porque o
 * cancelamento é um desfecho esperado, não um erro (ver
 * docs/spec/code-style.md#estilo-de-código).
 */
export function extrairIdTokenWeb(resposta: RespostaIdTokenWeb): string | null {
  if (resposta.select_by === 'canceled' || !resposta.credential) {
    return null;
  }
  return resposta.credential;
}

const GoogleClientId = process.env.EXPO_PUBLIC_GOOGLE_CLIENT_ID ?? '';

/**
 * Wrapper único de obtenção do idToken do Google (issue #65), com
 * ramificação por plataforma — mesmo padrão de
 * `lib/auth/sessao.ts` que ramifica por `Platform.OS`. Nunca lança: quando
 * o provider da plataforma não está disponível (web sem o Google Identity
 * Services carregado ou nativo sem o SDK configurado/cancelado), devolve
 * `null`.
 */
export async function obterIdTokenGoogle(): Promise<string | null> {
  if (Platform.OS === 'web') {
    return obterIdTokenNoBrowserWeb();
  }
  return obterIdTokenNativo();
}

async function obterIdTokenNoBrowserWeb(): Promise<string | null> {
  // Em SSR/web o `window` existe; em runtime fora do browser o `?.` preserva
  // o contrato de "devolve null em vez de lançar".
  const janelaComGoogle = globalThis.window as Window &
    typeof globalThis &
    GoogleIdentityServicesWeb;
  const contas = janelaComGoogle.google?.accounts;
  if (!contas) {
    return null;
  }
  if (!GoogleClientId) {
    return null;
  }

  return new Promise((resolve) => {
    contas.initialize({
      client_id: GoogleClientId,
      callback: (resposta: RespostaIdTokenWeb) => resolve(extrairIdTokenWeb(resposta)),
    });
    contas.prompt();
  });
}

/**
 * Obtém o idToken via Google Sign-In nativo (`@react-native-google-signin`,
 * issue #192). Na versão instalada (v16) o `signIn()` NÃO lança no
 * cancelamento — devolve a união discriminada `{ type: 'success' }` ou
 * `{ type: 'cancelled' }`, então o mapeamento de cancelamento para `null`
 * é feito pelo `type`, e QUALQUER erro de SDK (ex: Google Play Services
 * ausente) também vira `null`, preservando o contrato do arquivo de "nunca
 * lança".
 */
async function obterIdTokenNativo(): Promise<string | null> {
  try {
    const resposta = await GoogleSignin.signIn();
    if (resposta.type === 'cancelled') {
      return null;
    }
    return resposta.data.idToken ?? null;
  } catch {
    return null;
  }
}
