import { fetchComTimeout, MensagemErroConexao } from './httpClient';

export type RegistroFrequenciaItem = { matriculaId: string; presente: boolean };

export type RegistroFrequencia = {
  matriculaId: string;
  presente: boolean;
  confirmadoPeloAluno: boolean | null;
};

export type RegistrarFrequenciaResultado =
  { sucesso: true; registros: RegistroFrequencia[] } | { sucesso: false; mensagem: string };

export type ConfirmacaoPresenca = { aulaId: string; matriculaId: string; confirmadoPeloAluno: boolean };

export type ConfirmarPresencaResultado =
  { sucesso: true; confirmacao: ConfirmacaoPresenca } | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir a operação. Tente novamente.';

/**
 * Envolve o `fetch` do endpoint de registro de frequência (issue #14) atrás
 * de uma interface própria (ver docs/spec/code-style.md#dependências),
 * mesmo envelope de `cancelamentos.ts`: nunca lança para erros de negócio
 * (`matriculaId` não alocada, horário inexistente) ou de rede — sempre
 * devolve um resultado tipado. Sem `professorId` (issue #23) — o Professor é
 * sempre quem está logado, a Api deriva a identidade do token da sessão.
 */
export async function registrarFrequencia(
  horarioId: string,
  data: string,
  registros: RegistroFrequenciaItem[],
): Promise<RegistrarFrequenciaResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(caminhoFrequencias(horarioId, data), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ registros }),
    });
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }
  return { sucesso: true, registros: (corpo as RegistroFrequencia[] | null) ?? [] };
}

function caminhoFrequencias(horarioId: string, data: string): string {
  return `/professores/horarios/${horarioId}/aulas/${data}/frequencias`;
}

/**
 * Envolve o `fetch` do endpoint de confirmação de presença pelo Aluno (issue
 * #15), mesmo envelope de `cancelamentos.ts#cancelarAula`: nunca lança para
 * erros de negócio (Aluno já cancelou esta ocorrência) ou de rede — sempre
 * devolve um resultado tipado. `professorId` continua explícito (identifica
 * o Professor sendo navegado pelo Aluno); `matriculaId` não é enviada pelo
 * cliente (issue #23) — a Api resolve a matrícula do Aluno autenticado a
 * partir do token.
 */
export async function confirmarPresenca(
  professorId: string,
  horarioId: string,
  data: string,
): Promise<ConfirmarPresencaResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(caminhoConfirmacaoPresenca(professorId, horarioId, data), { method: 'POST' });
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }
  return { sucesso: true, confirmacao: corpo as ConfirmacaoPresenca };
}

function caminhoConfirmacaoPresenca(professorId: string, horarioId: string, data: string): string {
  return `/professores/${professorId}/horarios/${horarioId}/aulas/${data}/confirmacao-presenca`;
}
