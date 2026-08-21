# Task: corrigir regressões de layout do MenuNavegacao apontadas pelo dev-review (#129)

Card: https://github.com/RafaVargas1/Synclass/issues/129

Esta branch (`fix/ux-emergencial-menu-painel`) já tem o PR #125 aberto,
corrigindo #124 (menu sobrepondo o header, hamburger como texto, Painel com
botão "Meu perfil" gigante). `dev-review` encontrou 2 regressões
bloqueantes + 1 achado de atenção nesse mesmo PR — esta Task corrige os 3,
sem tocar no que já foi aprovado (cards de ação do Painel, ícone hamburger,
`ChipSelector.descricao`).

## Ordem de execução

- [ ] Teste (RNTL, `MenuNavegacao.test.tsx`): navegar para `/perfil`
      (`usePathname` mockado retornando `/perfil`) e assert que o item "Meu
      perfil" recebe `accessibilityState={{ selected: true }}` — hoje
      `encontrarSecaoAtiva` só busca em `secoesDoPapel(...)`, que nunca
      inclui `SecaoMeuPerfil`, então o teste deve falhar antes da correção.
- [ ] Implementação mínima: fazer a comparação de seção ativa considerar
      `SecaoMeuPerfil` junto das seções de `secoesDoPapel` (ex: comparar
      contra `todasSecoes`, já calculado em `MenuNavegacao`, em vez de só
      `secoes`).
- [ ] Teste (RNTL, `MenuNavegacao.test.tsx`): com papel Professor e
      `usuarioId` definido (5 seções de `secoesProfessor` + "Meu perfil" =
      6 itens), em modo mobile aberto (`useIsTelaLarga` mockado `false`,
      `aberto=true` via clique no botão), assert que todos os 6 itens
      renderizam como filhos diretos do mesmo container que tem a classe
      de fundo/borda do dropdown (`bg-background`/`border`/`shadow-md`) —
      não apenas os 2 primeiros. RNTL não mede pixel real, então o teste
      verifica estrutura (todos os itens dentro do mesmo elemento com essas
      classes), não posição geométrica.
- [ ] Investigação de causa raiz (docs/spec/engenharia-de-qualidade.md):
      antes de mudar código, explique em comentário de commit por que o
      fundo da caixa não cobre todos os itens hoje — é dimensionamento da
      `View` absoluta que não expande pra conteúdo dinâmico, `overflow`
      cortando o conteúdo, ordem de stacking (`zIndex`) entre o dropdown e
      o corpo da tela, ou outra causa. Reproduza localmente (Playwright
      contra `npx expo start --web`, viewport 390×844, sessão fake com
      `papeis: ["Professor"]`, aguardando `GET /usuarios/me` resolver com
      `usuarioId` definido) antes de aplicar a correção.
- [ ] Implementação mínima: corrigir o dropdown mobile pra cobrir todos os
      itens com fundo opaco, qualquer quantidade de seções. Verifique
      visualmente com o mesmo cenário de 6 itens via Playwright (Passo 3.5
      de dev-review) — anexe screenshot ao PR/commit.
- [ ] Teste (RNTL, `MenuNavegacao.test.tsx`): em modo desktop (`telaLarga`
      mockado `true`), assert que a raiz de `MenuNavegacao` NÃO tem uma
      classe que force `w-full`/`flex-1` diretamente — a faixa de seções
      de largura total, se necessária, deve estar num elemento que não é
      irmão direto de `Logotipo` dentro do `flex-row` de `Topbar.tsx`. Se a
      estrutura mudar (ex: `menuNavegacao` virar uma segunda linha do
      `Topbar`, fora do `flex-row` do cabeçalho), atualize
      `Topbar.test.tsx`/`TopbarAutenticada.test.tsx` de acordo.
- [ ] Implementação mínima: restructure para a marca permanecer no canto
      superior esquerdo do cabeçalho em qualquer viewport — verifique
      visualmente via Playwright (1440×900) que a marca não cola em "Sair"
      nem se desloca do canto esquerdo, com o menu tanto fechado quanto
      (se aplicável) com todas as seções visíveis.
- [ ] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md` e `docs/spec/engenharia-de-qualidade.md`
      antes de marcar a Task concluída (autorrevisão, já no prompt de
      sistema do harness).
- [ ] Fix pontual (Atenção do dev-review, não bloqueante mas incluído
      nesta Task por ser trivial): `scripts/dashboard-server.mjs` — o
      servidor do dashboard local escuta em todas as interfaces de rede;
      mude para escutar só em `127.0.0.1` (ferramenta interna do harness,
      não precisa estar acessível pela rede). Não adicione autenticação
      nem outra mudança de escopo — só o bind de interface.

## Fora de escopo desta Task

- Não mexer em `frontend/src/app/painel/index.tsx` (cards de ação),
  `IconeHamburguer`, ou `ChipSelector.descricao` — já aprovados no
  `dev-review` do PR #125, não repita trabalho nem re-teste o que já
  passou.
- Não resolver a issue #126 (mensagem "modelo Vago") nem o épico #127
  (Painel como dashboard de dados) — fora do escopo desta Task.
