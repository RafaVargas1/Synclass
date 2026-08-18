import { fetchComTimeout, MensagemErroConexao } from './httpClient';

/**
 * Espelha `ValorDevidoResponse` do backend (issue #12). `valor` é `null`
 * quando `semRegraDefinida` é `true` — nunca `0` (critério de aceite 3).
 */
export type ValorDevidoPorMatricula = {
  matriculaId: string;
  alunoUsuarioId: string | null;
  nome: string;
  valor: number | null;
  semRegraDefinida: boolean;
};

/** Período `[inicio, fim)` no formato `yyyy-MM-dd`, mesmo contrato do backend. */
export type PeriodoConsultaInput = {
  inicio: string;
  fim: string;
};

export type ListarValorDevidoResultado =
  { sucesso: true; valoresDevidos: ValorDevidoPorMatricula[] } | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir a operação. Tente novamente.';

/**
 * Envolve o `fetch` de GET /professores/{professorId}/valor-devido (issue
 * #12) atrás de uma interface própria (ver docs/spec/code-style.md#dependências).
 * `periodo` ausente omite `inicio`/`fim` da query string — o backend usa o
 * mês corrente como default, mesma decisão do contrato de Api.
 */
export async function listarValorDevido(
  professorId: string,
  periodo?: PeriodoConsultaInput,
): Promise<ListarValorDevidoResultado> {
  return buscarValorDevido(caminhoComQuery(`/professores/${professorId}/valor-devido`, periodo));
}

/**
 * Envolve o `fetch` de GET /alunos/valor-devido (issue #13) — Aluno
 * autenticado consultando o próprio total devido, detalhado por Professor.
 * Sem parâmetro de id: `alunoUsuarioId` vem do token da sessão anexado por
 * `fetchComTimeout`, nunca de rota/query (mesmo padrão de `marcacoes.ts`).
 */
export async function listarValorDevidoDoAluno(periodo?: PeriodoConsultaInput): Promise<ListarValorDevidoResultado> {
  return buscarValorDevido(caminhoComQuery('/alunos/valor-devido', periodo));
}

/**
 * Fetch + tratamento de resposta compartilhado pelas duas direções (issue
 * #12/#13) — só o caminho muda entre elas, ver implementation.md da #13.
 */
async function buscarValorDevido(caminho: string): Promise<ListarValorDevidoResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(caminho);
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: MensagemErroGenerica };
  }
  return { sucesso: true, valoresDevidos: (corpo as ValorDevidoPorMatricula[] | null) ?? [] };
}

function caminhoComQuery(base: string, periodo?: PeriodoConsultaInput): string {
  if (!periodo) {
    return base;
  }
  return `${base}?inicio=${periodo.inicio}&fim=${periodo.fim}`;
}
