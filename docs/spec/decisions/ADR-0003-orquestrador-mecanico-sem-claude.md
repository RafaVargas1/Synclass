# ADR-0003: orquestrador mecânico do cron não depende de sessão Claude

- **Status**: aceita
- **Data**: 2026-08-20

## Contexto

O [ADR-0002](ADR-0002-continuidade-cruzada-limites.md) assumiu que "o lado
Claude sem limite" já estava resolvido pelo cron horário
(`~/.claude/cron-scripts/no_name-continue.sh`), porque ele dispara uma nova
sessão que lê o estado externo (`git`/`gh`/`task.md`) e continua dali. Essa
premissa está errada: o próprio `no_name-continue.sh` funciona disparando
`claude -p "$PROMPT"` — ou seja, o processo que decide o que fazer, aciona
`scripts/deepseek-agent.mjs`, roda `dev-review` e faz o merge **é ele
mesmo uma sessão Claude**, sujeita à mesma cota de uso da conta que a sessão
interativa do mantenedor.

O log de produção do cron (`~/.claude/cron-scripts/no_name-continue.log`)
confirma o efeito: em várias janelas horárias seguidas, a única saída da
execução é `You've hit your session limit`, repetida a cada disparo — o
`claude -p` morre antes de rodar qualquer `git status`/`gh pr list`, então
nunca chega a acionar `deepseek-agent.mjs`, mesmo esse harness rodando 100%
fora da cota do Claude (chama a API da DeepSeek direto via `fetch`, sem
`claude -p` no meio). O mesmo vale pra `synclass-worker`: é um subagente
Sonnet, herda a mesma cota da conta, não é uma via de escape.

Resultado prático: quando o Claude da conta está sem capacidade, **nada**
acontece — nem o merge de um PR já com `dev-review` aprovado e CI verde,
que é trabalho puramente mecânico e não deveria depender de LLM nenhum.
Isso contradiz o pedido original do mantenedor (ADR-0001/ADR-0002): a
DeepSeek deveria continuar codando e avançando a fila sem depender da
disponibilidade do Claude.

## Decisão

1. **Novo script `scripts/pipeline-orchestrator.sh`, sem `claude -p` em
   nenhum caminho de execução.** Roda como processo puro (bash + `gh` +
   `node scripts/deepseek-agent.mjs`), nunca abre uma sessão Claude Code.
   Cobre só o que é mecânico o bastante pra não exigir julgamento de LLM:
   - Continuar uma Task já especificada (`docs/specs/<n>-<slug>/task.md`
     já existe, com itens não marcados, sem seção `## Inconsistências
     encontradas` e sem `## Bloqueado por limite da API DeepSeek` recente):
     dispara `deepseek-agent.mjs` direto na worktree correspondente.
   - Distingue os códigos de saída do harness exatamente como o
     `synclass-worker` já faz (ADR-0002): 0 segue pro gate de CI, 3 (limite
     DeepSeek) só loga e sai — a próxima rodada tenta de novo — e 1
     (teto de iterações / inconsistência) também só loga, sem tentar
     resolver ambiguidade sozinho.
   - Depois de um harness bem-sucedido, roda o gate completo de
     `CONTRIBUTING.md#antes-de-abrir-um-pr` (`dotnet format && dotnet
     test`, `npm run lint && npm run typecheck && npm test`) direto em
     bash. Se verde e não houver PR ainda, abre um (`gh pr create`).
   - **Faz o squash-merge** (`gh pr merge --squash --delete-branch`) de
     qualquer PR que já esteja com CI verde **e** o marcador de
     `dev-review` aprovado (item 2 abaixo) — merge é decisão mecânica
     dado esses dois sinais, não julgamento novo.
   - Remove worktrees cujo PR correspondente já foi mergeado.
   - **Nunca** inicia uma Task nova (escrever `task.md`/`implementation.md`
     do zero) nem decide ambiguidade/bloqueio de produto — isso continua
     exigindo julgamento (Fase 1/2 do `fluxo-de-feature.md`), fica
     reservado pra quando uma sessão Claude estiver disponível.
