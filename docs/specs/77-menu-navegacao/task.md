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

_(Nenhuma até o momento — preencher durante a implementação se houver.)_
