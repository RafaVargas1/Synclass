# Task: corrigir regressões de layout do MenuNavegacao apontadas pelo dev-review (#129)

Card: https://github.com/RafaVargas1/Synclass/issues/129

Esta branch (`fix/ux-emergencial-menu-painel`) já tem o PR #125 aberto,
corrigindo #124 (menu sobrepondo o header, hamburger como texto, Painel com
botão "Meu perfil" gigante). `dev-review` encontrou 2 regressões
bloqueantes + 1 achado de atenção nesse mesmo PR — esta Task corrige os 3,
sem tocar no que já foi aprovado (cards de ação do Painel, ícone hamburger,
`ChipSelector.descricao`).

## Ordem de execução

- [x] Teste (RNTL, `MenuNavegacao.test.tsx`): navegar para `/perfil`
      (`usePathname` mockado retornando `/perfil`) e assert que o item "Meu
      perfil" recebe `accessibilityState={{ selected: true }}` — hoje
      `encontrarSecaoAtiva` só busca em `secoesDoPapel(...)`, que nunca
      inclui `SecaoMeuPerfil`, então o teste deve falhar antes da correção.
- [x] Implementação mínima: fazer a comparação de seção ativa considerar
      `SecaoMeuPerfil` junto das seções de `secoesDoPapel` (ex: comparar
      contra `todasSecoes`, já calculado em `MenuNavegacao`, em vez de só
      `secoes`).
- [x] Teste (RNTL, `MenuNavegacao.test.tsx`): com papel Professor e
      `usuarioId` definido (5 seções de `secoesProfessor` + "Meu perfil" =
      6 itens), em modo mobile aberto (`useIsTelaLarga` mockado `false`,
      `aberto=true` via clique no botão), assert que todos os 6 itens
      renderizam como filhos diretos do mesmo container que tem a classe
      de fundo/borda do dropdown (`bg-background`/`border`/`shadow-md`) —
      não apenas os 2 primeiros. RNTL não mede pixel real, então o teste
      verifica estrutura (todos os itens dentro do mesmo elemento com essas
      classes), não posição geométrica.
- [x] Investigação de causa raiz (docs/spec/engenharia-de-qualidade.md):
      **causa raiz encontrada por leitura de computed style via Playwright,
      não tentativa às cegas**: React Native Web dá a toda `View` um
      `z-index: 0` implícito quando `position: relative` (default do
      RN-Web), o que cria uma stacking context em CADA View da árvore. O
      dropdown do menu tinha `z-index: 10`, mas isso só vale DENTRO da
      stacking context do próprio `Topbar` (seus ancestrais também
      `position:relative`/`z-index:0`) — não escapava pra competir com o
      corpo do Painel, um branch IRMÃO do `Topbar` (mesma stacking context
      "0" do pai comum `SafeAreaView`), que pinta por cima por vir depois
      no DOM. Confirmado tirando bounding boxes + `getComputedStyle` reais
      via Playwright, não achado por tentativa. **Reproduza SEM backend
      real**: não
      registre Professor via API nem espere `GET /usuarios/me` resolver
      contra um servidor de verdade — isso não é necessário e uma
      tentativa anterior desta mesma Task gastou o teto de iterações
      tentando isso (ver `## Bloqueado` abaixo). Em vez disso, no script
      Playwright, intercepte a chamada com `page.route('**/usuarios/me',
      route => route.fulfill({ json: { usuarioId: 'prof-fake-129', nome:
      'Ana' } }))` **antes** de navegar para `/painel`, junto com o
      `localStorage` de sessão fake já usado nesta sessão
      (`synclass.sessao.token`/`synclass.sessao.papeis =
      '["Professor"]'`). Isso faz `usePerfilLogado`/`secoesProfessor`
      resolverem as 5 seções completas sem precisar de Postgres, `dotnet
      run`, nem fluxo de cadastro. Viewport 390×844, clique no botão de
      abrir o menu, screenshot.
