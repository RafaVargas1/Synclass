import { fetchComTimeout, MensagemErroConexao } from './httpClient';

export type CadastroUsuarioInput = {
  nome: string;
  contato: string;
};

export type CadastroUsuarioResultado =
  { sucesso: true; nome: string } | { sucesso: false; mensagem: string };

export type VerificarContatoResultado = { identidadeExistente: boolean; nome: string | null };

const MensagemErroGenerica = 'Não foi possível concluir o cadastro. Tente novamente.';
const IdentidadeInexistente: VerificarContatoResultado = { identidadeExistente: false, nome: null };

/**
 * Fábrica dos dois `fetch` (POST /cadastro, GET /verificar-contato) atrás de
 * uma interface própria (ver docs/spec/code-style.md#dependências) —
 * Professor (issue #1) e Aluno (issue #61) usam o mesmo contrato de Api, só
 * a rota base muda. Nenhuma das duas funções lança para erro de negócio
 * (contato inválido/duplicado) ou de rede, sempre devolvem um resultado
 * tipado, para a tela exibir a mensagem sem travar.
 */
export function criarClienteCadastroUsuario(rotaBase: string) {
  async function cadastrar(input: CadastroUsuarioInput): Promise<CadastroUsuarioResultado> {
    let response: Response;
    try {
      response = await fetchComTimeout(`${rotaBase}/cadastro`, {
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
    return { sucesso: true, nome: corpo?.nome ?? '' };
  }

  /**
   * Fail-open — erro de rede ou resposta inesperada nunca trava o cadastro,
   * só deixa o campo Nome editável (mesmo comportamento desde a issue #27).
   */
  async function verificarContato(contato: string): Promise<VerificarContatoResultado> {
    let response: Response;
    try {
      response = await fetchComTimeout(`${rotaBase}/verificar-contato?contato=${encodeURIComponent(contato)}`);
    } catch {
      return IdentidadeInexistente;
    }

    if (!response.ok) {
      return IdentidadeInexistente;
    }
    const corpo = await response.json().catch(() => null);
    return corpo?.identidadeExistente
      ? { identidadeExistente: true, nome: corpo.nome ?? null }
      : IdentidadeInexistente;
  }

  return { cadastrar, verificarContato };
}
