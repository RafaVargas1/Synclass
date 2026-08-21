# Task: Menu de navegação persistente e responsivo (#77)

Card: https://github.com/RafaVargas1/Synclass/issues/77

## Ordem de execução

- [x] Teste de componente: refatoração de `acoesProfessor`/`acoesAluno` do Painel — extrai as duas listas para `src/lib/secoesPorPapel.ts` sem mudar comportamento (testes existentes de `painel/index.test.tsx` continuam passando)
- [x] Implementação: extrai `acoesProfessor`/`acoesAluno` para `src/lib/secoesPorPapel.ts` (renomeando para `secoesProfessor`/`secoesAluno`) e atualiza `painel/index.tsx` para importar de lá
- [x] Teste unidade: hook `useIsTelaLarga` — retorna `true` acima do breakpoint (1024px), `false` abaixo (mock de `useWindowDimensions`)
- [x] Implementação: hook `useIsTelaLarga` (`src/lib/useIsTelaLarga.ts`) com `useWindowDimensions` e breakpoint fixo de 1024px
- [ ] Teste de componente: `MenuNavegacao` — indica a seção atual a partir da rota ativa (mock de `usePathname`), fazendo match por segmento dinâmico (ex: rota `/professor/abc-123/horarios` casa com a seção `/professor/[professorId]/horarios`)
- [ ] Implementação mínima: `MenuNavegacao.tsx` (organism) — recebe `papeis`, `papelAtivo`, `onSelecionarPapel` (mesmas props de `AlternadorDePapel`), deriva a lista de seções via `secoesPorPapel` e a seção ativa via `usePathname`
- [ ] Teste de componente: `MenuNavegacao` — navegação direta entre duas seções via `Link`, sem passar por `/painel`
- [ ] Implementação: `MenuNavegacao` — renderiza a lista de seções como links, com a seção ativa destacada
- [ ] Teste de componente: `MenuNavegacao` — em viewport larga (mock de `useIsTelaLarga` retornando `true`), o menu renderiza sempre visível (sem exigir toque para abrir)
- [ ] Teste de componente: `MenuNavegacao` — em viewport estreita (mock retornando `false`), o menu começa fechado e só aparece após acionar o botão de abrir
- [ ] Implementação: `MenuNavegacao` — dois modos de exibição conforme `useIsTelaLarga` (persistente vs. acionado por botão)
- [ ] Teste de componente: `MenuNavegacao` — com dois papéis (`papeis=['Professor','Aluno']`), mostra as seções do `papelAtivo` corrente e inclui `AlternadorDePapel`; ao trocar de papel, a lista de seções exibida muda
- [ ] Implementação: `MenuNavegacao` — inclui `AlternadorDePapel` quando `papeis.length > 1`
- [ ] Teste de componente: `Topbar` — aceita a nova prop opcional para acionar/exibir o `MenuNavegacao` (ex: botão de menu no mobile) sem quebrar o uso atual de `titulo`/`children`
- [ ] Implementação: integra `MenuNavegacao` no fluxo autenticado (`app/painel/index.tsx` e demais telas via `Topbar`/wrapper), usando `useSessao()` para `papeis`/`papelAtivo`/`definirPapelAtivo`
- [ ] Docs: atualizar `docs/spec/design-system.md` se o `MenuNavegacao` introduzir um padrão visual novo (ex: painel lateral) que ainda não está documentado na seção de layout

### Inconsistências encontradas

1. **`MenuNavegacao` não recebe `usuarioId`, mas as seções de Professor com segmento dinâmico dependem dele.**

   O item 6 diz que `MenuNavegacao` recebe só `papeis`, `papelAtivo` e `onSelecionarPapel` ("mesmas props de `AlternadorDePapel`"), derivando a lista via `secoesPorPapel`; o item 11 confirmaria isso ao integrar só com `papeis`/`papelAtivo`/`definirPapelAtivo` do `useSessao()`. Porém `secoesProfessor(usuarioId)` exige `usuarioId` para montar os hrefs das seções dinâmicas (`/professor/{usuarioId}/horarios`, `/alocacoes`, `/convites/novo`, `/valor-devido`). Sem essa prop, o menu de Professor renderiza só "Cadastrar Aluno" — nunca "Gerenciar horários" nem as demais seções, e o teste do item 5 não pode ser satisfeito.

2. **"Match por segmento dinâmico" (item 5) exige hrefs-com-templates, contradizendo `secoesProfessor` (items 1-2) que produz hrefs concretos.**

   O exemplo do item 5 (rota `/professor/abc-123/horarios` casa com `[professorId]`) indica que a seção carrega o segmento literal `[professorId]`. Mas `secoesProfessor` já retorna hrefs concretos com o id real substituído (e `secoesPorPapel.test.ts`, aprovado nos items 1-2, valida exatamente isso). Ou seja: para o match do item 5 funcionar, ou `secoesProfessor` passa a emitir o template `[professorId]` (quebrando o teste existente e exigindo outra forma de montar o link navegável), ou o `MenuNavegacao` precisa do `usuarioId` e o "match por segmento" vira igualdade exata de rota contra o href concreto.

   **Como resolver (duas direções mutuamente exclusivas):**
   - **(A)** adicionar prop `usuarioId` ao `MenuNavegacao` (e passá-la na integração do item 11), mantendo `secoesProfessor` como está; o "match" passa a ser por igualdade da rota com o href já resolvido — e a menção a `[professorId]` no item 5 seria só ilustrativa da natureza dinâmica do segmento, não do formato literal da seção.
   - **(B)** fazer `secoesProfessor`/`secoesPorPapel` emitirem hrefs com o literal `[professorId]` (exigindo atualizar `secoesPorPapel.test.ts` dos items 1-2) e o `MenuNavegacao` casar rota×template segmento a segmento; o link navegável precisaria de resolução extra do id (via `useLocalSearchParams`/`useSessao`), o que reintroduz a necessidade de contexto/parâmetro não previsto nas props do item 6.

   Bloqueia o item 5 (e consequentemente toda a implementação do `MenuNavegacao`). Sem decisão do mantenedor entre (A) e (B), não há como seguir sem adivinhar.
