import type { Href } from 'expo-router';

export type Secao = { label: string; href: Href };

/**
 * Seções do Aluno (issue #44, #77 e #144): telas do Aluno sem segmento
 * dinâmico na rota — `/aluno/entrar-em-turma`, `/aluno/historico-frequencia`
 * e `/aluno/valor-devido`. "Entrar em nova turma" (issue #144) fica
 * primeiro: é ação de aquisição de vínculo novo, mais próxima do fluxo de
 * "primeira vez" do que as duas telas de consulta (ver
 * docs/spec/ux-heuristics.md#agrupamento-visual). As telas de Professor da
 * matrícula do Aluno (`/aluno/professores/[professorId]/...`) continuam fora
 * daqui porque `professorId`, nesse caso, identifica o Professor da
 * matrícula, não o próprio Aluno logado, e a sessão (`useSessao`) não
 * carrega esse vínculo hoje — ver nota de limitação conhecida no PR.
 */
export function secoesAluno(): Secao[] {
  return [
    { label: 'Entrar em nova turma', href: '/aluno/entrar-em-turma' },
    { label: 'Ver histórico de frequência', href: '/aluno/historico-frequencia' },
    { label: 'Ver valor devido', href: '/aluno/valor-devido' },
  ];
}

/**
 * Seções do Professor (issue #44 e #77). `Cadastrar Aluno` não depende de
 * `usuarioId` (a Api deriva o Professor autenticado do token, issue #23).
 * As demais usam `/professor/{professorId}/...` — `professorId` é o mesmo
 * `Usuario.Id` do Professor logado (não existe uma entidade `Professor`
 * separada, ver `ProfessoresController.Cadastrar`), por isso só aparecem
 * depois que `usuarioId` resolve via `GET /usuarios/me`.
 */
export function secoesProfessor(usuarioId: string | undefined): Secao[] {
  const secoes: Secao[] = [{ label: 'Cadastrar Aluno', href: '/professor/alunos/cadastro' }];

  if (usuarioId) {
    secoes.push(
      { label: 'Gerenciar horários', href: `/professor/${usuarioId}/horarios` as Href },
      { label: 'Meus Alunos', href: `/professor/${usuarioId}/alunos` as Href },
      { label: 'Alocar Aluno em horário', href: `/professor/${usuarioId}/alocacoes` as Href },
      { label: 'Convidar Aluno', href: `/professor/${usuarioId}/convites/novo` as Href },
      { label: 'Ver valor devido', href: `/professor/${usuarioId}/valor-devido` as Href },
    );
  }

  return secoes;
}

/**
 * Seções do papel ativo corrente, para o `MenuNavegacao` (issue #77). Sem
 * papel reconhecido, retorna lista vazia — não há seções a exibir.
 */
export function secoesDoPapel(
  papelAtivo: string | undefined,
  usuarioId: string | undefined,
): Secao[] {
  if (papelAtivo === 'Professor') {
    return secoesProfessor(usuarioId);
  }
  if (papelAtivo === 'Aluno') {
    return secoesAluno();
  }
  return [];
}
