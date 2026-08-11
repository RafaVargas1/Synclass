export type CadastroProfessorInput = {
  nome: string;
  contato: string;
};

export type CadastroProfessorResultado =
  { sucesso: true; nome: string } | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir o cadastro. Tente novamente.';
const MensagemErroConexao =
  'Não foi possível conectar ao servidor. Verifique sua conexão e tente novamente.';

/**
 * URL base da Api do Synclass. Configurável via variável de ambiente pública
 * do Expo (`EXPO_PUBLIC_*`); usa a porta de desenvolvimento local por padrão
 * (ver backend/README.md#debug).
 */
const ApiBaseUrl = process.env.EXPO_PUBLIC_API_URL ?? 'http://localhost:5005';

/**
 * Tempo máximo de espera pela resposta antes de tratar como erro de conexão.
 * Sem isso, uma conexão que abre mas nunca responde (proxy travado, app em
 * segundo plano no mobile) deixa a tela presa em "Enviando..." para sempre —
 * ver Cenário 6 da issue #1 ("sem travar ou mostrar tela em branco").
 */
const TimeoutRequisicaoMs = 15000;

/**
 * Envolve o `fetch` de POST /professores/cadastro atrás de uma interface
 * própria (ver docs/spec/code-style.md#dependências): nunca lança para erros
 * de negócio (contato inválido/duplicado) ou de rede — sempre devolve um
 * resultado tipado, para a tela exibir a mensagem sem travar.
 */
export async function cadastrarProfessor(
  input: CadastroProfessorInput,
): Promise<CadastroProfessorResultado> {
  let response: Response;
  try {
    response = await postCadastro(input);
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }

  return interpretarResposta(response);
}

function postCadastro(input: CadastroProfessorInput): Promise<Response> {
  const controle = new AbortController();
  const timeoutId = setTimeout(() => controle.abort(), TimeoutRequisicaoMs);

  return fetch(`${ApiBaseUrl}/professores/cadastro`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
    signal: controle.signal,
  }).finally(() => clearTimeout(timeoutId));
}

async function interpretarResposta(response: Response): Promise<CadastroProfessorResultado> {
  const corpo = await response.json().catch(() => null);

  if (!response.ok) {
    return { sucesso: false, mensagem: corpo?.mensagem ?? MensagemErroGenerica };
  }

  return { sucesso: true, nome: corpo?.nome ?? '' };
}
