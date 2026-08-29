import { Platform } from 'react-native';

const ClientId = process.env.EXPO_PUBLIC_APPLE_CLIENT_ID ?? '';

/**
 * Forma mínima do Sign in with Apple JS injetado no `window` pela página
 * web — declaração local porque a lib de tipos da Apple não faz parte do
 * projeto (ver docs/spec/code-style.md#dependências: API de terceiro é
 * envolvida com uma interface fina própria). O SDK define a propriedade
 * `AppleID` no `window`, espelhando a forma de `GoogleIdentityServicesWeb`
 * de `google.ts` (que expõe `google` no `window`).
 */
type AppleIdWeb = {
  AppleID?: {
    auth?: {
      init: (config: Record<string, unknown>) => void;
      signIn: () => Promise<RespostaIdTokenApple>;
    };
  };
};

/**
 * Resposta do Sign in with Apple JS com <c>usePopup: true</c> — o SDK
 * resolve a Promise com o payload do OAuth vindo do popup (ver
 * implementation.md#frontend-botão-e-integração-com-sign-in-with-apple-js).
 */
export type RespostaIdTokenApple = {
  authorization?: {
    code?: string;
    id_token?: string;
    state?: string;
  };
  user?: {
    email?: string;
    name?: { firstName?: string; lastName?: string };
  };
  error?: string;
};

let jaInicializado = false;

/**
 * Init do Sign in with Apple JS, análogo ao GoogleClientId de google.ts —
 * chamado sob demanda (não no escopo do módulo): `expo-router` renderiza
 * este módulo também durante o SSR do bundle web, onde `window` ainda não
 * existe — inicializar no top-level (mesmo com a guarda `Platform.OS ===
 * 'web'`, que não distingue SSR de browser real) derruba a renderização de
 * toda tela que importa `BotaoLoginApple` com
 * `Cannot read properties of undefined (reading 'AppleID')`. Adiada para a
 * primeira chamada de `obterIdTokenApple`, que só acontece a partir de um
 * toque do usuário no browser real. O AppleClientId aqui é o Services ID da
 * Apple (`EXPO_PUBLIC_APPLE_CLIENT_ID`), diferente do AppleClientId do
 * backend — são registros distintos no Apple Developer (mesmo conceito do
 * Google ter Client ID web vs Client ID iOS).
 */
function inicializarSeNecessario(janelaComApple: Window & typeof globalThis & AppleIdWeb): void {
  if (jaInicializado || !janelaComApple.AppleID?.auth || !ClientId) {
    return;
  }
  janelaComApple.AppleID.auth.init({
    clientId: ClientId,
    scope: 'email name',
    redirectURI: `${process.env.EXPO_PUBLIC_APP_URL ?? 'http://localhost:8081'}/auth/apple/callback`,
    usePopup: true,
  });
  jaInicializado = true;
}

/**
 * idToken da Apple obtido via Sign in with Apple JS (web).
 *
 * O SDK só existe na plataforma web (`appleid.cdn-apple.com`); no nativo o
 * login Apple é a task #213 (arquivo separado). A chamada é condicionada a
 * `Platform.OS === 'web'` (mesmo racional de `google.ts` ramificar por
 * plataforma) — sem essa guarda, a referência a `window.AppleID` no bundle
 * nativo lançaria ReferenceError em qualquer tela que importa este arquivo.
 */
export async function obterIdTokenApple(): Promise<string | null> {
  if (Platform.OS !== 'web' || typeof window === 'undefined') {
    return null;
  }

  const janelaComApple = globalThis.window as Window & typeof globalThis & AppleIdWeb;
  inicializarSeNecessario(janelaComApple);
  const appleAuth = janelaComApple.AppleID?.auth;
  if (!appleAuth) {
    return null; // script do Sign in with Apple ainda não carregado na página
  }

  try {
    const resposta = await appleAuth.signIn();
    return resposta.authorization?.id_token ?? null;
  } catch {
    return null; // usuário cancelou o popup ou falha do provedor — mesmo padrão de obterIdTokenGoogle()
  }
}
