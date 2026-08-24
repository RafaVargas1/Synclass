import { fetchComTimeout, MensagemErroConexao } from './httpClient';

export type GerarCodigoEntradaTurmaResultado =
  | { sucesso: true; codigo: string; expiraEm: string }
  | { sucesso: false; mensagem: string };

export type AceitarCodigoEntradaTurmaResultado =
  | { sucesso: true; professorId: string; professorNome: string }
  | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir a operação. Tente novamente.';

/**
 * Envolve o `fetch` de POST /professores/{professorId}/codigos-entrada:
 * gera um código curto, válido por poucos minutos, que qualquer Aluno
 * autenticado pode usar pra entrar na turma — diferente de `gerarConvite`
 * (`./convites.ts`), não é vinculado a um contato nem de uso único. Nunca
 * lança para erros de rede — sempre devolve um resultado tipado.
 */
export async function gerarCodigoEntradaTurma(professorId: string): Promise<GerarCodigoEntradaTurmaResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(`/professores/${professorId}/codigos-entrada`, {
      method: 'POST',
    });
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }
  return { sucesso: true, codigo: corpo.codigo, expiraEm: corpo.expiraEm };
}

/**
 * Envolve o `fetch` de POST /alunos/codigos-entrada/{codigo}/aceite — o
 * Aluno autenticado (token resolve a identidade, sem precisar de nome/
 * contato no corpo) entra na turma do Professor dono do código. Nunca
 * lança para erros de negócio (código inválido/expirado) ou de rede.
 */
export async function aceitarCodigoEntradaTurma(codigo: string): Promise<AceitarCodigoEntradaTurmaResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(`/alunos/codigos-entrada/${codigo}/aceite`, {
      method: 'POST',
    });
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }
  return { sucesso: true, professorId: corpo.professorId, professorNome: corpo.professorNome };
}
