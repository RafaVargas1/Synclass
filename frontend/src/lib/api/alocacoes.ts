import { fetchComTimeout, MensagemErroConexao } from './httpClient';

export type Alocacao = {
  id: string;
  horarioId: string;
  matriculaId: string;
  createdAt: string;
};

export type AlocarAlunoResultado =
  { sucesso: true; alocacao: Alocacao } | { sucesso: false; mensagem: string };

export type ListarAlocacoesResultado =
  { sucesso: true; alocacoes: Alocacao[] } | { sucesso: false; mensagem: string };

export type DesalocarAlunoResultado = { sucesso: true } | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir a operação. Tente novamente.';

/**
 * Envolve o `fetch` dos endpoints de alocação de Aluno em horário (issue #8)
 * atrás de uma interface própria (ver docs/spec/code-style.md#dependências):
 * nunca lança para erros de negócio (modelo Vago, horário lotado, Aluno não
 * vinculado, já alocado) ou de rede — sempre devolve um resultado tipado.
 */
export async function alocarAluno(
  professorId: string,
  horarioId: string,
  matriculaId: string,
): Promise<AlocarAlunoResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(caminhoAlocacoes(professorId, horarioId), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ matriculaId }),
    });
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }
  return { sucesso: true, alocacao: corpo as Alocacao };
}

export async function listarAlocacoes(
  professorId: string,
  horarioId: string,
): Promise<ListarAlocacoesResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(caminhoAlocacoes(professorId, horarioId));
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: MensagemErroGenerica };
  }
  return { sucesso: true, alocacoes: (corpo as Alocacao[] | null) ?? [] };
}

export async function desalocarAluno(
  professorId: string,
  horarioId: string,
  matriculaId: string,
): Promise<DesalocarAlunoResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(`${caminhoAlocacoes(professorId, horarioId)}/${matriculaId}`, {
      method: 'DELETE',
    });
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  if (!response.ok) {
    return { sucesso: false, mensagem: MensagemErroGenerica };
  }
  return { sucesso: true };
}

function caminhoAlocacoes(professorId: string, horarioId: string): string {
  return `/professores/${professorId}/horarios/${horarioId}/alocacoes`;
}
