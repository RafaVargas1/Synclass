import { fetchComTimeout, MensagemErroConexao } from './httpClient';

/**
 * Espelha `Synclass.Domain.Cobrancas.TipoRegraDeCobranca` do backend —
 * trafega como string no contrato de Api (diferente de `diaSemana`/
 * `modeloAgendamento`, que trafegam como inteiro), mesma decisão do
 * discriminador TPH do EF Core (ver
 * docs/specs/11-regra-cobranca/implementation.md#contrato-de-api).
 */
export type TipoRegraDeCobranca = 'ValorPorAula' | 'FixoMensal' | 'FixoPorAula';

export type RegraDeCobranca = {
  matriculaId: string;
  tipo: TipoRegraDeCobranca;
  valor: number;
  frequenciaSemanalContratada: number | null;
};

export type DefinirRegraDeCobrancaInput = {
  tipo: TipoRegraDeCobranca;
  valor: number;
  frequenciaSemanalContratada: number | null;
};

export type ObterRegraDeCobrancaResultado =
  | { sucesso: true; definida: true; regra: RegraDeCobranca }
  | { sucesso: true; definida: false }
  | { sucesso: false; mensagem: string };

export type DefinirRegraDeCobrancaResultado =
  { sucesso: true; regra: RegraDeCobranca } | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir a operação. Tente novamente.';
const JsonHeaders = { 'Content-Type': 'application/json' };

function caminho(professorId: string, matriculaId: string): string {
  return `/professores/${professorId}/matriculas/${matriculaId}/regra-de-cobranca`;
}

/**
 * Envolve os endpoints de regra de cobrança (issue #11) atrás de uma
 * interface própria (ver docs/spec/code-style.md#dependências). 404 é um
 * resultado esperado (sem regra configurada — critério de aceite 1), não um
 * erro, mesmo padrão de `obterConfiguracao`.
 */
export async function obterRegraDeCobranca(
  professorId: string,
  matriculaId: string,
): Promise<ObterRegraDeCobrancaResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(caminho(professorId, matriculaId));
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  if (response.status === 404) {
    return { sucesso: true, definida: false };
  }
  const corpo = response.ok ? await response.json().catch(() => null) : null;
  if (!corpo) {
    return { sucesso: false, mensagem: MensagemErroGenerica };
  }
  return { sucesso: true, definida: true, regra: corpo as RegraDeCobranca };
}

export async function definirRegraDeCobranca(
  professorId: string,
  matriculaId: string,
  input: DefinirRegraDeCobrancaInput,
): Promise<DefinirRegraDeCobrancaResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(caminho(professorId, matriculaId), {
      method: 'PUT',
      body: JSON.stringify(input),
      headers: JsonHeaders,
    });
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok || !corpo) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }
  return { sucesso: true, regra: corpo as RegraDeCobranca };
}
