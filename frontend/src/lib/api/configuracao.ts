import { fetchComTimeout, MensagemErroConexao } from './httpClient';

/**
 * Espelha `Synclass.Domain.Configuracoes.ModeloAgendamento` do backend —
 * trafega como inteiro no contrato de Api, mesma decisão já tomada para
 * `diaSemana` na issue #6 (ver
 * docs/specs/7-modelo-agendamento/implementation.md#contrato-de-api).
 */
export enum ModeloAgendamento {
  Vago = 0,
  Fixo = 1,
  Hibrido = 2,
}

export type ObterConfiguracaoResultado =
  | { sucesso: true; definida: true; modeloAgendamento: ModeloAgendamento }
  | { sucesso: true; definida: false }
  | { sucesso: false; mensagem: string };

export type DefinirModeloAgendamentoResultado =
  { sucesso: true; modeloAgendamento: ModeloAgendamento } | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir a operação. Tente novamente.';

/**
 * Envolve os endpoints de configuração do Professor (issue #7) atrás de uma
 * interface própria (ver docs/spec/code-style.md#dependências). 404 é um
 * resultado esperado (configuração ainda não definida), não um erro — o
 * chamador (gate de `horarios.tsx`) usa `definida: false` para decidir o que
 * renderizar, sem precisar inspecionar `response.status` fora deste módulo.
 */
export async function obterConfiguracao(professorId: string): Promise<ObterConfiguracaoResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(`/professores/${professorId}/configuracao`);
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  if (response.status === 404) {
    return { sucesso: true, definida: false };
  }
  if (!response.ok) {
    return { sucesso: false, mensagem: MensagemErroGenerica };
  }

  const corpo = await response.json();
  return { sucesso: true, definida: true, modeloAgendamento: corpo.modeloAgendamento };
}

export async function definirModeloAgendamento(
  professorId: string,
  modeloAgendamento: ModeloAgendamento,
): Promise<DefinirModeloAgendamentoResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(
      `/professores/${professorId}/configuracao/modelo-agendamento`,
      {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ modeloAgendamento }),
      },
    );
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }
  return { sucesso: true, modeloAgendamento: corpo.modeloAgendamento };
}
