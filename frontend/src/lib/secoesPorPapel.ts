import type { Href } from 'expo-router';

/**
 * Nomes de ícone do conjunto `Ionicons` (`@expo/vector-icons`) usados no
 * menu — união fechada em vez de `string` solto, pra pegar erro de
 * digitação de nome de ícone em tempo de compilação. Mantém este arquivo
 * (puro, sem JSX) desacoplado do componente que efetivamente desenha o
 * ícone (`MenuNavegacao`/`IconeSecao`).
 */
export type NomeIconeSecao =
  | 'home-outline'
  | 'person-outline'
  | 'person-add-outline'
  | 'clipboard-outline'
  | 'time-outline'
  | 'people-outline'
  | 'link-outline'
  | 'cash-outline'
  | 'enter-outline'
  | 'calendar-outline'
  | 'bar-chart-outline';

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
    { label: 'Minhas aulas', href: '/aluno/minhas-aulas', icone: 'calendar-outline' },
    { label: 'Entrar em nova turma', href: '/aluno/entrar-em-turma', icone: 'enter-outline' },
    {
      label: 'Ver histórico de frequência',
      href: '/aluno/historico-frequencia',
      icone: 'bar-chart-outline',
    },
    { label: 'Ver valor devido', href: '/aluno/valor-devido', icone: 'cash-outline' },
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
    { label: 'Fazer chamada', href: `/professor/${usuarioId}/chamada` as Href, icone: 'clipboard-outline' },
    { label: 'Gerenciar horários', href: `/professor/${usuarioId}/horarios` as Href, icone: 'time-outline' },
    {
      label: 'Alocar Aluno em horário',
      href: `/professor/${usuarioId}/alocacoes` as Href,
      icone: 'link-outline',
    },
    { label: 'Meus Alunos', href: `/professor/${usuarioId}/alunos` as Href, icone: 'people-outline' },
    {
      label: 'Adicionar Aluno',
      href: `/professor/${usuarioId}/alunos/adicionar` as Href,
      icone: 'person-add-outline',
    },
    { label: 'Ver valor devido', href: `/professor/${usuarioId}/valor-devido` as Href, icone: 'cash-outline' },
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
