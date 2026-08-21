# ADR-0002: continuidade cruzada quando Claude ou DeepSeek batem limite

- **Status**: aceita
- **Data**: 2026-08-20

## Contexto

O [ADR-0001](ADR-0001-pipeline-claude-deepseek.md) tirou Claude do loop de
implementação (Fase 3 vai para a DeepSeek via `scripts/deepseek-agent.mjs`),
mas não separou dois motivos bem diferentes pelos quais esse harness pode
sair com erro:

1. **Limite/quota da API DeepSeek** (HTTP 429, saldo insuficiente) — não é
   um problema da Task, é a DeepSeek temporariamente indisponível. Hoje o
   harness trata isso como qualquer outra falha: sai com código 1, e
   `synclass-worker`/o cron de fallback (`no_name-continue.sh`, hora em
   hora) não distinguem essa saída de um teto de iterações genuíno — o que
   empurra para o fallback explícito (Claude implementa na mão), anulando
   na hora o ganho de custo que o ADR-0001 existe para capturar.
2. **Sessão Claude fica sem capacidade de uso no meio do fluxo** — já tem
   mitigação: o cron `no_name-continue.sh` (hora em hora) dispara uma nova
   sessão `claude -p` que lê o estado real via `git status`/`git worktree
   list`/`gh pr list`/`gh issue list` e continua dali, porque o progresso
   já vive fora da sessão (commits, PRs, board, `task.md` com checkboxes).
   Esse mecanismo já resolve o "Claude vai e volta" — não precisa de
   handoff novo, só precisa continuar sendo o padrão documentado (o que
   este ADR faz explicitamente, para não ser reinventado numa Task futura).

O pedido do mantenedor foi: se um dos dois modelos ficar sem limite no meio
do trabalho, o outro continua, e quando o limite volta, quem voltou retoma
de onde parou — sem duplicar trabalho, sem exigir handoff manual.

## Decisão

1. **`deepseek-agent.mjs` distingue limite de falha real.** Ao detectar
   erro de rate limit/quota na resposta da API DeepSeek (`error.type` ou
   mensagem contendo padrões conhecidos de 429/saldo insuficiente), o
   harness:
   - não escreve `## Inconsistências encontradas` no `task.md` (isso é
     reservado para ambiguidade de produto/código, que exige julgamento);
   - grava uma seção `## Bloqueado por limite da API DeepSeek` no
     `task.md`, com timestamp, para o item em andamento;
   - loga o evento em `deepseek-run.log`;
   - sai com código **3** (distinto de 0=sucesso e 1=falha genérica/teto de
     iterações).
2. **`synclass-worker` e o cron de fallback tratam a saída 3 como "tentar
   de novo mais tarde", não como sinal para implementar na mão.** Se o
   harness sair com código 3, o worker não aciona o fallback explícito
   (Claude implementando) — ele registra o bloqueio e devolve controle
   para quem chamou (a próxima rodada do cron, ou o usuário, tenta o
   mesmo comando de novo). Isso preserva o objetivo do ADR-0001: só cai
   para Claude implementar quando a causa é ambiguidade real ou teto de
   iterações genuíno, nunca quota temporária.
3. **O lado "Claude sem limite" continua sendo o cron horário existente**
   (`~/.claude/cron-scripts/no_name-continue.sh`), sem mecanismo novo —
   ele já resume do estado externo (git/GH/`task.md`), que é justamente o
   que este ADR generaliza como o contrato de continuidade dos dois lados:
   **todo progresso relevante fica fora da sessão/processo que o produziu**
   (commit, PR, checkbox de `task.md`, seção de bloqueio), nunca só na
   memória de uma sessão `claude -p` ou de uma chamada à API DeepSeek.
4. Isso não cria um daemon nem um scheduler novo para o lado DeepSeek — o
   próprio cron horário, ao tentar continuar a Task pendente, naturalmente
   redispara `deepseek-agent.mjs` e tenta de novo. Se o limite da DeepSeek
   ainda não tiver voltado, o harness sai com código 3 de novo e a rodada
   seguinte tenta outra vez — comportamento aceitável dado o intervalo de
   uma hora do cron.

## Consequências

**Positivas**: quota temporária da DeepSeek não força mais Claude a
implementar na mão (preserva o ganho de custo do ADR-0001); o
comportamento de "Claude sem limite → cron retoma" fica documentado como
decisão, não como acidente de infraestrutura.

**Negativas / riscos aceitos**:
- Não há retry imediato dentro da mesma execução — se a quota da DeepSeek
  voltar em minutos, a Task só é retomada na próxima janela do cron (até
  1h de espera). Aceito porque o objetivo é continuidade, não latência
  mínima, e a rotina já tolera esse intervalo para o lado Claude.
- A detecção de "erro de limite" depende de padrões de mensagem/tipo da
  API DeepSeek observados até aqui — se a API mudar o formato do erro sem
  aviso, o harness pode voltar a tratar quota como falha genérica
  (degrada para o comportamento anterior, não silenciosamente pior).

## Alternativas consideradas

- **Handoff explícito Claude→DeepSeek dentro da mesma sessão** (a sessão
  Claude, ao perceber que está perto do limite, dispara o harness antes de
  encerrar) — descartado por já ser coberto pelo cron horário: a sessão
  não precisa prever o próprio fim, o cron já garante que alguém volta a
  olhar o estado dentro de 1h.
- **Retry com backoff dentro do próprio `deepseek-agent.mjs`** (dormir e
  tentar de novo na mesma execução) — descartado porque quota da API
  costuma levar mais que segundos/minutos para resetar, e manter o
  processo dormindo prende a worktree/recursos sem necessidade; o cron já
  é o mecanismo de retry, num intervalo mais realista.
