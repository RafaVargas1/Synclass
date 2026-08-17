---
name: feature-flow
description: Pipeline autônomo ponta a ponta do projeto Synclass — da ideia (ou de uma issue já existente) ao merge e relatório final — que dispara quando o usuário expressa a intenção de implementar algo novo ("quero implementar login", "quero criar cadastro de horário", "quero adicionar recuperação de senha", "implementa X pra mim", "bota isso pra rodar do jeito que a gente combinou") ou pede para retomar o fluxo numa issue que já existe ("roda o fluxo completo pra issue #12", "continua esse fluxo na issue https://github.com/.../issues/12", "roda o feature-flow nessa issue"). Se uma issue for referenciada, reaproveita ela em vez de criar uma nova. Cria a issue no GitHub (quando ainda não existe), reflete em até 3 rodadas (conhecimento geral + código + regras de negócio anteriores) perguntando ao usuário o que for ambíguo, gera 1+ cards seguindo `docs/backlog/padrao-de-issue.md` — quebrando em Épico + Tasks (sub-issues nativas do GitHub) quando o pedido é grande/fullstack demais para um card só —, formaliza a reflexão técnica de cada Task em `docs/specs/<n>-<slug>/{task.md,implementation.md}` (`docs/spec/especificacao-tecnica.md`), implementa com TDD e commits precisos usando os logs de dev como guardrail, revisa em até 3 rodadas com `dev-review` + `qa-review` em paralelo, faz squash-merge em `main` e publica um relatório final (Artifact + notificação) com o card, o PR e os prints da última rodada de QA. Roda sem pausas de confirmação (autonomia total, decisão explícita do mantenedor) — não usar para tarefas pontuais que não envolvem uma feature/fix nova.
---

# feature-flow

Esta skill é o executor do processo descrito em
[`docs/spec/fluxo-de-feature.md`](../../../docs/spec/fluxo-de-feature.md) —
leia esse documento primeiro se ainda não estiver carregado no contexto, ele
é a fonte de verdade para os critérios de cada fase. Este arquivo é o roteiro
operacional (que comando rodar, em que ordem, com qual ferramenta).

**Autonomia**: esta skill roda do início ao fim sem pedir confirmação —
inclusive `gh pr merge` e comentários postados via `gh` — exceto pelas
perguntas da Fase 2, que são o próprio mecanismo de alinhamento com o
usuário, não uma aprovação de ação arriscada. Ao invocar `dev-review` e
`qa-review` a partir daqui, pule o passo de confirmação delas antes de
`gh pr comment`/`gh issue comment` — essa exceção só vale dentro deste fluxo.

