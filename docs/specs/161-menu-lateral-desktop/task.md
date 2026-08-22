# Task: menu vira painel lateral persistente também no desktop (#161)

Card: https://github.com/RafaVargas1/Synclass/issues/161

Leia `implementation.md` ANTES do primeiro item — decisão de design já
resolvida (onde a coluna é montada, por quê), código pronto pros 3
arquivos tocados. Esta Task tem risco de regressão alto (toca o layout
raiz do app) — siga a ordem exata, rode a suíte completa a cada passo, e
NÃO pule a verificação visual final.

## Ordem de execução

- [x] Teste (`AppShell.test.tsx`, criar): os 3 cenários de `AppShell`
      descritos em `implementation.md` (com sessão + tela larga → coluna
      aparece; sem sessão → não aparece; tela estreita → não aparece). Ver
      falhar. Arquivo criado pelo harness como `_layout.test.tsx` e depois
      renomeado (ver "Inconsistências encontradas").
- [x] Implementação mínima: `frontend/src/app/_layout.tsx` conforme
      `implementation.md`.
- [x] `npm test` (suíte completa, não só o arquivo novo) — confirme que
      nada mais quebrou só com essa primeira mudança antes de seguir.
- [x] Teste (`MenuNavegacao.test.tsx`): ajuste os testes de `telaLarga`
      pra nova classe de coluna vertical (`implementation.md`). Nenhum
      teste existente assertava a classe exata (`flex-row flex-wrap`/
      `border-t`) — suíte já passa sem ajuste.
- [x] Implementação mínima: `MenuNavegacao.tsx` — branch `telaLarga` vira
      coluna vertical, `largoTotal` sempre `true` no `.map`.
- [x] Teste (`Topbar.test.tsx`): remova/ajuste o teste de ordem
      menu-antes-do-Voltar em tela larga (não existe mais esse
      comportamento aqui) — comente por quê, não apague sem explicação.
- [x] Implementação mínima: `Topbar.tsx` — remove o branch `telaLarga` do
      bloco de `menuNavegacao`, deixa só o Voltar numa linha sempre
      (`implementation.md`).
- [x] `npm run lint && npm run typecheck && npm test` (suíte completa)
      verde.
- [x] Verificação visual (Playwright): `npx expo start --web`, sessão fake
      via `localStorage`, viewport 1440×900, `/painel` — coluna lateral
      aparece à esquerda, altura cheia, conteúdo ao lado sem sobreposição
      nem menu duplicado. Confirmado.
- [x] Refatore: comentários desatualizados em `Topbar.tsx`/
      `MenuNavegacao.tsx` (mencionavam a antiga faixa horizontal) revisados
      pra refletir a coluna lateral.

## Inconsistências encontradas

- `_layout.test.tsx` (nome pedido originalmente) quebra o dev server web
  (`npx expo start --web`): o Expo Router trata arquivos `_layout.*` como
  especiais e gera a árvore de rotas por uma varredura própria que não
  respeita o `blockList` do `metro.config.js` (o mesmo bloqueio que já
  exclui `.test.tsx` comuns, como `cadastro.test.tsx`, funciona
  normalmente pra esses). Resultado: `Metro error: The layouts
  "./_layout.test.tsx" and "./_layout.tsx" conflict on the route
  "/_layout.test"`, dev server preso em erro. Resolvido renomeando o
  arquivo pra `AppShell.test.tsx` (mesmo diretório, mesmo conteúdo,
  importa `./_layout` normalmente) — sem esse prefixo reservado, o
  conflito desaparece e o `npm test` continua cobrindo os 3 cenários
  normalmente.

## Fora de escopo

Ver seção "Fora de escopo" de `implementation.md`.
