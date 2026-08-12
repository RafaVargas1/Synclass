/**
 * Envelope de `fetch` compartilhado por todos os módulos de `lib/api/*`
 * (ver docs/spec/code-style.md#dependências — bibliotecas de terceiros
 * atrás de uma interface fina própria). Extraído de `professores.ts` ao
 * criar `horarios.ts` (issue #6) para não duplicar a base da Api, o timeout
 * e a mensagem de erro de conexão entre os dois módulos.
 */

/**
 * URL base da Api do Synclass. Configurável via variável de ambiente pública
 * do Expo (`EXPO_PUBLIC_*`); usa a porta de desenvolvimento local por padrão
 * (ver backend/README.md#debug).
 */
const ApiBaseUrl = process.env.EXPO_PUBLIC_API_URL ?? 'http://localhost:5005';

/**
 * Tempo máximo de espera pela resposta antes de tratar como erro de conexão.
 * Sem isso, uma conexão que abre mas nunca responde (proxy travado, app em
 * segundo plano no mobile) deixa a tela presa em estado de carregamento
 * para sempre — ver Cenário 6 da issue #1 ("sem travar ou mostrar tela em
 * branco").
 */
const TimeoutRequisicaoMs = 15000;

export const MensagemErroConexao =
  'Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.';

/**
 * `fetch` com base URL e timeout já aplicados. Nunca resolve para uma
 * resposta "pendurada" — ou devolve dentro do timeout, ou rejeita (o
 * chamador decide como transformar isso num resultado de erro tipado).
 */
export function fetchComTimeout(caminho: string, init?: RequestInit): Promise<Response> {
  const controle = new AbortController();
  const timeoutId = setTimeout(() => controle.abort(), TimeoutRequisicaoMs);

  return fetch(`${ApiBaseUrl}${caminho}`, { ...init, signal: controle.signal }).finally(() =>
    clearTimeout(timeoutId),
  );
}
