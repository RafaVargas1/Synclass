import type { Alocacao } from './alocacoes';
import { fetchComTimeout, MensagemErroConexao } from './httpClient';

export type HorarioVago = {
  id: string;
  diaSemana: number;
  horaInicio: string;
  duracaoMinutos: number;
  vagasRestantes: number;
};

export type ListarHorariosVagosResultado =
  { sucesso: true; horarios: HorarioVago[] } | { sucesso: false; mensagem: string };

export type MarcarHorarioResultado =
  { sucesso: true; alocacao: Alocacao } | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir a operação. Tente novamente.';

/**
 * Envolve o `fetch` dos endpoints de marcação livre do Aluno (issue #9)
 * atrás de uma interface própria (ver docs/spec/code-style.md#dependências),
 * mesmo envelope de `alocacoes.ts` (issue #8): nunca lança para erros de
 * negócio (modelo não permite, horário lotado, Aluno não vinculado, já
 * marcado) ou de rede — sempre devolve um resultado tipado. `professorId`
 * continua explícito (identifica o Professor sendo navegado pelo Aluno, não
 * quem chama); `matriculaId` não é mais enviado pelo cliente (issue #23) —
 * a Api resolve a matrícula do Aluno autenticado a partir do token.
 */
export async function listarHorariosVagos(professorId: string): Promise<ListarHorariosVagosResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(caminhoVagos(professorId));
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }
  return { sucesso: true, horarios: (corpo as HorarioVago[] | null) ?? [] };
}

export async function marcarHorario(professorId: string, horarioId: string): Promise<MarcarHorarioResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(caminhoMarcacoes(professorId, horarioId), { method: 'POST' });
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }
  return { sucesso: true, alocacao: corpo as Alocacao };
}

function caminhoVagos(professorId: string): string {
  return `/professores/${professorId}/horarios/vagos`;
}

function caminhoMarcacoes(professorId: string, horarioId: string): string {
  return `/professores/${professorId}/horarios/${horarioId}/marcacoes`;
}
