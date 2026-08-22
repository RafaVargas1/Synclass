# Task: título da aba do navegador mostra "Synclass - <Página>" (#133)

Card: https://github.com/RafaVargas1/Synclass/issues/133

Leia `implementation.md` ANTES do primeiro item — a causa raiz já foi
investigada e confirmada (expo-router sempre monta `Head.Provider`/
react-helmet-async por baixo; `document.title` imperativo é sobrescrito
por ele). Não re-investigue do zero, implemente a correção já desenhada:
trocar o mecanismo imperativo (`useNavigation().setOptions`) por
`<Head><title>` de verdade (`expo-router/head`) + `setOptions` em
conjunto (ver "Inconsistências encontradas" abaixo).

## Ordem de execução

- [x] Teste (`frontend/src/lib/TituloDaAba.test.tsx`, criar): os 2
      cenários descritos em `implementation.md` (prefixo "Synclass - "
      aplicado; não duplicado se já presente). Ver falhar.
- [x] Implementação mínima: cria `frontend/src/lib/TituloDaAba.tsx`
      conforme `implementation.md`. Apaga `frontend/src/lib/useTituloDaAba.ts`
      e `frontend/src/lib/useTituloDaAba.test.ts` (substituídos).
- [x] Teste (`Topbar.test.tsx`): remove o mock de `useNavigation`/
      `mockSetOptions` (não usado mais) e os 2 testes antigos de
      `mockSetOptions`; adiciona os 3 testes novos de `TituloDaAba`
      descritos em `implementation.md` (titulo → tab title; tituloDaAba
      como fallback quando titulo ausente; fallback bare "Synclass" sem
      nenhum dos dois).
- [x] Implementação mínima: `Topbar.tsx` — troca o import de
      `useTituloDaAba` por `TituloDaAba`, adiciona a prop `tituloDaAba`,
      envolve o JSX num Fragment com `<TituloDaAba titulo={tituloDaAba ??
      titulo ?? 'Synclass'} />` como primeiro filho (código exato em
      `implementation.md`).
- [x] Teste (`TopbarAutenticada.test.tsx`): adiciona teste de repasse da
      prop `tituloDaAba` pro `Topbar` mockado (mesmo padrão dos testes de
      repasse de `titulo`/`children` já existentes neste arquivo).
- [x] Implementação mínima: `TopbarAutenticada.tsx` — adiciona a prop
      `tituloDaAba`, repassa pro `Topbar`.
- [x] Teste (`frontend/src/app/painel/index.test.tsx`): atualiza o mock
      de `TopbarAutenticada` pra capturar props recebidas (não só
      renderizar `children`) e adiciona teste que `tituloDaAba="Painel"`
      é passado.
- [x] Implementação mínima: `frontend/src/app/painel/index.tsx` — troca
      `<TopbarAutenticada>` por `<TopbarAutenticada tituloDaAba="Painel">`
      (sem mudar mais nada da tela).
- [x] Teste (`frontend/src/app/index.test.tsx`, Home): já cobria isso
      indiretamente (mock de `TituloDaAba` intercepta a chamada feita de
      dentro do `Topbar` real usado por `HomeTemplate`) — ver
      "Inconsistências encontradas" sobre por que a implementação mudou
      de rota (título vem de dentro de `HomeTemplate`, não de um
      `TituloDaAba` solto em `app/index.tsx`).
- [x] Implementação mínima: `HomeTemplate.tsx` — `<Topbar tituloDaAba="Início">`
      (não `app/index.tsx` como o plano original previa — ver
      "Inconsistências encontradas").
- [x] `npm run lint && npm run typecheck && npm test` (suíte completa)
      verde — 530/530. Nenhum dos outros 15 call sites de
      `Topbar`/`TopbarAutenticada` precisou de mudança.
- [x] Verificação visual (Playwright): `page.title()` em `/` → "Synclass
      - Início", `/login` → "Synclass - Entrar", `/painel` (sessão fake)
      → "Synclass - Painel". Confirmado.
- [x] Refatore: releu o diff final contra `docs/spec/code-style.md`.

## Inconsistências encontradas (resolvidas nesta mesma Task, registradas pra referência)

- **`HomeTemplate.tsx` já renderizava seu próprio `<Topbar>`** (sem
  `titulo`) — o plano original (`implementation.md`) previa um
  `<TituloDaAba titulo="Início">` solto direto em `app/index.tsx`, sem
  perceber que isso criaria DOIS `TituloDaAba` montados na mesma tela (um
  nosso, um de dentro do `Topbar` do `HomeTemplate`, cujo fallback sem
  `tituloDaAba`/`titulo` é `'Synclass'` puro) — os dois efeitos corriam
  pra escrever `document.title`, e o de dentro do `Topbar` (sem
  `tituloDaAba`) vencia, sempre resultando em "Synclass" puro na Home.
  Confirmado ao verificar visualmente com Playwright (não só rodando os
  testes — os testes unitários, mockando `TituloDaAba`, não pegam dois
  mocks concorrentes da mesma forma que o app real). Corrigido: em vez de
  um `TituloDaAba` solto em `app/index.tsx`, `HomeTemplate.tsx` passa
  `tituloDaAba="Início"` pro seu próprio `Topbar` — UM único
  `TituloDaAba` montado por tela, sempre.
- **Regra geral pra próximas Tasks que tocarem título de aba**: antes de
  adicionar um `TituloDaAba` (direto ou indireto) numa tela, confirme que
  nenhum componente já usado por ela (`Topbar`/`TopbarAutenticada`, ou
  outro template) já renderiza um. Dois na mesma árvore competem pelo
  resultado final, e o teste unitário (que mocka o componente) não
  detecta a colisão — só a verificação visual pega isso.

## Call sites que já passam `titulo` (não precisaram de mudança, confirmado)

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
