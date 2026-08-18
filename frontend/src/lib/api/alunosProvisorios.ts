import { fetchComTimeout, MensagemErroConexao } from './httpClient';

export type CadastroAlunoProvisorioInput = {
  nome: string;
  identificador: string;
};

export type CadastroAlunoProvisorioResultado =
  | { sucesso: true; nome: string; identificador: string }
  | { sucesso: false; mensagem: string };

export type AlunoProvisorio = {
  matriculaId: string;
  nome: string;
  identificador: string;
};

export type ListarAlunosProvisoriosResultado =
  { sucesso: true; alunos: AlunoProvisorio[] } | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir a operação. Tente novamente.';

/**
 * Envolve o `fetch` de POST /professores/alunos-provisorios atrás de uma
 * interface própria (ver docs/spec/code-style.md#dependências): nunca lança
 * para erros de negócio (nome/identificador inválido, identificador
 * duplicado) ou de rede — sempre devolve um resultado tipado, para a tela
 * exibir a mensagem sem travar. `professorId` não é mais enviado pelo
 * cliente (issue #23) — a Api deriva o Professor do token da sessão
 * (`Authorization: Bearer`, anexado por `fetchComTimeout`).
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
  return fetchComTimeout('/professores/alunos-provisorios', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ nome: input.nome, identificador: input.identificador }),
  });
}

async function interpretarResposta(response: Response): Promise<CadastroAlunoProvisorioResultado> {
  const corpo = await response.json().catch(() => null);

  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }

  return { sucesso: true, nome: corpo?.nome ?? '', identificador: corpo?.identificador ?? '' };
}

/**
 * Envolve o `fetch` de GET /professores/alunos-provisorios (issue #8,
 * alimenta o seletor de Aluno de `HorarioAlocacaoCard`) — mesma política de
 * nunca lançar de `cadastrarAlunoProvisorio` acima. Sem `professorId`
 * (issue #23), mesmo motivo.
 */
export async function listarAlunosProvisorios(): Promise<ListarAlunosProvisoriosResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout('/professores/alunos-provisorios');
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: MensagemErroGenerica };
  }
  return { sucesso: true, alunos: (corpo as AlunoProvisorio[] | null) ?? [] };
}
