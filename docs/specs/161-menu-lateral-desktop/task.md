# Task: menu vira painel lateral persistente também no desktop (#161)

Card: https://github.com/RafaVargas1/Synclass/issues/161

Leia `implementation.md` ANTES do primeiro item — decisão de design já
resolvida (onde a coluna é montada, por quê), código pronto pros 3
arquivos tocados. Esta Task tem risco de regressão alto (toca o layout
raiz do app) — siga a ordem exata, rode a suíte completa a cada passo, e
NÃO pule a verificação visual final.

## Ordem de execução

- [ ] Teste (`_layout.test.tsx`, criar): os 3 cenários de `AppShell`
      descritos em `implementation.md` (com sessão + tela larga → coluna
      aparece; sem sessão → não aparece; tela estreita → não aparece). Ver
      falhar.
- [ ] Implementação mínima: `frontend/src/app/_layout.tsx` conforme
      `implementation.md`.
- [ ] `npm test` (suíte completa, não só o arquivo novo) — confirme que
      nada mais quebrou só com essa primeira mudança antes de seguir.
- [ ] Teste (`MenuNavegacao.test.tsx`): ajuste os testes de `telaLarga`
      pra nova classe de coluna vertical (`implementation.md`).
- [ ] Implementação mínima: `MenuNavegacao.tsx` — branch `telaLarga` vira
      coluna vertical, `largoTotal` sempre `true` no `.map`.
- [ ] Teste (`Topbar.test.tsx`): remova/ajuste o teste de ordem
      menu-antes-do-Voltar em tela larga (não existe mais esse
      comportamento aqui) — comente por quê, não apague sem explicação.
- [ ] Implementação mínima: `Topbar.tsx` — remove o branch `telaLarga` do
      bloco de `menuNavegacao`, deixa só o Voltar numa linha sempre
      (`implementation.md`).
- [ ] `npm run lint && npm run typecheck && npm test` (suíte completa)
      verde.
- [ ] Verificação visual (Playwright, se disponível no ambiente): suba
      `npx expo start --web`, injete sessão fake (`localStorage`,
      `synclass.sessao.token`/`synclass.sessao.papeis`), viewport
      1440×900, confirme visualmente que a coluna lateral aparece à
      esquerda, com altura cheia, e o conteúdo de uma tela (ex: `/painel`)
      renderiza normalmente ao lado — sem sobreposição, sem menu
      duplicado. Se não for possível verificar visualmente no ambiente,
      registre isso em "## Inconsistências encontradas" explicitamente
      em vez de pular a etapa em silêncio.
- [ ] Refatore se necessário: releia o diff final contra
      `docs/spec/code-style.md` e `docs/spec/engenharia-de-qualidade.md`.

## Fora de escopo

Ver seção "Fora de escopo" de `implementation.md`.
