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

1. **`MenuNavegacao` não recebe `usuarioId`, mas as seções de Professor com segmento dinâmico dependem dele (não resolvido pela `implementation.md`).**

   O item 6 e a `docs/specs/77-menu-navegacao/implementation.md` ("Papel ativo") dizem que `MenuNavegacao` recebe só `papeis`, `papelAtivo` e `onSelecionarPapel` ("mesma assinatura de `AlternadorDePapel`"); o item 11 reforça isso ao integrar só com `papeis`/`papelAtivo`/`definirPapelAtivo` do `useSessao()`. Porém `secoesProfessor(usuarioId)` exige `usuarioId` para montar os hrefs das seções dinâmicas (`/professor/{usuarioId}/horarios`, `/alocacoes`, `/convites/novo`, `/valor-devido`), e a própria `implementation.md` ("Como decide a seção atual") afirma que o match usa esses hrefs já "montado[s] com o `usuarioId` real". Sem `usuarioId`, o menu de Professor renderiza só "Cadastrar Aluno" — nunca "Gerenciar horários" nem as demais seções, e o teste do item 5 é insatisfazível.

   A `implementation.md` **resolve** a direção (B) registrada antes (hrefs-com-template `[professorId]`): ela é explícita que o `href` das seções de Professor usa o `usuarioId` real, não o literal `[professorId]` — portanto as seções dinâmicas continuam sendo hrefs concretos. E ela **resolve** a semântica do match: por segmento fixo, ignorando o segmento dinâmico (rótulo/papel e cauda comparados; o id do meio ignorado). O que ela **não resolve** é a origem do `usuarioId` dentro do `MenuNavegacao`: mesmo com props "mesma assinatura de `AlternadorDePapel`", o menu precisa desse valor para construir (e consequentemente casar) as seções de Professor.

   **Direção restante (precisa de decisão do mantenedor):** como o `MenuNavegacao` obtém o `usuarioId` usado para montar as seções de Professor? Opções:
   - adicionar prop `usuarioId`/`usuarioIdDoPapelAtivo` ao `MenuNavegacao` e passá-la na integração do item 11 (que hoje só cita `papeis`/`papelAtivo`/`definirPapelAtivo`) — exige alterar a "mesma assinatura de `AlternadorDePapel`" declarada;
   - ou o `MenuNavegacao` resolve o id internamente (ex: `useSessao()` + `buscarPerfil`, espelhando o `usePerfilLogado` do Painel), o que contraria a premissa de organismo puramente controlado por props e duplica a busca já feita pelo Painel.

   Bloqueia o item 5 (e consequentemente toda a implementação do `MenuNavegacao`). Sem essa decisão, não há como seguir sem adivinhar.
