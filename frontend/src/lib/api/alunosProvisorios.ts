export type CadastroAlunoProvisorioInput = {
  professorId: string;
  nome: string;
  identificador: string;
};

export type CadastroAlunoProvisorioResultado =
  | { sucesso: true; nome: string; identificador: string }
  | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir o cadastro. Tente novamente.';
const MensagemErroConexao =
  'Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.';

/**
 * URL base da Api do Synclass — mesma variável usada por
 * `lib/api/professores.ts` (ver backend/README.md#debug).
 */
const ApiBaseUrl = process.env.EXPO_PUBLIC_API_URL ?? 'http://localhost:5005';

/**
 * Tempo máximo de espera pela resposta antes de tratar como erro de conexão
 * — mesmo valor e justificativa de `lib/api/professores.ts`.
 */
const TimeoutRequisicaoMs = 15000;

/**
 * Envolve o `fetch` de POST /professores/{professorId}/alunos-provisorios
 * atrás de uma interface própria (ver docs/spec/code-style.md#dependências):
 * nunca lança para erros de negócio (nome/identificador inválido,
 * identificador duplicado) ou de rede — sempre devolve um resultado
 * tipado, para a tela exibir a mensagem sem travar.
 */
export async function cadastrarAlunoProvisorio(
  input: CadastroAlunoProvisorioInput,
): Promise<CadastroAlunoProvisorioResultado> {
  let response: Response;
  try {
    response = await postCadastro(input);
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  return interpretarResposta(response);
}

function postCadastro(input: CadastroAlunoProvisorioInput): Promise<Response> {
  const controle = new AbortController();
  const timeoutId = setTimeout(() => controle.abort(), TimeoutRequisicaoMs);

  return fetch(`${ApiBaseUrl}/professores/${input.professorId}/alunos-provisorios`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ nome: input.nome, identificador: input.identificador }),
    signal: controle.signal,
  }).finally(() => clearTimeout(timeoutId));
}

async function interpretarResposta(response: Response): Promise<CadastroAlunoProvisorioResultado> {
  const corpo = await response.json().catch(() => null);

  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }

  return { sucesso: true, nome: corpo?.nome ?? '', identificador: corpo?.identificador ?? '' };
}
