# Task: título da aba do navegador mostra "Synclass - <Página>" (#133)

Card: https://github.com/RafaVargas1/Synclass/issues/133

Leia `implementation.md` ANTES do primeiro item — a causa raiz já foi
investigada e confirmada (expo-router sempre monta `Head.Provider`/
react-helmet-async por baixo; `document.title` imperativo é sobrescrito
por ele). Não re-investigue do zero, implemente a correção já desenhada:
trocar o mecanismo imperativo (`useNavigation().setOptions`) por
`<Head><title>` de verdade (`expo-router/head`).

## Ordem de execução

- [ ] Teste (`frontend/src/lib/TituloDaAba.test.tsx`, criar): os 2
      cenários descritos em `implementation.md` (prefixo "Synclass - "
      aplicado; não duplicado se já presente). Ver falhar.
- [ ] Implementação mínima: cria `frontend/src/lib/TituloDaAba.tsx`
      conforme `implementation.md`. Apaga `frontend/src/lib/useTituloDaAba.ts`
      e `frontend/src/lib/useTituloDaAba.test.ts` (substituídos).
- [ ] Teste (`Topbar.test.tsx`): remove o mock de `useNavigation`/
      `mockSetOptions` (não usado mais) e os 2 testes antigos de
      `mockSetOptions`; adiciona os 3 testes novos de `TituloDaAba`
      descritos em `implementation.md` (titulo → tab title; tituloDaAba
      como fallback quando titulo ausente; fallback bare "Synclass" sem
      nenhum dos dois).
- [ ] Implementação mínima: `Topbar.tsx` — troca o import de
      `useTituloDaAba` por `TituloDaAba`, adiciona a prop `tituloDaAba`,
      envolve o JSX num Fragment com `<TituloDaAba titulo={tituloDaAba ??
      titulo ?? 'Synclass'} />` como primeiro filho (código exato em
      `implementation.md`).
- [ ] Teste (`TopbarAutenticada.test.tsx`): adiciona teste de repasse da
      prop `tituloDaAba` pro `Topbar` mockado (mesmo padrão dos testes de
      repasse de `titulo`/`children` já existentes neste arquivo).
- [ ] Implementação mínima: `TopbarAutenticada.tsx` — adiciona a prop
      `tituloDaAba`, repassa pro `Topbar`.
- [ ] Teste (`frontend/src/app/painel/index.test.tsx`): atualiza o mock
      de `TopbarAutenticada` pra capturar props recebidas (não só
      renderizar `children`) e adiciona teste que `tituloDaAba="Painel"`
      é passado.
- [ ] Implementação mínima: `frontend/src/app/painel/index.tsx` — troca
      `<TopbarAutenticada>` por `<TopbarAutenticada tituloDaAba="Painel">`
      (sem mudar mais nada da tela).
- [ ] Teste (`frontend/src/app/index.test.tsx`, Home): adiciona mock de
      `TituloDaAba` e teste que `titulo="Início"` é passado.
- [ ] Implementação mínima: `frontend/src/app/index.tsx` — envolve o
      retorno num Fragment com `<TituloDaAba titulo="Início" />` como
      primeiro filho, antes de `<HomeTemplate>` (código exato em
      `implementation.md`).
- [ ] `npm run lint && npm run typecheck && npm test` (suíte completa)
      verde. Confirme especificamente que nenhum dos outros 15 call sites
      de `Topbar`/`TopbarAutenticada` (lista abaixo) quebrou — todos já
      passam `titulo`, não deveriam precisar de nenhuma mudança.
- [ ] Verificação visual (Playwright, se disponível no ambiente): suba
      `npx expo start --web`, confirme `page.title()` em `/`, `/login`, e
      uma tela autenticada (`/painel` com sessão fake) — nunca vazio,
      nunca só o host/porta, sempre `"Synclass - <algo>"`. Se não for
      possível verificar no ambiente, registre isso explicitamente em
      "## Inconsistências encontradas" em vez de pular em silêncio.
- [ ] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md`.

## Call sites que já passam `titulo` (não devem precisar de mudança)

`perfil.tsx`, `professor/cadastro.tsx`, `professor/alunos/cadastro.tsx`,
`professor/[professorId]/convites/novo.tsx`,
`aluno/professores/[professorId]/horarios.tsx`,
`professor/[professorId]/matriculas/[matriculaId]/regra-de-cobranca.tsx`,
`aluno/historico-frequencia.tsx`, `login/index.tsx`,
`aluno/valor-devido.tsx`,
`professor/[professorId]/horarios/[horarioId]/chamada.tsx`,
`professor/[professorId]/alunos.tsx`, `login/verificar.tsx`,
`professor/[professorId]/horarios.tsx`,
`professor/[professorId]/alocacoes.tsx`,
`professor/[professorId]/valor-devido.tsx`,
`aluno/professores/[professorId]/minhas-aulas.tsx`, `aluno/index.tsx`.

## Fora de escopo

Ver seção "Fora de escopo" de `implementation.md`.
