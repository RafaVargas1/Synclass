# ADR-0004: autorrevisão pela DeepSeek e revisão final única do Claude

- **Status**: aceita
- **Data**: 2026-08-20

## Contexto

ADR-0001/ADR-0003 já tiravam Claude do loop de implementação e do merge
mecânico, mas mantinham Claude como o único ator de revisão (`dev-review`,
Fase 4) — o gate de merge do `pipeline-orchestrator.sh` só libera um PR
quando encontra o marcador `<!-- dev-review:status=aprovado -->`, que só a
skill `dev-review` (sessão Claude) escreve.

O mantenedor pediu, para a leva atual de 14 issues (#60-#89, restantes
depois de #60/#71/#72/#73-76/#61-64/#70 já resolvidas), reduzir o uso de
Claude ao mínimo possível: implementação **e** revisão por DeepSeek, em
paralelo, coordenado pelo próprio mecanismo do harness/orquestrador — Claude
entra só para escrever spec técnica nova (Fase 2.5, julgamento real) e para
**uma única revisão grande no final**, depois que as 14 issues estiverem
mergeadas ou explicitamente bloqueadas. Pediu também que travas de
ambiguidade virem issue no GitHub (não pausa esperando Claude) e que os
commits da DeepSeek sejam claros o bastante para facilitar reversão nessa
revisão final tardia, já que ela vai avaliar as 14 de uma vez, não uma a
uma logo após cada merge.

## Decisão

1. **Novo script `scripts/deepseek-review.mjs`** — chama a API DeepSeek
   (mesma convenção de chave/env de `deepseek-agent.mjs`) com o diff do PR
   e o mesmo checklist mecânico de `docs/spec/code-style.md` que
   `dev-review` usa (tamanho de função/arquivo, nomes específicos, tipos
   explícitos, DI via construtor, wrapper de terceiro, early return,
   mensagens de exceção, cobertura de teste por função nova). Produz a
   mesma tabela de achados e o mesmo marcador de máquina
   (`<!-- dev-review:status=aprovado -->` ou
   `<!-- dev-review:status=mudancas-solicitadas -->`), postado via
   `gh pr comment` — **sem** sessão Claude no caminho. Isso é o único
   ponto de acoplamento que muda: o gate de merge do
   `pipeline-orchestrator.sh` continua lendo o mesmo marcador, não sabe
   nem precisa saber quem o escreveu.
2. **`pipeline-orchestrator.sh` chama `deepseek-review.mjs` automaticamente**
   depois de abrir um PR novo, em vez de deixar isso pendente pra Etapa B
   (Claude). Se o veredito for "mudanças solicitadas", o orquestrador
   reabre o `task.md` com os achados anexados (nova seção `## Achados da
   autorrevisão`) e redispara `deepseek-agent.mjs` para corrigir — até 2
   rodadas de correção automática; na 3ª tentativa sem aprovação, trata
   como bloqueio (item 3).
3. **Bloqueio vira issue no GitHub, nunca pausa esperando Claude.** Quando
   `deepseek-agent.mjs` sair com código 1 (teto de iterações ou
   `## Inconsistências encontradas`) ou a autorrevisão não aprovar depois
   de 2 rodadas de correção, o orquestrador roda
   `gh issue create` referenciando a issue/Task original, com o conteúdo
   da seção de bloqueio e o link do branch/worktree, marca o `task.md`
   local como `## Bloqueado — ver issue #<nova>` (não deleta, fica de
   rastro) e **segue para a próxima Task disponível na fila**, na mesma
   execução — não é mais um `judgment_needed=yes` que só a Etapa B
   resolve; o próprio orquestrador mecânico já sabe abrir a issue e
   seguir. Etapa B (Claude) só entra se a issue aberta precisar de decisão
   de produto — mas isso já é trabalho normal de backlog, não bloqueio de
   pipeline.
4. **`pipeline-orchestrator.sh` processa mais de uma Task por execução,
   em paralelo**, uma por worktree independente (mesmo critério de
   independência já documentado em
   `fluxo-de-feature.md#fase-3--implementação`: não paralelizar Tasks que
   tocam `SynclassDbContext.cs`/`Program.cs`/migration nova ao mesmo
   tempo). Dispara um `deepseek-agent.mjs` em background por worktree
   elegível e aguarda todos antes de passar para o passo de merge —
   diferente do comportamentos anterior (uma Task por execução,
   sequencial).
5. **Epics (#66, #72, e futuros) são fechados automaticamente** pelo
   orquestrador quando todas as Tasks-filhas conhecidas estiverem
   fechadas/mergeadas (mesma lógica do que já foi feito manualmente para
   #60 nesta sessão).
6. **Commits da DeepSeek precisam ser atômicos e descritivos.** O prompt
   de sistema de `deepseek-agent.mjs` passa a exigir um commit por item
   concluído do `task.md` (não um commit único no final), com mensagem no
   formato `<tipo>(<escopo>): <o quê>` + corpo curto do porquê quando não
   for óbvio — é o que permite ao Claude, na revisão final única sobre até
   14 PRs já mergeados, reverter ou corrigir um item pontual via
   `git revert <sha>` sem precisar desfazer o PR inteiro.
7. **Claude revisa só uma vez, no final.** Nenhuma sessão Claude roda
   `dev-review` por PR nesta leva — isso é papel de `deepseek-review.mjs`
   agora (item 1). Quando as 14 issues estiverem todas mergeadas ou
   bloqueadas-com-issue, uma única sessão Claude roda `/code-review` (ou
   `dev-review` estendido) sobre o intervalo de commits/PRs da leva
   inteira, com poder de reverter/corrigir diretamente em `main` — não
   por PR individual.

## Consequências

**Positivas**: custo de Claude cai ainda mais (revisão deixa de ser por
PR); múltiplas Tasks independentes avançam na mesma janela em vez de uma
por hora; nenhuma Task fica esperando disponibilidade de sessão Claude
para revisão ou para decisão de bloqueio simples (vira issue e segue).

**Negativas / riscos aceitos**:
- Autorrevisão pela própria DeepSeek tem menos independência de julgamento
  que uma revisão por um modelo diferente (mesmo modelo que implementou
  avaliando o próprio código) — mitigado por manter os checks mecânicos
  determinísticos (gate de CI) como a barreira dura, e pela revisão final
  do Claude cobrir as 14 de uma vez como rede de segurança.
- Revisão do Claude acontecer só no final, depois de já mergeado, significa
  que um problema real fica em `main` por mais tempo antes de ser pego —
  aceito explicitamente pelo mantenedor; mitigado pelo requisito de commits
  atômicos (item 6), que barateia a reversão pontual.
- Issues abertas automaticamente por bloqueio podem se acumular sem
  triagem se ninguém olhar o board por um tempo — aceito, mesmo padrão de
  risco que já existia pra bloqueios manuais (ex: #71).

## Alternativas consideradas

- **Manter `dev-review` por Claude por PR, só paralelizar implementação** —
  não atende o pedido explícito de reduzir Claude ao mínimo nesta leva;
  descartada.
- **Deixar bloqueio pausado esperando Etapa B decidir** (comportamento
  atual do ADR-0003) — descartada porque o mantenedor pediu explicitamente
  "não interromper"; issue automática + seguir em frente atende melhor.
