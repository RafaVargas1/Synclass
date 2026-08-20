import { fetchComTimeout, MensagemErroConexao } from './httpClient';

export type GerarConviteInput = {
  professorId: string;
  contato: string;
  matriculaId?: string | null;
};

export type GerarConviteResultado =
  | { sucesso: true; conviteId: string; token: string; codigo: string; expiraEm: string }
  | { sucesso: false; mensagem: string };

export type AceitarConviteInput = {
  token: string;
  nome: string;
  contato: string;
};

export type AceitarConviteResultado =
  | { sucesso: true; usuarioId: string; nome: string; papeis: string[] }
  | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir a operação. Tente novamente.';

/**
 * Envolve o `fetch` de POST /professores/{professorId}/convites (issue #2)
 * atrás de uma interface própria (ver docs/spec/code-style.md#dependências):
 * nunca lança para erros de negócio (Aluno já vinculado, matrícula de
 * origem inválida) ou de rede — sempre devolve um resultado tipado.
 */
export async function gerarConvite(input: GerarConviteInput): Promise<GerarConviteResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(`/professores/${input.professorId}/convites`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ contato: input.contato, matriculaId: input.matriculaId ?? null }),
    });
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }
  return {
    sucesso: true,
    conviteId: corpo.conviteId,
    token: corpo.token,
    codigo: corpo.codigo,
    expiraEm: corpo.expiraEm,
  };
}

/**
 * Envolve o `fetch` de POST /convites/{token}/aceite (issue #2): nunca
 * lança para erros de negócio (convite expirado/inválido, contato
 * divergente) ou de rede — sempre devolve um resultado tipado.
 */
export async function aceitarConvite(input: AceitarConviteInput): Promise<AceitarConviteResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout(`/convites/${input.token}/aceite`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ nome: input.nome, contato: input.contato }),
    });
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }
  return {
    sucesso: true,
    usuarioId: corpo.usuarioId,
    nome: corpo.nome,
    papeis: corpo.papeis ?? [],
  };
}
