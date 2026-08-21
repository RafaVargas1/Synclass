# Implementação: corrigir regressões do MenuNavegacao (#129)

## Entidades/componentes afetados

- `frontend/src/components/organisms/MenuNavegacao.tsx` — `encontrarSecaoAtiva`
  (destaque de "Meu perfil"), estrutura do dropdown mobile (fundo cobrindo
  todos os itens), raiz do componente em modo desktop (não pode competir
  com `Logotipo` por largura).
- `frontend/src/components/organisms/Topbar.tsx` — se a correção da marca
  exigir tirar `menuNavegacao` do `flex-row` do cabeçalho (provável: virar
  uma segunda linha abaixo do cabeçalho em vez de inline), este arquivo
  muda também. `TopbarAutenticada.tsx` só repassa a prop, não deve
  precisar mudar.
- `scripts/dashboard-server.mjs` — bind de interface (`listen(porta,
  '127.0.0.1', ...)` em vez de todas as interfaces).

Nenhuma mudança de contrato de API, nenhuma migration, nenhum componente
novo — é correção de layout/estrutura de componentes já existentes.

## Contrato de API

N/A — sem mudança de backend/contrato nesta Task.

## Modelo de dados

N/A.

## Edge points (não cobertos por um critério Gherkin)

- **Papel único (sem `AlternadorDePapel`)**: o dropdown mobile também
  precisa cobrir corretamente quando só há 1 papel (sem o alternador) — o
  cenário de teste de 6 itens do task.md é o pior caso (mais itens), mas
  confirme visualmente que o caso de 1 papel + poucas seções (cenário já
  testado no PR original) continua correto após a correção.
- **Dark mode**: as classes de fundo (`bg-background dark:bg-dark-background`)
  já existem no dropdown — a correção não deve remover o suporte a tema
  escuro: se restructurar o container, mantenha as classes `dark:` em
  paridade com o modo claro.
- **`TopbarAutenticada.test.tsx`/`Topbar.test.tsx`**: se a estrutura de
  `Topbar` mudar (menu virar segunda linha), releia esses dois arquivos de
  teste antes de editar — eles já cobrem o slot `menuNavegacao` e podem
  ter asserções sobre a estrutura atual que precisam de ajuste, não só
  `MenuNavegacao.test.tsx`.

## Dependência de outras Tasks

Nenhuma — Task isolada sobre uma branch já existente (`fix/ux-emergencial-menu-painel`,
PR #125), sem depender de outro Epic/Task em andamento.
