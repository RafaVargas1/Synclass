import { Platform } from 'react-native';

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

async function obterIdTokenNativo(): Promise<string | null> {
  // Google Sign-In nativo (`expo-auth-session` / `@react-native-google-signin`) ainda
  // não está instalado nesta versão — o fluxo nativo fica propositalmente
  // desligado (devolve `null`, nunca lança) até a integração real entrar na
  // issue #65. O contrato de retorno já fica fixado agora para a tela
  // tratar o cancelamento como `null`, não como exceção.
  return null;
}
