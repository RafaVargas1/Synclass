import { fetchComTimeout, MensagemErroConexao } from './httpClient';

export type VinculoProfessor = { professorId: string; nome: string };

export type ListarVinculosAlunoResultado =
  | { sucesso: true; vinculos: VinculoProfessor[] }
  | { sucesso: false; mensagem: string };

const MensagemErroGenerica = 'Não foi possível concluir a operação. Tente novamente.';

export async function listarVinculosAluno(): Promise<ListarVinculosAlunoResultado> {
  let response: Response;
  try {
    response = await fetchComTimeout('/alunos/professores');
  } catch {
    return { sucesso: false, mensagem: MensagemErroConexao };
  }
  if (!response.ok) {
    return { sucesso: false, mensagem: MensagemErroGenerica };
  }
  const corpo = await response.json().catch(() => null);
  return { sucesso: true, vinculos: (corpo as VinculoProfessor[] | null) ?? [] };
}
