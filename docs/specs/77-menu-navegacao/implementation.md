# Implementação: Menu de navegação persistente e responsivo (#77)

## Entidades/classes afetadas

- **Novo componente (frontend, `organisms/MenuNavegacao.tsx`)**: navegação
  lateral/persistente (desktop) ou compacta acionada por botão (mobile).
- **Novo hook (frontend, `lib/useIsTelaLarga.ts`)**: decide entre desktop e
  mobile a partir da largura da viewport.
- **Novo módulo (frontend, `lib/secoesPorPapel.ts`)**: centraliza a lista de
  seções por papel (Professor/Aluno), hoje duplicada implicitamente em
  `acoesProfessor`/`acoesAluno` de `painel/index.tsx`.
- **Modificado**: `app/painel/index.tsx` — importa `secoesProfessor`/
  `secoesAluno` de `lib/secoesPorPapel.ts` em vez de defini-las localmente
  (sem mudar o comportamento já coberto por `painel/index.test.tsx`).
- **Modificado**: `components/organisms/Topbar.tsx` — ganha um jeito de
  acionar/exibir o `MenuNavegacao` sem quebrar a API atual (`titulo`,
  `children`).

## Contrato de API

Não há — mudança puramente client-side, sem endpoint novo.

## Modelo de dados

Nenhuma mudança — sem tabela/coluna nova. Só dado de configuração (seções
por papel, já existente como código) e estado de UI local (menu aberto/
fechado em mobile).

## Decisões de design

### Como decide seções por papel

`lib/secoesPorPapel.ts` exporta `secoesProfessor(usuarioId)` e
`secoesAluno()`, extraídas 1:1 de `acoesProfessor`/`acoesAluno` (mesmo
formato `{ label, href }` já usado em `painel/index.tsx`, sem renomear os
campos para não obrigar um segundo diff no Painel). Mesmo espírito da
extração de `lib/opcoesTipoMarcacao.ts` feita na Task #71 — evita duplicar
a lista de seções entre o Painel e o menu novo.

### Como decide a seção atual ("onde o usuário está")

`usePathname()` do expo-router dá a rota corrente (ex:
`/professor/abc-123/horarios`). O `MenuNavegacao` compara essa rota com os
`href` de `secoesPorPapel`, fazendo match por segmento fixo (ignorando o
segmento dinâmico, ex: o `professorId` do meio da rota) — comparação
literal falharia porque o `href` das seções de Professor é montado com o
`usuarioId` real, não o literal `[professorId]` da definição de rota do
Expo Router.

### Como decide mobile vs. desktop

`lib/useIsTelaLarga.ts` usa `useWindowDimensions` do React Native e retorna
`true` a partir de 1024px (breakpoint único, mesmo valor a ser reaproveitado
por qualquer decisão futura de responsividade — o card #77 pede pra alinhar
esse critério com a Task #69, que já mergeou sem introduzir breakpoint
algum: `MaxContentWidthPainel` só limita a largura máxima do conteúdo, não
alterna layout por viewport. #77 é a primeira Task a introduzir esse hook;
`useIsTelaLarga` fica em `lib/` justamente para ser reaproveitado por telas
futuras sem duplicar o valor 1024).

### Papel ativo

`MenuNavegacao` recebe `papeis`, `papelAtivo`, `onSelecionarPapel` (mesma
assinatura de `AlternadorDePapel`) e inclui esse componente internamente
quando `papeis.length > 1` — não duplica a lógica de troca de papel, só
reaproveita o organism já existente.

## Onde entra no layout

- **Desktop** (`useIsTelaLarga() === true`): `MenuNavegacao` fica
  persistente, como painel lateral fixo, sem exigir toque para aparecer.
- **Mobile** (`useIsTelaLarga() === false`): `MenuNavegacao` começa
  fechado; um botão no `Topbar` abre/fecha o menu por cima do conteúdo
  (overlay), sem empurrar/reduzir o espaço vertical da tela.
- Aplicado nas telas autenticadas alcançáveis a partir do Painel — não
  aparece em `login`/`professor/cadastro`/`aluno/index` (fluxos anônimos ou
  de entrada, sem seção "atual" fazer sentido).

## Edge points

- **Rotas com segmento dinâmico**: o match de seção ativa precisa ignorar o
  valor do segmento (`professorId`, `horarioId` etc.), não comparar string
  exata — ver "Como decide a seção atual" acima.
- **Rotas fora do Painel hoje**: `/aluno/professores/[professorId]/horarios`
  e `.../minhas-aulas` ficam de fora de `secoesAluno()`, mesma limitação já
  documentada em `painel/index.tsx` (a sessão não carrega o vínculo com o
  `professorId` da matrícula) — não resolver aqui, fora de escopo do card.
- **`/aluno/index`** (tela de entrada/cadastro do Aluno) não é uma seção de
  navegação pós-login — não entra em `secoesAluno()`. Quando a issue #64
  (tela unificada de entrada do Aluno) existir com uma rota estável pós-
  login, ela entra em `secoesAluno()` — `secoesPorPapel.ts` já centraliza
  isso para facilitar a adição futura.
- **Voltar continua funcionando igual** (RN explícita do card): o
  `MenuNavegacao` não substitui `TituloComVoltar`/pilha de navegação
  existente, é um caminho adicional.

## Dependência de outras Tasks

- **Issue #69** (já mergeada, PR #102): nenhuma dependência de fato — não
  introduziu breakpoint nenhum, só `MaxContentWidthPainel` (limite de
  largura). `MenuNavegacao` pode reaproveitar esse token para não esticar
  o painel lateral além do necessário, mas a decisão de breakpoint
  responsivo (1024px) é nova, definida nesta Task.
- **Issue #64** (relacionada, não bloqueante): quando a tela unificada de
  entrada do Aluno existir, sua rota entra em `secoesAluno()`.
