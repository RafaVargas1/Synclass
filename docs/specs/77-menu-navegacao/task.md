# Task: Menu de navegação persistente e responsivo (#77)

Card: https://github.com/RafaVargas1/Synclass/issues/77

## Ordem de execução

- [x] Teste de componente: refatoração de `acoesProfessor`/`acoesAluno` do Painel — extrai as duas listas para `src/lib/secoesPorPapel.ts` sem mudar comportamento (testes existentes de `painel/index.test.tsx` continuam passando)
- [x] Implementação: extrai `acoesProfessor`/`acoesAluno` para `src/lib/secoesPorPapel.ts` (renomeando para `secoesProfessor`/`secoesAluno`) e atualiza `painel/index.tsx` para importar de lá
- [x] Teste unidade: hook `useIsTelaLarga` — retorna `true` acima do breakpoint (1024px), `false` abaixo (mock de `useWindowDimensions`)
- [x] Implementação: hook `useIsTelaLarga` (`src/lib/useIsTelaLarga.ts`) com `useWindowDimensions` e breakpoint fixo de 1024px
- [ ] Refatoração: extrai `usePerfilLogado` de `app/painel/index.tsx` para `src/lib/usePerfilLogado.ts` (mesma assinatura/comportamento, sem mudar `painel/index.test.tsx`)
- [ ] Teste de componente: `MenuNavegacao` — indica a seção atual a partir da rota ativa (mock de `usePathname` e de `usePerfilLogado` para o `usuarioId`), fazendo match por segmento dinâmico (ex: rota `/professor/abc-123/horarios` casa com a seção `/professor/[professorId]/horarios`)
- [ ] Implementação mínima: `MenuNavegacao.tsx` (organism) — recebe `papeis`, `papelAtivo`, `onSelecionarPapel` (mesmas props de `AlternadorDePapel`), resolve `token` via `useSessao()` e `usuarioId` via `usePerfilLogado(token)` internamente, deriva a lista de seções via `secoesPorPapel` e a seção ativa via `usePathname`
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

1. **`MenuNavegacao` não recebe `usuarioId`, mas as seções de Professor com segmento dinâmico dependem dele — RESOLVIDA pelo mantenedor (decisão de arquitetura, ver abaixo).**

   O harness chegou a essa mesma lacuna (`secoesProfessor(usuarioId)` exige
   `usuarioId`, que `useSessao()` não expõe — só vem de `GET /usuarios/me`)
   e propôs uma prop `usuarioId` extra em `MenuNavegacao`, preenchida por
   cada caller. **Decisão final (revisada pelo mantenedor): não usar prop
   extra.** Motivo: o item 15 já prevê `MenuNavegacao` integrado em
   "`app/painel/index.tsx` e demais telas via `Topbar`/wrapper" — ou seja,
   múltiplos pontos de integração, não só o Painel. Uma prop `usuarioId`
   obrigaria cada tela/wrapper a resolver `GET /usuarios/me` por conta
   própria só para repassar o valor, duplicando exatamente o hook que o
   Painel já tem (`usePerfilLogado`) em vez de reaproveitá-lo — o oposto do
   que a Task já fez com `secoesPorPapel.ts` (extrair para reuso em vez de
   duplicar).

   **Decisão adotada:** extrair `usePerfilLogado` de `painel/index.tsx`
   para `frontend/src/lib/usePerfilLogado.ts` (mesmo padrão da extração de
   `secoesPorPapel.ts` já feita nesta Task — módulo comportamental
   reaproveitável, sem mudar a assinatura nem o comportamento). `Painel`
   passa a importar de lá. `MenuNavegacao` chama `useSessao()` (para
   `token`, já que ele não vem mais como prop) e `usePerfilLogado(token)`
   **internamente**, usando só o campo `usuarioId` do resultado — mantém a
   assinatura de props exatamente como especificada originalmente
   (`papeis`, `papelAtivo`, `onSelecionarPapel`, iguais a
   `AlternadorDePapel`), sem prop nova, sem duplicar a chamada de rede em
   cada tela que montar o menu.

   **Impacto no spec:** novo item na ordem de execução, antes do item 6
   (extração de `usePerfilLogado`); o item 6 mantém a assinatura de props
   inalterada; o item 5 (teste) mocka `usePerfilLogado` (não recebe
   `usuarioId` via prop) para exercitar o match de seção ativa.