2. **`dev-review` passa a postar um marcador legível por máquina** no
   comentário final (`.claude/skills/dev-review/SKILL.md`, Passo 7):
   uma linha `<!-- dev-review:status=aprovado -->` ou
   `<!-- dev-review:status=mudancas-solicitadas -->` (comentário HTML,
   invisível na renderização do GitHub) de acordo com o veredito do
   Passo 6. É o único acoplamento entre o julgamento (Claude, via
   `dev-review`) e o mecânico (`pipeline-orchestrator.sh`, que faz
   `gh pr view --json comments` e faz grep desse marcador pra decidir se
   pode mergear) — sem isso, "PR aprovado" só existiria em prosa, ilegível
   por script sem gastar uma chamada de LLM só pra interpretar.
3. **`no_name-continue.sh` vira duas etapas por execução horária:**
   - **Etapa A (sempre roda, imune a limite de Claude):**
     `scripts/pipeline-orchestrator.sh`. Avança o que já está especificado
     e mergeia o que já está aprovado, independente de sobrar cota de
     Claude.
   - **Etapa B (julgamento, best-effort):** dispara `claude -p "$PROMPT"`
     igual antes, mas com escopo reduzido — decidir a próxima Task da
     fila (spec nova), rodar `dev-review` nos PRs sem marcador ainda, e
     resolver bloqueios/ambiguidade. Se essa etapa falhar por limite de
     sessão, **não é mais uma execução perdida**: a Etapa A já rodou e já
     avançou o que dava pra avançar sem Claude.
4. Isso não muda a divisão de responsabilidade do ADR-0001 (DeepSeek
   implementa, Claude julga/revisa) — só corrige o mecanismo de disparo
   pra que a parte mecânica do julgamento (merge) não fique refém da
   disponibilidade da parte de julgamento em si.

## Consequências

**Positivas**: PRs com `dev-review` já aprovado e CI verde deixam de
esperar a próxima janela em que o Claude da conta tiver cota — mergeiam na
mesma hora em que ficam prontos. Tasks já especificadas continuam sendo
implementadas pela DeepSeek mesmo em janelas inteiras sem capacidade de
Claude. O cron para de "morrer em silêncio" quando bate limite — sempre
sobra pelo menos o trabalho mecânico da Etapa A.

**Negativas / riscos aceitos**:
- Novas Tasks (issues ainda sem `task.md`) continuam paradas até a Etapa B
  conseguir rodar — aceito, porque escrever spec técnica nova é julgamento
  real (Fase 2), não algo que se quer automatizar sem Claude no loop.
- O marcador de `dev-review` é convenção de texto (comentário HTML num
  comentário do PR) — se alguém postar manualmente um comentário parecido
  sem passar pela skill, o script pode ler o marcador errado. Mitigado por
  sempre usar o comentário mais recente que contenha o marcador, não o
  primeiro.
- `pipeline-orchestrator.sh` roda `gh pr merge` sem segunda revisão humana
  quando os dois sinais mecânicos batem — mesmo risco que já existia
  quando o `claude -p` do cron fazia squash-merge direto por instrução do
  mantenedor (rotina já roda com autonomia total); não é um risco novo
  introduzido por este ADR, só o mesmo risco sem depender de uma sessão
  Claude no caminho crítico.

## Alternativas consideradas

- **Aumentar o intervalo do cron ou adicionar retry com backoff dentro do
  `claude -p`** — não resolve nada: o problema não é frequência, é que o
  processo inteiro é uma sessão Claude e herda a cota de conta inteira
  enquanto ela durar.
- **Rodar o cron com uma conta/API key Claude separada só pra
  orquestração** — resolveria o sintoma, mas contradiz o objetivo de
  custo do ADR-0001 (reduzir gasto de Claude) e adiciona uma credencial
  nova pra gerenciar só pra tarefas que são mecânicas por natureza.
  Descartada.
- **Deixar a DeepSeek decidir também o merge, via prompt de julgamento
  na própria API DeepSeek** — descartada porque merge sem marcador
  estruturado exigiria a DeepSeek interpretar prosa do `dev-review`, o
  que é o mesmo problema que o marcador (item 2) já resolve de forma
  determinística e mais barata.
