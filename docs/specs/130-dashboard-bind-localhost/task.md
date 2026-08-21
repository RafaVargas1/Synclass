# Task: dashboard local do harness escutar só em 127.0.0.1

Card: achado de "Atenção" do dev-review no PR #125 (ver comentário
`dev-review — PR #125 (correção/complemento...)`), registrado como parte
da issue #129, extraído aqui pra rodar em paralelo por não tocar nenhum
arquivo em comum com a correção de `MenuNavegacao.tsx`/`Topbar.tsx`.

`scripts/dashboard-server.mjs` (`server.listen(PORT, ...)`, linha ~187)
hoje escuta em todas as interfaces de rede (comportamento padrão do Node
quando `listen` não recebe um host). É uma ferramenta interna, só usada
localmente por quem roda `scripts/deepseek-agent.mjs` na própria máquina —
não precisa estar acessível por outros hosts da rede, e deixar aberto
aumenta a superfície de ataque à toa (`docs/spec/security-rules.md`).

## Ordem de execução

- [ ] Teste (`node --test`, novo arquivo `scripts/dashboard-server.test.mjs`
      se ainda não existir, ou adicione a `scripts/dashboard-store.test.mjs`
      se fizer mais sentido colocalizado): suba o servidor numa porta
      efêmera (`0`) e confirme via `server.address().address` que o bind
      é `127.0.0.1` (ou `::1`, dependendo da stack IPv4/IPv6 do Node) —
      não `0.0.0.0`/`::`. Escreva o teste primeiro, veja-o falhar contra o
      código atual.
- [ ] Implementação mínima: `server.listen(PORT, '127.0.0.1', () => {...})`
      em `scripts/dashboard-server.mjs`. Não adicione autenticação, não
      mude o formato de `/api/ingest`/`/api/state`/`/api/logs`, não mude
      nenhum outro arquivo — escopo é só o bind de interface.
- [ ] Rode `npm test` (raiz do repo, não `frontend/`) pra confirmar que os
      testes existentes de `dashboard-store.test.mjs`/`event-tracker.test.mjs`
      continuam passando.

## Fora de escopo

- Não mexer em `dashboard.html`, `dashboard-store.mjs`,
  `dashboard-historico.mjs`, `event-tracker.mjs` — nenhum desses precisa
  mudar pra este fix.
- Não adicionar autenticação ao `/api/ingest` (fora de escopo desta Task,
  registrado só como observação no achado original do dev-review).
