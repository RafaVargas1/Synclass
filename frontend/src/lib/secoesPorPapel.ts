import type { Href } from 'expo-router';

/**
 * Nomes de ícone do conjunto Phosphor (`phosphor-react-native`) usados no
 * menu — união fechada em vez de `string` solto, pra pegar erro de
 * digitação de nome de ícone em tempo de compilação. Mantém este arquivo
 * (puro, sem JSX) desacoplado do componente que efetivamente desenha o
 * ícone (`MenuNavegacao`/`IconeSecao`).
 */
export type NomeIconeSecao =
  | 'home'
  | 'perfil'
  | 'adicionar-aluno'
  | 'chamada'
  | 'horarios'
  | 'alunos'
  | 'alocacoes'
  | 'valor-devido'
  | 'entrar-turma'
  | 'minhas-aulas'
  | 'historico';

export type Secao = { label: string; href: Href; icone: NomeIconeSecao };

/**
 * Seções do Aluno (issue #44, #77, #144 e #182). Ordem por frequência de
 * uso, não por ordem de "primeira vez" (achado de usabilidade do usuário,
 * revisão do menu): "Minhas aulas" é a ação do dia a dia (marcar, cancelar,
 * confirmar presença) — muito mais frequente que "Entrar em nova turma",
 * que só acontece uma vez por Professor novo. Ver
 * docs/spec/ux-heuristics.md (posicionar ações frequentes primeiro reduz o
 * custo de navegação médio, mesmo racional de frequência de uso do menu do
 * Professor logo abaixo). "Ver histórico" e "Ver valor devido" são
 * consultas periódicas, não o motivo mais comum de abrir o app — ficam por
 * último.
 */
export function secoesAluno(): Secao[] {
  return [
    { label: 'Minhas aulas', href: '/aluno/minhas-aulas', icone: 'minhas-aulas' },
    { label: 'Entrar em nova turma', href: '/aluno/entrar-em-turma', icone: 'entrar-turma' },
    {
      label: 'Ver histórico de frequência',
      href: '/aluno/historico-frequencia',
      icone: 'historico',
    },
    { label: 'Ver valor devido', href: '/aluno/valor-devido', icone: 'valor-devido' },
  ];
}

/**
 * Seções do Professor (issue #44 e #77). Ordem por frequência de uso
 * (achado de usabilidade do usuário, revisão do menu): "Fazer chamada" é a
 * ação operacional do dia a dia de dar aula, então abre a lista; "Gerenciar
 * horários" e "Alocar Aluno em horário" ficam juntos por serem sobre o
 * mesmo objeto (agenda semanal) e serem consultados/ajustados com
 * frequência parecida; "Meus Alunos" é consulta de rotina; "Adicionar
 * Aluno" e "Ver valor devido" são as menos frequentes (onboarding pontual e
 * checagem financeira periódica, não diária) — foram primeiro e último,
 * respectivamente, antes desta revisão, e viram os últimos da lista agora.
 * `Adicionar Aluno` substitui as antigas "Cadastrar Aluno" + "Convidar
 * Aluno" — porta de entrada única com os três jeitos de trazer um Aluno
 * (sem contato, por convite, por código de entrada de turma). Todas as
 * seções usam `/professor/{professorId}/...` — `professorId` é o mesmo
 * `Usuario.Id` do Professor logado (não existe uma entidade `Professor`
 * separada, ver `ProfessoresController.Cadastrar`), por isso só aparecem
 * depois que `usuarioId` resolve via `GET /usuarios/me`.
 */
export function secoesProfessor(usuarioId: string | undefined): Secao[] {
  if (!usuarioId) {
    return [];
  }

  return [
    { label: 'Fazer chamada', href: `/professor/${usuarioId}/chamada` as Href, icone: 'chamada' },
    { label: 'Gerenciar horários', href: `/professor/${usuarioId}/horarios` as Href, icone: 'horarios' },
    {
      label: 'Alocar Aluno em horário',
      href: `/professor/${usuarioId}/alocacoes` as Href,
      icone: 'alocacoes',
    },
    { label: 'Meus Alunos', href: `/professor/${usuarioId}/alunos` as Href, icone: 'alunos' },
    {
      label: 'Adicionar Aluno',
      href: `/professor/${usuarioId}/alunos/adicionar` as Href,
      icone: 'adicionar-aluno',
    },
    { label: 'Ver valor devido', href: `/professor/${usuarioId}/valor-devido` as Href, icone: 'valor-devido' },
  ];
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