Isso só funciona sem interrupção se `.claude/settings.json` já liberar as
skills/tools que o pipeline usa (ver
[`fluxo-de-feature.md#autonomia-sem-pausas`](../../../docs/spec/fluxo-de-feature.md#autonomia-sem-pausas))
— se um prompt de permissão aparecer no meio de uma rodada, é sinal de gap
na allowlist, não de uma ação que devesse pedir confirmação; corrija o
`settings.json` em vez de aprovar item a item. Siga o padrão de
["Execução longa/autônoma"](../../../docs/spec/fluxo-de-feature.md#execução-longaautônoma-várias-horas-ou-durante-a-noite)
do mesmo documento em toda execução deste fluxo, não só nas que você já
espera que durem horas — supervisor em `/loop`, implementação/revisão
delegadas a `Agent`, nunca uma sessão solta acumulando o contexto de todas
as 6 fases sem checkpoint.

## Passo 1 — Issue semente

**Primeiro, verifique se o usuário já referenciou uma issue existente** —
número (`#12`, `issue 12`), URL do GitHub, ou algo como "continua o fluxo pra
issue tal"/"roda o fluxo completo nessa issue". Se sim:

```bash
gh issue view <n> --json title,body,number,labels,state
```

Use o título e corpo atuais como ponto de partida da Fase 2 (Passo 2) — **não
rode `gh issue create`**, a issue já existe. Se a issue estiver fechada ou já
tiver um PR mergeado associado, pare e avise o usuário em vez de reabrir ou
duplicar trabalho.

Se não houver issue referenciada, crie uma semente nova: extraia da fala do
usuário um título direto (verbo + resultado) e a história de usuário
(`Como <Professor|Aluno>, quero <ação>, para <valor>`; pergunte qual papel se
não estiver claro).

```bash
gh issue create --repo RafaVargas1/Synclass --title "<título>" \
  --body "**História de usuário:** Como <papel>, quero <ação>, para <valor>." \
  --label feature   # ou --label fix
gh project item-add 1 --owner RafaVargas1 --url <url-da-issue-criada>
```

Nos dois casos, guarde o número da issue — é referenciado no PR (`Closes #N`)
e no relatório final.

## Passo 2 — Reflexão (até 3 rodadas)

Para cada rodada (pare mais cedo se nada mudar da rodada anterior):

1. Rascunhe a Regra de Negócio a partir da história de usuário. Se for a
   1ª rodada e não houver ambiguidade de domínio ainda a resolver, dá para
   delegar este rascunho a um subagente leve (`Agent`, `model: "haiku"`),
   passando como referência `docs/backlog/padrao-de-issue.md` inteiro (ele
   já tem um card de exemplo para few-shot). Rodadas seguintes e qualquer
   ajuste que exija ligar este card a outro (ex: papéis acumuláveis,
   conceitos que já existem em `requisitos-funcionais.md`) ficam com você
   mesmo — não delegue julgamento, só o primeiro rascunho mecânico.
2. Releia os arquivos relevantes do repositório (`backend/src/Synclass.Domain`,
   `Infrastructure`, `Api`, componentes/rotas do `frontend/`) e ajuste a RN
   com o que o código real permite ou exige. Use o agente `Explore` para
   localizar rapidamente onde um conceito já vive, em vez de varrer o repo
   manualmente.
3. Releia `docs/backlog/requisitos-funcionais.md` e issues fechadas
   relacionadas (`gh issue list --state closed --search "<termo>"`) atrás de
   conceitos que já têm dono ou regra prévia.
4. Liste as perguntas que sobraram — só o que as 3 reflexões acima não
   resolvem — e use `AskUserQuestion` (ou pergunta aberta em texto, se não
   couber em opções). Espere a resposta antes de fechar a rodada.

Ao final: escreva o card completo (as 6 seções de
`docs/backlog/padrao-de-issue.md`, incluindo **Critérios técnicos**). Se o
escopo se partir em mais de uma funcionalidade — ou uma única funcionalidade
for grande/fullstack demais para um card só (heurística em
`docs/backlog/padrao-de-issue.md#épico-e-task-features-grandes-ou-fullstack`)
— transforme a issue semente em **Épico** e crie uma **Task** filha por
fatia entregável, usando sub-issue nativa (não menção `#N`):

```bash
# só se houve quebra em mais de uma Task:
gh issue edit <n> --title "Épico: <resultado amplo>" --add-label epic
gh issue create --repo RafaVargas1/Synclass --parent <n> \
  --title "<título da Task>" --body "<corpo com as 6 seções>" --label feature
gh issue edit <task-n> --add-label "priority:P2"   # ajuste conforme a Task

# se não houve quebra, a issue semente vira o card único:
gh issue edit <n> --title "<título final>" --body "<corpo com as 6 seções>"
gh issue edit <n> --add-label "priority:P2"
```

Mova o(s) card(s) no board para "Backlog" (já devem estar lá) ou
"Em Desenvolvimento" ao iniciar a Fase 3 (`gh project item-edit`).

## Passo 2.5 — Spec técnica

Para cada Task (ou o card único, se não houve quebra em épico), gere
`docs/specs/<n>-<slug>/{task.md,implementation.md}` seguindo
`docs/spec/especificacao-tecnica.md` — formaliza em arquivo a reflexão de
código já feita no Passo 2 (entidades afetadas, contrato de API, ordem de
implementação), servindo de roteiro objetivo para o Passo 3 e para qualquer
agente de swarm delegado. Commit inicial da branch, antes de qualquer teste.

## Passo 3 — Implementação

1. Branch: `git worktree add ../synclass-<escopo> feature/<escopo-curto>`
   (ou `fix/<escopo-curto>`), seguindo `CONTRIBUTING.md`. Primeiro commit é a
   pasta de spec técnica do Passo 2.5.
2. TDD seguindo a ordem do `task.md`: escreva o teste do item, veja falhar,
   implemente o mínimo, refatore, commit (`tipo(escopo): descrição no
   imperativo`), marque o item como concluído no `task.md`.
3. Durante mudanças de backend, mantenha `backend/scripts/watch.sh` rodando
   em background e observe o log estruturado (`pretty-log.sh`) para
   confirmar que o comportamento e os eventos logados batem com os
   Critérios técnicos do card antes de considerar o teste suficiente.
4. Avalie swarm (ver `fluxo-de-feature.md#fase-3--implementação` para os
   critérios exatos de quando compensa): cards/Tasks independentes → um
   `Agent` (`subagent_type: "synclass-worker"`) por card, cada um em sua
   worktree, com o caminho de `docs/specs/<n>-<slug>/` no prompt em vez do
   desenho técnico reexplicado inline — o toolset mais estreito desse
   agente (sem `Agent`/`Artifact`) evita pagar overhead de ferramentas que
   implementação nunca usa, em cada uma das execuções paralelas; dentro de um card back+front → backend primeiro até o contrato
   estabilizar, depois paralelize frontend contra esse contrato. Não abra
   agente extra para trabalho pequeno ou acoplado — e não trate "mesmo
   épico" como sinal de independência: Tasks de um épico rodam espaçadas
   (uma execução do fluxo por Task) por padrão, swarm só quando já
   satisfazem o critério normal de independência. **Exceção que não conta
   como independência**: se dois cards em swarm mexem em
   `SynclassDbContext.cs`, `Program.cs` (registro de DI) ou geram migration
   nova, os `ModelSnapshot.cs` de cada worktree vão divergir do outro e do
   `main` — conflito garantido no merge, e caro de resolver manualmente
   (arquivo gerado, não dá pra pegar um lado só). Nesse caso, sempre que um
   dos cards do swarm mergear em `main`, rebase os worktrees dos outros
   antes de continuar a revisão deles — resolver o conflito ali, com o
   card ainda em progresso e o contexto fresco, é muito mais barato que
   resolver depois de dev-review/qa-review já terem rodado sobre código que
   vai mudar de novo no merge.
5. Antes do PR, rode os checks de `CONTRIBUTING.md#antes-de-abrir-um-pr`
   (`dotnet format && dotnet test`, `npm run lint && npm run typecheck &&
   npm test`). Só prossiga com tudo verde.
6. Abra o PR:

```bash
gh pr create --title "<título>" --body "Closes #<n>

<resumo do que foi implementado>"
```

## Passo 4 — Revisão (até 3 rodadas)

Cada rodada, em paralelo (duas chamadas independentes na mesma resposta),
delegue para um `Agent` (`subagent_type: "synclass-worker"`) — não chame
`Skill()` direto neste passo: `dev-review`/`qa-review` produzem muito output
bruto (docker, `dotnet test`/`npm test`, traces do Playwright) que você não
quer acumulando na sua própria janela de contexto ao longo de até 3 rodadas.
Deixe explícito em cada prompt que o agente está "rodando como subagente" —
as duas skills já reduzem a resposta a só a tabela quando avisadas disso
(ver Passo 6 de `dev-review` e Passo 8 de `qa-review`):

- Agent 1: "Você está rodando como subagente. Invoque a skill `dev-review`
  sobre o PR #<n> e retorne só a tabela de achados (Passo 6 da skill), sem
  prosa adicional."
- Agent 2: "Você está rodando como subagente. Invoque a skill `qa-review`
  sobre o PR #<n> e retorne só a tabela de critérios + seção de UX (Passo 8
  da skill), sem prosa adicional."

Corrija tudo que voltar bloqueante/falhou, um commit por correção. Como
autonomia total já está decidida (não há confirmação a pausar), aplique a
correção na mesma sessão/contexto que acabou de receber o achado pronto —
nunca abra uma rodada de revisão e uma rodada de correção como dois `Agent`
pesados e independentes para o mesmo achado: o segundo reconstrói do zero
(releitura de arquivos, diff, code-style.md) um contexto que o primeiro já
tinha na mão, e isso é o maior desperdício de token do pipeline. Pare a
rotação assim que uma rodada não encontrar mais nada bloqueante (não force
até 3). Nas rodadas 1–2 não poste nada no GitHub — os achados guiam correção
interna. Na rodada final (ou na 3ª, o que vier primeiro), consolide os dois
relatórios (que já voltaram compactos dos agentes) e poste um único
`gh pr comment <n>` com o veredito — sem pedir confirmação (exceção de
autonomia desta skill). Preserve
`frontend/e2e/.qa-review/screenshots/<slug-do-pr>/` da rodada final: é a
evidência usada no relatório do Passo 6.

## Passo 5 — Merge

```bash
gh pr merge <n> --squash --delete-branch
gh project item-edit --id <item-id> --field-id <status-field-id> --single-select-option-id <concluído-option-id>
```

(Resolva os ids de `gh project field-list`/`item-list` se não estiverem à
mão.) Remova a worktree usada (`git worktree remove ...`) se a branch já foi
deletada no merge.

## Passo 6 — Relatório final

Monte um HTML com: link da issue (e issues-irmãs), link do PR mergeado,
resumo do que foi implementado, veredito final de `dev-review`, tabela
critério Gherkin × resultado da rodada final de `qa-review`, e os
screenshots dessa rodada embutidos como `data:` URI (o Artifact precisa ser
autocontido). **Carregue a skill `artifact-design` antes de escrever o
HTML.** Publique com `Artifact` e mande `PushNotification` com o link e um
resumo de uma linha. Depois de publicar, apague os artefatos temporários
(`frontend/e2e/.qa-review/**`) que ainda restarem no disco.
