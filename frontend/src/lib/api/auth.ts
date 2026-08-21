export type SolicitarCodigoInput = {
  contato: string;
};

export type SolicitarCodigoResultado = { sucesso: true } | { sucesso: false; mensagem: string };

export type ConfirmarCodigoInput = {
  contato: string;
  codigo: string;
};

export type ConfirmarCodigoResultado =
  | { sucesso: true; token: string; nome: string; papeis: string[] }
  | { sucesso: false; mensagem: string };

export type LoginGoogleResultado =
  | { sucesso: true; token: string; nome: string; papeis: string[] }
  | { sucesso: false; cadastroPendente: true; email: string }
  | { sucesso: false; mensagem: string };

type CorpoResposta = Record<string, unknown> | null;

type RespostaComando = { ok: true; corpo: CorpoResposta } | { ok: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir a solicitação. Tente novamente.';
const MensagemErroConexao =
  'Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.';

/**
 * URL base da Api do Synclass — mesma configuração de src/lib/api/professores.ts.
 */
const ApiBaseUrl = process.env.EXPO_PUBLIC_API_URL ?? 'http://localhost:5005';

/**
 * Tempo máximo de espera pela resposta antes de tratar como erro de conexão
 * (mesmo raciocínio de src/lib/api/professores.ts).
 */
const TimeoutRequisicaoMs = 15000;

/**
 * Envolve o `fetch` de POST /auth/codigo atrás de uma interface própria (ver
 * docs/spec/code-style.md#dependências): nunca lança, sempre devolve um
 * resultado tipado — inclusive quando o contato não tem identidade plena
 * (issue #18), para a tela exibir a mensagem sem travar.
 */
export async function solicitarCodigo(input: SolicitarCodigoInput): Promise<SolicitarCodigoResultado> {
  const resposta = await enviarComando('/auth/codigo', input);
  return resposta.ok ? { sucesso: true } : { sucesso: false, mensagem: resposta.mensagem };
}

/**
 * Envolve o `fetch` de POST /auth/confirmacao — mesmo contrato de
 * `solicitarCodigo`, devolvendo o token e os papéis do usuário em caso de
 * sucesso.
 */
export async function confirmarCodigo(input: ConfirmarCodigoInput): Promise<ConfirmarCodigoResultado> {
  const resposta = await enviarComando('/auth/confirmacao', input);
  if (!resposta.ok) {
    return { sucesso: false, mensagem: resposta.mensagem };
  }

  return {
    sucesso: true,
    token: (resposta.corpo?.token as string) ?? '',
    nome: (resposta.corpo?.nome as string) ?? '',
    papeis: (resposta.corpo?.papeis as string[]) ?? [],
  };
}

/**
 * Envolve o `fetch` de POST /auth/google (issue #65): idToken do Google já
 * validado na Api. Mesmo contrato tipado de `solicitarCodigo`/
 * `confirmarCodigo` — nunca lança — acrescido do desfecho de cadastro
 * pendente, quando o e-mail do idToken ainda não corresponde a nenhum
 * usuário (a Api não cria conta implicitamente; ver
 * docs/spec/business-rules.md#identidade-de-usuário).
 */
export async function loginComGoogle(idToken: string): Promise<LoginGoogleResultado> {
  const resposta = await enviarComando('/auth/google', { idToken });
  if (!resposta.ok) {
    return { sucesso: false, mensagem: resposta.mensagem };
  }

  if (resposta.corpo?.cadastroPendente === true) {
    return {
      sucesso: false,
      cadastroPendente: true,
      email: (resposta.corpo.email as string) ?? '',
    };
  }

  return {
    sucesso: true,
    token: (resposta.corpo?.token as string) ?? '',
    nome: (resposta.corpo?.nome as string) ?? '',
    papeis: (resposta.corpo?.papeis as string[]) ?? [],
  };
}

/**
 * Ponto único de tratamento de erro (conexão e erro de negócio) para os dois
 * comandos acima, evitando duplicar o parsing de resposta em cada um.
 */
async function enviarComando(caminho: string, corpo: unknown): Promise<RespostaComando> {
  let response: Response;
  try {
    response = await postJson(caminho, corpo);
  } catch {
    return { ok: false, mensagem: MensagemErroConexao };
  }

  // Corpo corrompido/ilegível nunca é sucesso, mesmo com HTTP 200 — sem
  // isso, um corpo malformado virava `{ sucesso: true, token: '' }`, que a
  // tela de verificação salvava como sessão válida (dev-review do PR #25,
  // issue #18).
  let corpoResposta: CorpoResposta;
  try {
    corpoResposta = await response.json();
  } catch {
    return { ok: false, mensagem: MensagemErroGenerica };
  }

  if (!response.ok) {
    return { ok: false, mensagem: (corpoResposta?.mensagem as string) ?? MensagemErroGenerica };
  }

  return { ok: true, corpo: corpoResposta };
}

function postJson(caminho: string, corpo: unknown): Promise<Response> {
  const controle = new AbortController();
  const timeoutId = setTimeout(() => controle.abort(), TimeoutRequisicaoMs);

  return fetch(`${ApiBaseUrl}${caminho}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(corpo),
    signal: controle.signal,
  }).finally(() => clearTimeout(timeoutId));
}
