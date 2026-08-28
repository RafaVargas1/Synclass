import { fetchComTimeout, MensagemErroConexao } from './httpClient';

/**
 * Resultado de `conectarMercadoPago` (issue #203): sucesso carrega a URL de
 * autorização do Mercado Pago para o Professor abrir no navegador; falha
 * carrega uma mensagem amigável. Mesmo formato de resultado tipado dos
 * demais módulos de `lib/api/*`.
 */
export type ConectarMercadoPagoResultado =
  { sucesso: true; url: string } | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir a operação. Tente novamente.';

/**
 * Envolve o `fetch` de GET /professores/mercado-pago/conectar (issue #203)
 * atrás de uma interface própria (ver docs/spec/code-style.md#dependências).
 * O endpoint é autenticado: o `professorId` do token da sessão, anexado por
 * `fetchComTimeout`, identifica o Professor — o id em rota não é usado pelo
 * backend (a conexão pertence ao usuário autenticado, e o `state` do fluxo
 * liga o callback a ele). Em erro de rede, usa a mensagem de conexão padrão.
 */
export async function conectarMercadoPago(professorId: string): Promise<ConectarMercadoPagoResultado> {
  try {
    const resposta = await fetchComTimeout(`/professores/mercado-pago/conectar`, { method: 'GET' });
    if (!resposta.ok) {
      return { sucesso: false, mensagem: MensagemErroGenerica };
    }
    const dados = (await resposta.json()) as { url: string };
    return { sucesso: true, url: dados.url };
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }
}
