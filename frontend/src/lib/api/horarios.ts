import { fetchComTimeout, MensagemErroConexao } from './httpClient';

/**
 * Espelha `Synclass.Domain.Horarios.TipoMarcacao` do backend — trafega como
 * inteiro (issue #76, mesmo padrão de `ModeloAgendamento` em
 * `lib/api/configuracao.ts`).
 */
export enum TipoMarcacao {
  Livre = 0,
  Fixo = 1,
  Hibrido = 2,
}

export type Horario = {
  id: string;
  diaSemana: number;
  horaInicio: string;
  duracaoMinutos: number;
  limiteAlunos: number;
  tipoMarcacao: TipoMarcacao;
};

export type CriarHorarioInput = {
  diaSemana: number;
  horaInicio: string;
  duracaoMinutos: number;
  limiteAlunos: number;
  tipoMarcacao: TipoMarcacao;
};

export type CriarHorarioResultado =
  { sucesso: true; horario: Horario } | { sucesso: false; mensagem: string };

export type ListarHorariosResultado =
  { sucesso: true; horarios: Horario[] } | { sucesso: false; mensagem: string };

export type RemoverHorarioResultado = { sucesso: true } | { sucesso: false; mensagem: string };

export type AlterarTipoMarcacaoResultado =
  { sucesso: true; horario: Horario } | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir a operação. Tente novamente.';

/**
 * Envolve o `fetch` dos endpoints de horários (issue #6) atrás de uma
 * interface própria (ver docs/spec/code-style.md#dependências): nunca lança
 * para erros de negócio (conflito, alunos alocados) ou de rede — sempre
 * devolve um resultado tipado.
 */
export async function criarHorario(
  professorId: string,
  input: CriarHorarioInput,
): Promise<CriarHorarioResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(`/professores/${professorId}/horarios`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(input),
    });
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }
  return { sucesso: true, horario: corpo as Horario };
}

export async function listarHorarios(professorId: string): Promise<ListarHorariosResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(`/professores/${professorId}/horarios`);
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: MensagemErroGenerica };
  }
  return { sucesso: true, horarios: (corpo as Horario[] | null) ?? [] };
}

export async function removerHorario(
  professorId: string,
  horarioId: string,
): Promise<RemoverHorarioResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(`/professores/${professorId}/horarios/${horarioId}`, {
      method: 'DELETE',
    });
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  if (!response.ok) {
    const corpo = await response.json().catch(() => null);
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }
  return { sucesso: true };
}

export async function alterarTipoMarcacaoHorario(
  professorId: string,
  horarioId: string,
  tipoMarcacao: TipoMarcacao,
): Promise<AlterarTipoMarcacaoResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(`/professores/${professorId}/horarios/${horarioId}`, {
      method: 'PATCH',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ tipoMarcacao }),
    });
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }
  return { sucesso: true, horario: corpo as Horario };
}