- [x] Implementação mínima: corrigir o dropdown mobile pra cobrir todos os
      itens com fundo opaco, qualquer quantidade de seções. **Duas partes**:
      (1) `Topbar.tsx` ganha `className="z-20 ..."` na raiz — escapa a
      stacking context local e faz todo o subtree do cabeçalho (incluindo
      o dropdown) pintar acima do corpo da tela; (2) além do z-index,
      adicionado um fundo escurecido (`Pressable` com `position: 'fixed'`
      no web, `bg-black/40`, guardado por `Platform.OS === 'web'`) cobrindo
      o resto da tela enquanto o menu mobile está aberto — sem isso, o
      Painel (que desde #124 mostra as MESMAS ações como cards no corpo)
      ficava visível ao redor do dropdown e a coincidência de texto
      idêntico passava impressão de "menu sem fundo" mesmo com o dropdown
      corretamente desenhado. Toque fora do menu fecha (bônus de UX,
      convenção de overlay). Verificado visualmente via Playwright
      (390×844, cenário de 6 itens) — ver screenshots anexados ao PR.
- [x] Teste (RNTL, `MenuNavegacao.test.tsx`): em modo desktop (`telaLarga`
      mockado `true`), assert que a raiz de `MenuNavegacao` NÃO tem uma
      classe que force `w-full`/`flex-1` diretamente — a faixa de seções
      de largura total, se necessária, deve estar num elemento que não é
      irmão direto de `Logotipo` dentro do `flex-row` de `Topbar.tsx`. Se a
      estrutura mudar (ex: `menuNavegacao` virar uma segunda linha do
      `Topbar`, fora do `flex-row` do cabeçalho), atualize
      `Topbar.test.tsx`/`TopbarAutenticada.test.tsx` de acordo.
- [x] Implementação mínima: restructure para a marca permanecer no canto
      superior esquerdo do cabeçalho em qualquer viewport — verifique
      visualmente via Playwright (1440×900) que a marca não cola em "Sair"
      nem se desloca do canto esquerdo, com o menu tanto fechado quanto
      (se aplicável) com todas as seções visíveis. **Feito**: `Topbar.tsx`
      agora renderiza `menuNavegacao` como segunda linha de largura total
      abaixo do cabeçalho em viewport larga, fora do `flex-row` que contém
      a `Logotipo` — confirmado via Playwright que a marca fica no canto
      esquerdo em qualquer estado do menu.
- [x] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md` e `docs/spec/engenharia-de-qualidade.md`
      antes de marcar a Task concluída (autorrevisão, já no prompt de
      sistema do harness).
- [x] ~~Fix pontual: `scripts/dashboard-server.mjs` bind em `127.0.0.1`~~ —
      **removido desta Task**, virou uma Task paralela isolada
      (`docs/specs/129b-dashboard-bind-localhost/task.md`, outra worktree),
      já que é um arquivo sem relação nenhuma com `MenuNavegacao.tsx`/
      `Topbar.tsx` — não competir pelo mesmo teto de iterações desta Task.

## Fora de escopo desta Task

- Não mexer em `frontend/src/app/painel/index.tsx` (cards de ação),
  `IconeHamburguer`, ou `ChipSelector.descricao` — já aprovados no
  `dev-review` do PR #125, não repita trabalho nem re-teste o que já
  passou.
- Não resolver a issue #126 (mensagem "modelo Vago") nem o épico #127
  (Painel como dashboard de dados) — fora do escopo desta Task.


## Retomado após bloqueio (issue #130)

2026-08-21 — Primeira tentativa gastou o teto de 40 iterações tentando
reproduzir o cenário de 6 seções contra um backend real (`curl -X POST
.../professores/cadastro`, 500 Internal Server Error — ambiente da
worktree, não bug de produto), sem nunca chegar no bug de layout em si.
Causa raiz: o item de "Investigação de causa raiz" acima não deixava claro
que a reprodução deveria ser 100% frontend (mock/interceptação), não um
fluxo de cadastro real. Corrigido acima — retome a partir do item não
concluído seguinte (o item de "Meu perfil" ativo já está `[x]` e
commitado, não repita).
