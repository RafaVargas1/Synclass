import { fetchComTimeout, MensagemErroConexao } from './httpClient';

export type BuscarPerfilResultado =
  | { sucesso: true; usuarioId: string; nome: string; contato: string }
  | { sucesso: false; mensagem: string };

export type AtualizarNomeResultado =
  { sucesso: true; nome: string } | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir a operação. Tente novamente.';

/**
 * Envolve o `fetch` de GET /usuarios/me (issue #27) atrás de uma interface
 * própria (ver docs/spec/code-style.md#dependências): nunca lança, sempre
 * devolve um resultado tipado, para a tela de perfil exibir o nome atual
 * sem travar. `Authorization: Bearer` é anexado por `fetchComTimeout`.
 */
export async function buscarPerfil(): Promise<BuscarPerfilResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout('/usuarios/me');
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }

  return {
    sucesso: true,
    usuarioId: corpo?.usuarioId ?? '',
    nome: corpo?.nome ?? '',
    contato: corpo?.contato ?? '',
  };
}

/**
 * Envolve o `fetch` de PUT /usuarios/me/nome (issue #27) — mesma política de
 * nunca lançar de `buscarPerfil` acima.
 */
export async function atualizarNome(nome: string): Promise<AtualizarNomeResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout('/usuarios/me/nome', {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ nome }),
    });
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  const corpo = await response.json().catch(() => null);
  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }

  return { sucesso: true, nome: corpo?.nome ?? '' };
}
