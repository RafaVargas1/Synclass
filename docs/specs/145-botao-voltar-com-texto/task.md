# Task: botão Voltar com texto, numa linha própria (#145)

Card: https://github.com/RafaVargas1/Synclass/issues/145

Leia `implementation.md` ANTES do primeiro item — código pronto pra colar,
incluindo o teste ajustado.

## Ordem de execução

- [x] Teste (`Topbar.test.tsx`): adicione os dois testes novos descritos em
      `implementation.md` ("Voltar" visível como texto quando há `titulo`;
      nenhum botão "Voltar" quando não há `titulo`). Ver falhar.
- [x] Implementação mínima: aplique a mudança de `implementation.md`
      (`Titulo` sem seta, `BotaoVoltar` numa linha própria abaixo do
      cabeçalho).
- [x] Rode a suíte completa de `Topbar.test.tsx` e `TopbarAutenticada.test.tsx`
      — confirme que os testes existentes (voltar com histórico, voltar
      sem histórico pra `/`, alvo de toque ≥44×44) continuam passando sem
      alteração de query.
- [x] `grep -rn "TituloComVoltar" frontend/src` — confirme que não sobrou
      nenhuma referência ao nome antigo (renomeado pra `Titulo`).
- [x] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md`.

## Fora de escopo

Ver seção "Fora de escopo" de `implementation.md`.
