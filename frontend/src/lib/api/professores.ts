import { fetchComTimeout, MensagemErroConexao } from './httpClient';

export type CadastroProfessorInput = {
  nome: string;
  contato: string;
};

export type CadastroProfessorResultado =
  { sucesso: true; nome: string } | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir o cadastro. Tente novamente.';

/**
 * Envolve o `fetch` de POST /professores/cadastro atrás de uma interface
 * própria (ver docs/spec/code-style.md#dependências): nunca lança para erros
 * de negócio (contato inválido/duplicado) ou de rede — sempre devolve um
 * resultado tipado, para a tela exibir a mensagem sem travar.
 */
export async function cadastrarProfessor(
  input: CadastroProfessorInput,
): Promise<CadastroProfessorResultado> {
  let response: Response;
  try {
    response = await postCadastro(input);
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  return interpretarResposta(response);
}

function postCadastro(input: CadastroProfessorInput): Promise<Response> {
  return fetchComTimeout('/professores/cadastro', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  });
}

async function interpretarResposta(response: Response): Promise<CadastroProfessorResultado> {
  const corpo = await response.json().catch(() => null);

  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }

  return { sucesso: true, nome: corpo?.nome ?? '' };
}

export type VerificarContatoResultado = { identidadeExistente: boolean; nome: string | null };

const IdentidadeInexistente: VerificarContatoResultado = { identidadeExistente: false, nome: null };

/**
 * Envolve o `fetch` de GET /professores/verificar-contato (issue #27):
 * alimenta o campo Nome readonly do formulário quando o contato já pertence
 * a uma identidade existente. Fail-open — erro de rede ou resposta
 * inesperada nunca trava o cadastro, só deixa o campo Nome editável (pior
 * caso é o comportamento original da RN da issue #20 se aplicar no fim).
 */
export async function verificarContatoProfessor(contato: string): Promise<VerificarContatoResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(`/professores/verificar-contato?contato=${encodeURIComponent(contato)}`);
  } catch {
    return IdentidadeInexistente;
  }

  if (!response.ok) {
    return IdentidadeInexistente;
  }
  const corpo = await response.json().catch(() => null);
  return corpo?.identidadeExistente
    ? { identidadeExistente: true, nome: corpo.nome ?? null }
    : IdentidadeInexistente;
}
