---
name: ux-review
description: Auditoria de usabilidade/UX feita pelo Claude sobre telas que a DeepSeek implementou (via harness, ADR-0001) no projeto Synclass — dispara quando o usuário pede para "revisar a UX", "auditar usabilidade", "achar problemas de UX/UI", "revisar o que a DeepSeek fez usando o Claude" ou similar. Ao contrário de `dev-review` (que audita PRs individuais contra `code-style.md` e é bloqueante), esta skill audita telas já mergeadas contra `docs/spec/ux-heuristics.md` e não bloqueia nada — cada achado real vira uma issue no GitHub (label `ux`), para entrar na fila normal do backlog. Não posta comentário em PR nem precisa de confirmação antes de `gh issue create` (issue nova é reversível — fechar/editar depois — ao contrário de um comentário público).
---

# ux-review

Você vai auditar telas do Synclass contra `docs/spec/ux-heuristics.md`
(hierarquia visual, alvo de toque, navegação/retorno, reconhecimento vs.
recordação, agrupamento Gestalt, número de opções simultâneas) e, quando
aplicável, contra as heurísticas gerais de Nielsen que esse documento já
referencia mas não enumera uma a uma (visibilidade do status do sistema,
prevenção de erro, reconhecimento > memorização de convenção de plataforma
— ex: botão "Entrar com Google" que não segue o padrão visual oficial do
Google quebra confiança, mesmo funcionando).

Esta skill não é um gate de PR — roda por iniciativa do usuário ou como
varredura periódica sobre trabalho já mergeado (a implementação em si já
passou por `dev-review`, que audita código, não usabilidade). O
entregável são issues novas, não um veredito de aprovação.

## Passo 1 — Resolver o alvo

Aceite: número(s) de PR já mergeado(s), uma lista de rotas/telas
(`frontend/src/app/...`), ou nenhum argumento.

- Nenhum argumento: audite as telas tocadas pelos PRs mergeados desde a
  última execução desta skill (verifique se há uma nota em
  `docs/spec/decisions/` ou peça ao usuário o intervalo — ex: "desde
  ontem", "o lote #60-89"). Na ausência de qualquer sinal, peça ao usuário
  para especificar em vez de adivinhar o escopo (`AskUserQuestion`).
- PR(s): `gh pr view <n> --json files` para a lista de arquivos, filtrando
  só rotas/componentes de UI (`frontend/src/app/**`,
  `frontend/src/components/**`) — ignore testes, specs, backend.
- Rotas explícitas: use a lista dada.

## Passo 2 — Ler cada tela como o usuário veria

Para cada arquivo de rota/tela na lista, leia o componente e os
organismos/moléculas que ele monta (siga os imports até o nível de botão/
texto visível — não pare no primeiro componente). Não rode o app nem tire
screenshot (isso é `qa-review`, Playwright, fora do escopo desta skill) —
a leitura de código já é suficiente para julgar hierarquia visual
(`className`/estilo), rótulos de texto, e presença/ausência de estado de
carregamento/erro.

## Passo 3 — Checklist contra `docs/spec/ux-heuristics.md`

Releia o documento antes de aplicar (as regras têm referência formal, não
são gosto pessoal). Para cada tela do Passo 2, avalie:

1. **Hierarquia de CTA** (Gestalt, seção "Agrupamento visual"): a ação que
   o usuário mais provavelmente quer/precisa tem o maior peso visual
   (botão preenchido, posição de destaque)? Uma ação secundária nunca deve
   competir ou vencer a primária em peso visual — releia a ordem de
   frequência de uso esperada, não a ordem de implementação.
2. **Alvo de toque** (Fitts/WCAG): todo `Pressable`/botão custom tem
   ≥44×44pt de área de toque efetiva, não só o glifo/texto visível.
3. **Navegação e retorno** (Nielsen #3/#4): todo fluxo tem saída clara;
   controles de mesmo papel (voltar, cancelar, entrar) ficam no mesmo
   lugar/comportamento em toda tela.
4. **Reconhecimento em vez de recordação** (Krug/Nielsen #6/Norman): texto
   de botão/link comunica a ação sem exigir que o usuário infira (ex:
   "Cadastrar como Aluno" comunica ação; "sou Aluno" comunica identidade,
   não ação — ambíguo com login). Ícone de terceiro (Google, Apple, redes
   sociais) segue o *signifier* oficial da marca (logo, cor, formato já
   convencionado) — um botão de "Entrar com X" que reaproveita o estilo
   genérico do app em vez do padrão oficial do provedor quebra
   reconhecimento e passa desconfiança (parece phishing/clone).
5. **Número de opções simultâneas** (Hick/Miller): a tela não apresenta
   mais escolhas de uma vez do que o necessário; o caminho mais frequente
   (ex: usuário recorrente entrando de novo) não deveria exigir mais
   cliques/decisões que o caminho raro (ex: primeiro cadastro).
6. **Visibilidade do status do sistema** (Nielsen #1): toda ação
   assíncrona (rede, autenticação) mostra estado de carregamento/erro —
   não fica muda entre o toque e o resultado.

Só registre um achado quando ele é concreto e acionável (aponta pra um
arquivo/linha e descreve o que muda) — não gere achados de gosto estético
sem base em regra do documento.

## Passo 4 — Criar uma issue por achado real

Para cada achado confirmado, escreva um card seguindo
`docs/backlog/padrao-de-issue.md` (as 6 seções: história de usuário, RN,
critérios de aceite Gherkin, critérios técnicos, contexto/ref) — mesmo
padrão que qualquer outra issue do backlog, pra poder entrar na fila
normal do `feature-flow`/harness DeepSeek depois. Na RN, cite a regra de
UX violada (ex: "viola Fitts/WCAG 2.5.8 — ver
`docs/spec/ux-heuristics.md#alvos-de-toque`") e o arquivo/linha atual.

```bash
gh issue create --repo RafaVargas1/Synclass --title "<título direto>" \
  --body "<card completo>" --label ux
```

Não peça confirmação antes de `gh issue create` — issue é reversível
(fechar/editar), diferente de comentário público em PR. Se o achado for
grande/fullstack (ex: redesenhar um fluxo inteiro de auth), aplique o
mesmo critério de quebra em Épico + Task de `padrao-de-issue.md`.

## Passo 5 — Relatório final

Liste as issues criadas (número + título + link), agrupadas por tela.
Se nenhum achado real surgiu numa tela, diga isso explicitamente (não
omita — "tela X auditada, sem achado" é informação útil). Não publique
Artifact nem notificação separada para isto — a lista de issues no chat
já é o entregável; o usuário decide se quer implementar agora ou deixar
na fila.
