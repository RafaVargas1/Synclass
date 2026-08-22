import { paraDataISO, proximoDia } from '@/lib/formatarData';

import { fetchComTimeout, MensagemErroConexao } from './httpClient';

/** Espelha `StatusHistoricoFrequenciaResponse` do backend (issue #16). */
export type StatusHistoricoFrequencia = 'NaoRegistrada' | 'Presente' | 'Ausente' | 'Cancelada';

/** Espelha `AulaFrequenciaHistoricoResponse` do backend (issue #16). */
export type AulaFrequenciaHistorico = {
  horarioId: string;
  data: string;
  diaSemana: number;
  horaInicio: string;
  status: StatusHistoricoFrequencia;
};

/**
 * Espelha `HistoricoFrequenciaPorProfessorResponse` do backend (issue #16) —
 * mesma forma de agrupamento de `ValorDevidoPorMatricula` (issue #13), nunca
 * somado/misturado entre Professores.
 */
export type HistoricoFrequenciaPorProfessor = {
  professorId: string;
  nomeProfessor: string;
  aulas: AulaFrequenciaHistorico[];
};

/** Período `[inicio, fim)` no formato `yyyy-MM-dd`, mesmo contrato do backend. */
export type PeriodoConsultaInput = {
  inicio: string;
  fim: string;
};

export type ListarHistoricoFrequenciaResultado =
  | { sucesso: true; historico: HistoricoFrequenciaPorProfessor[] }
  | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir a operação. Tente novamente.';

/**
 * Envolve o `fetch` de GET /alunos/historico-frequencia (issue #16) — Aluno
 * autenticado consultando o próprio histórico, detalhado por Professor. Sem
 * parâmetro de id: `alunoUsuarioId` vem do token da sessão anexado por
 * `fetchComTimeout`, mesmo padrão de `listarValorDevidoDoAluno`
 * (`lib/api/valorDevido.ts`, issue #13).
 */
export async function listarHistoricoFrequenciaDoAluno(
  periodo?: PeriodoConsultaInput,
): Promise<ListarHistoricoFrequenciaResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(caminhoComQuery('/alunos/historico-frequencia', periodo));
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: MensagemErroGenerica };
  }
  return { sucesso: true, historico: (corpo as HistoricoFrequenciaPorProfessor[] | null) ?? [] };
}

/**
 * Período `[hoje-dias+1, proximoDia(hoje))` = "os últimos `dias` incluindo hoje",
 * usado pelo resumo de frequência recente do Painel (issue #167). Mesmo
 * racional de cálculo deslocado de datas de `calcularPeriodoTodos`
 * (`lib/api/valorDevido.ts`); `fim` exclusivo = dia seguinte a hoje, então
 * `proximoDia(hoje)` inclui o dia de hoje na consulta (mesmo contrato
 * `[inicio, fim)` da Api).
 */
export function calcularPeriodoUltimosNDias(hoje: Date, dias: number): PeriodoConsultaInput {
  const inicio = new Date(hoje.getFullYear(), hoje.getMonth(), hoje.getDate() - (dias - 1));
  return { inicio: paraDataISO(inicio), fim: proximoDia(paraDataISO(hoje)) };
}

function caminhoComQuery(base: string, periodo?: PeriodoConsultaInput): string {
  if (!periodo) {
    return base;
  }
  return `${base}?inicio=${periodo.inicio}&fim=${periodo.fim}`;
}
