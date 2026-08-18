import { fetchComTimeout, MensagemErroConexao } from './httpClient';

export type AulaProxima = {
  horarioId: string;
  data: string;
  diaSemana: number;
  horaInicio: string;
  duracaoMinutos: number;
  podeCancelar: boolean;
  cancelavelAte: string;
  prazoCancelamentoMinutos: number;
};

export type CancelamentoAula = {
  id: string;
  aulaId: string;
  matriculaId: string;
  canceladoEm: string;
};

export type ListarProximasAulasResultado =
  { sucesso: true; aulas: AulaProxima[] } | { sucesso: false; mensagem: string };

export type CancelarAulaResultado =
  { sucesso: true; cancelamento: CancelamentoAula } | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir a operação. Tente novamente.';

/**
 * Envolve o `fetch` dos endpoints de cancelamento de aula (issue #10) atrás
 * de uma interface própria (ver docs/spec/code-style.md#dependências),
 * mesmo envelope de `marcacoes.ts` (issue #9): nunca lança para erros de
 * negócio (fora do prazo, horário/Aluno não vinculado) ou de rede — sempre
 * devolve um resultado tipado. `professorId` continua explícito (identifica
 * o Professor sendo navegado pelo Aluno, não quem chama); `matriculaId` não
 * é enviado pelo cliente (issue #23) — a Api resolve a matrícula do Aluno
 * autenticado a partir do token.
 */
export async function listarProximasAulas(professorId: string): Promise<ListarProximasAulasResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(caminhoProximasAulas(professorId));
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }
  return { sucesso: true, aulas: (corpo as AulaProxima[] | null) ?? [] };
}

export async function cancelarAula(
  professorId: string,
  horarioId: string,
  data: string,
): Promise<CancelarAulaResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(caminhoCancelamentos(professorId, horarioId, data), { method: 'POST' });
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }
  return { sucesso: true, cancelamento: corpo as CancelamentoAula };
}

function caminhoProximasAulas(professorId: string): string {
  return `/professores/${professorId}/horarios/proximas-aulas`;
}

function caminhoCancelamentos(professorId: string, horarioId: string, data: string): string {
  return `/professores/${professorId}/horarios/${horarioId}/aulas/${data}/cancelamentos`;
}
