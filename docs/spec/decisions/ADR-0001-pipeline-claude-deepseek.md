# ADR-0001: pipeline combinado Claude + DeepSeek

- **Status**: aceita
- **Data**: 2026-08-20

## Contexto

O pipeline autônomo do projeto (`docs/spec/fluxo-de-feature.md`, skill
`feature-flow`) usava o modelo principal da sessão (Claude, tipicamente
Sonnet) para todas as fases, incluindo a Fase 3 (implementação TDD) — a
tabela "Divisão de modelo/agente por fase" pinava explicitamente o agente
`synclass-worker` em Sonnet para implementação e revisão, com a
justificativa de que "o custo de erro em código de produção é alto" e por
isso não valia usar um modelo mais leve ali.

Essa decisão era válida no contexto em que foi tomada, mas o volume de
trabalho no backlog cresceu (épicos #60, #66, #72, mais de 20 issues numa
única sessão) e o custo de manter toda implementação em Claude passou a
pesar. O mantenedor pediu explicitamente reduzir o gasto de Claude no
longo prazo, mantendo qualidade via processo (Quality Contract +
quality gates determinísticos) em vez de depender só da capacidade de um
modelo único.

## Decisão

1. **DeepSeek assume a Fase 3** (implementação TDD, criação de testes,
   debugging, iteração até os quality gates passarem). Claude mantém as
   fases de julgamento alto: Fase 1 (issue), Fase 2 (reflexão/spec de
   produto), aprovação do plano técnico (`implementation.md`) antes da
   implementação começar em tarefas média/complexa, Fase 4 (revisão final
   — só `dev-review`) e Fase 6 (relatório).
2. **Mecanismo**: um harness próprio (`scripts/deepseek-agent.mjs`), não
   Claude orquestrando um loop de aplicar-cada-resposta-da-DeepSeek. A
   alternativa de Claude-no-loop foi descartada porque ainda gastaria
   tokens de Claude a cada iteração de TDD (leitura da resposta,
   aplicação do diff, execução de teste, reenvio do erro) — o harness
   próprio tira Claude do loop de implementação por completo, que é o que
   de fato reduz custo no longo prazo, ao custo de construir e manter um
   componente novo (um loop de function-calling contra a API da
   DeepSeek).
3. **`qa-review` sai do fluxo automático por padrão** — só `dev-review`
   roda a cada PR gerado pelo pipeline. `qa-review` (Playwright/UX)
   continua disponível sob pedido explícito do usuário, fora do fluxo
   automático. Mesmo corte já aplicado ao cron horário
   (`.claude/cron-scripts/no_name-continue.sh`) antes deste ADR, agora
   generalizado para o pipeline inteiro.
4. **Isso vira o novo default do `/feature-flow`**, inclusive para
   execuções já em andamento (cron horário).

## Consequências

**Positivas**: custo de Claude por Task cai (implementação, que é a fase
mais longa/iterativa, deixa de consumir tokens do modelo principal); o
board pode avançar mais rápido sem esperar disponibilidade/orçamento de
sessão do Claude.

**Negativas / riscos aceitos**:
- `scripts/deepseek-agent.mjs` executa comando de shell a partir do que a
  API da DeepSeek devolve, sem a camada de permissão/sandbox do Claude
  Code — mitigado por um guard mínimo (bloqueia padrões obviamente
  destrutivos) e log de todo comando, não por sandboxing completo (ver
  `security-rules.md#débitos-conhecidos`).
- Sem `qa-review` automático, uma tela nova só é validada por
  Playwright quando alguém pedir explicitamente — regressão visual/UX
  pode passar despercebida até esse pedido acontecer.
- Qualidade da implementação da DeepSeek depende inteiramente dos quality
  gates determinísticos (lint, typecheck, testes, build) e da revisão
  final do Claude pegarem o que escapar — o Quality Contract
  (`docs/spec/*.md`) precisa se manter atualizado e completo, já que é a
  única fonte de padrão que a DeepSeek segue (não tem a mesma "memória de
  projeto" acumulada que uma sessão Claude interativa constrói).

## Alternativas consideradas

- **Claude orquestra o loop de DeepSeek diretamente** (sem harness
  separado) — mais rápido de montar, mas não reduz custo de Claude no
  longo prazo (ele continua no loop a cada iteração). Descartada por não
  atender o objetivo principal do pedido.
- **Manter Claude implementando, usar DeepSeek só para rascunhos
  baratos** (ex: draft de RN, como já era feito com Haiku) — mantém o
  status quo de custo na fase mais cara (implementação). Descartada.
