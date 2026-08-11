# Fluxo autônomo de feature (ideia → merge → relatório)

Este documento descreve o pipeline que transforma um pedido informal
("quero implementar login") em um PR mergeado em `main`, com card(s) do
GitHub, testes, revisão e um relatório final — sem pausas para confirmação
manual. É executado pela skill `.claude/skills/feature-flow/SKILL.md`, que
implementa passo a passo o que está descrito aqui.

## Quando se aplica

Qualquer pedido do tipo "quero implementar/criar/adicionar X" para uma
funcionalidade ainda não coberta por uma issue existente. Não se aplica a
tarefas pontuais (ex: "corrige esse typo", "roda os testes") — essas seguem
o fluxo normal de conversa, sem passar pelo pipeline completo.

## Autonomia: sem pausas

Por decisão explícita do mantenedor, este fluxo roda do início ao fim **sem
pedir confirmação em nenhum ponto** — inclusive merge em `main` e postagem de
comentários de revisão no GitHub. Isso é uma exceção deliberada ao
comportamento padrão de `dev-review` e `qa-review` (que, fora deste fluxo,
sempre pedem confirmação antes de postar via `gh`): quando essas skills são
invocadas *a partir* deste pipeline, pulam o passo de confirmação. Quando
invocadas diretamente pelo usuário fora do fluxo, continuam pedindo
confirmação normalmente.

A única interação humana esperada é responder às perguntas geradas na Fase 2
— isso é reflexão de produto, não uma aprovação de ação arriscada.

Essa autonomia depende de `.claude/settings.json` (versionado, na raiz do
repo) liberar `Skill(feature-flow)`, `Skill(dev-review)`, `Skill(qa-review)`,
`Skill(artifact-design)`, `Agent`, `Artifact` e `PushNotification` — sem
essas entradas o fluxo para pedindo permissão no meio de uma rodada, mesmo
com tudo documentado aqui. Gaps de comando (`Bash`, ex: um subcomando novo de
`dotnet`/`npm`/`gh`) ainda podem aparecer; corrija-os na allowlist do mesmo
arquivo (não em `settings.local.json`, que é pessoal e não versionado) já
que são gaps do processo, não preferência de quem está rodando.

### Execução longa/autônoma (várias horas ou durante a noite)

Não deixe uma sessão só, em primeiro plano, rodando o fluxo por horas sem
nenhum ponto de checagem — se ela travar (permissão faltando, prompt
esperando resposta), ninguém percebe até voltar. Padrão correto: uma sessão
supervisora roda em `/loop` de auto-ritmo (`ScheduleWakeup`, ~30min),
delega a implementação a um agente em background (`Agent`, um por issue,
sequencial se tocarem `Domain`/`Infrastructure` em comum) e a cada
despertar confere o progresso (`ListAgents`, `TaskList`/`TaskGet` se o
agente reportar tarefas) antes de decidir se dorme de novo ou intervém. Isso
dá visibilidade incremental (não só no final) e um ponto natural de
recuperação se algo travar no meio.

## Fase 1 — Issue semente

Se o usuário referenciar uma issue já existente (número, URL, ou "roda o
fluxo nessa issue"), pule a criação: `gh issue view <n>` e use o corpo atual
como ponto de partida da Fase 2, sem duplicar a issue. Caso contrário, ao
identificar o pedido, crie imediatamente uma issue mínima no GitHub:

```bash
gh issue create --repo RafaVargas1/Synclass \
  --title "<Verbo + resultado, ex: Professor faz login>" \
  --body "**História de usuário:** Como <Professor|Aluno>, quero <ação>, para <valor>." \
  --label feature
```

(`--label fix` se for correção, não funcionalidade nova.) Adicione ao board
no projeto `Synclass` (`gh project item-add`, coluna Backlog por padrão — ver
`gh project item-list`/`item-edit` para mover depois). Esta issue é uma
semente: a Fase 2 reescreve o corpo dela por completo antes de qualquer
código ser escrito, e pode gerar issues-irmãs se o escopo se partir em mais
de um card.

## Fase 2 — Loop de reflexão e perguntas (até 3 rodadas)

Isto estende o loop de 3 iterações já definido em
[`padrao-de-issue.md`](../backlog/padrao-de-issue.md#processo-de-escrita-loop-de-3-iterações):
lá o loop é "escreve RN → confere em código → repete"; aqui ele ganha uma
terceira lente e um passo de pergunta ao usuário entre rodadas.

Cada rodada tem 4 passos:

1. **Reflexão de conhecimento geral** — o que esse tipo de funcionalidade
   tipicamente exige, independente deste projeto (ex: login normalmente
   envolve credenciais, sessão/token, e frequentemente recuperação de senha
   e rate limiting).
2. **Reflexão com base no código** — leia o repositório de verdade (camadas
   `Domain`/`Infrastructure`/`Api` no backend, componentes/rotas no
   frontend, migrations existentes) para entender o que já existe e o que
   precisaria mudar. Cite arquivo:linha nas anotações internas da rodada.
3. **Reflexão com regras de negócio anteriores** — releia
   [`requisitos-funcionais.md`](../backlog/requisitos-funcionais.md) e
   issues/RNs já fechadas relacionadas, procurando conceitos que já têm dono
   (ex: um "Aluno provisório" e um "Aluno cadastrado" podem ser conceitos
   diferentes) para não reintroduzir um conceito duplicado ou conflitante.
4. **Perguntas ao usuário** — a partir das 3 reflexões, liste só o que é
   genuinamente ambíguo e não decidível pelas reflexões acima (ex: "login
   deve aceitar e-mail, telefone ou os dois?", "recuperação de senha entra
   neste card ou é um card separado?"). Faça via `AskUserQuestion` quando
   couber em opções de múltipla escolha; pergunta aberta quando não couber.
   Espere a resposta antes de continuar.

Ao final de cada rodada, incorpore as respostas ao rascunho do(s) card(s).
Pare antes de 3 rodadas se uma rodada não mudar nada em relação à anterior
(mesmo critério de "não força a 3ª" do documento de padrão de issue) — 3 é o
teto para casos complexos, não uma meta.

### Saída da Fase 2: card(s) finalizados

Cada card segue a estrutura de
[`padrao-de-issue.md`](../backlog/padrao-de-issue.md#estrutura-do-card)
(título, história de usuário, Regra de Negócio, critérios de aceite Gherkin,
**critérios técnicos**, contexto/protótipo — ver a seção nova
"Critérios técnicos" adicionada lá).

Se a reflexão revelar que o pedido original cobre mais de uma
funcionalidade — ou que uma única funcionalidade é grande/fullstack demais
para um card só (ver heurística de tamanho em
[`padrao-de-issue.md#épico-e-task-features-grandes-ou-fullstack`](../backlog/padrao-de-issue.md#épico-e-task-features-grandes-ou-fullstack))
— quebre em **Épico + Tasks** usando sub-issues nativas do GitHub em vez de
forçar tudo em um card só: a issue semente vira o épico
(`gh issue edit <n> --title "Épico: ..." --add-label epic`) e cada fatia
entregável vira uma Task filha (`gh issue create --parent <n> ...`). Se o
pedido já cabe num card só, a issue semente simplesmente vira esse card
(comportamento anterior, sem mudança).

Reescreva a issue semente (`gh issue edit`) com o resultado final (card
único, ou corpo curto de épico se houve quebra); aplique label de prioridade
(`priority:P0`..`P3`, default `P2` se o usuário não opinar) em cada Task e
mova o(s) card(s) para a coluna certa do board.

## Fase 2.5 — Spec técnica

Só depois do(s) card(s) finalizados no GitHub: para cada Task (ou para o
card único, se não houve quebra em épico), gere a pasta
`docs/specs/<n>-<slug>/{task.md,implementation.md}` descrita em
[`especificacao-tecnica.md`](especificacao-tecnica.md). Não é trabalho novo
de reflexão — é formalizar em arquivo o que a Fase 2 já concluiu (que
entidade muda, contrato de API, ordem de implementação) para virar o roteiro
objetivo da Fase 3, especialmente quando ela é delegada a um agente de
swarm. Commit desses dois arquivos é o primeiro commit da branch da Task
(antes de qualquer teste), `docs(specs): adiciona spec técnica da Task #<n>`.

## Fase 3 — Implementação

- Branch por card: `feature/<escopo-curto>` (ou `fix/...`), conforme
  [`CONTRIBUTING.md`](../../CONTRIBUTING.md#branches-e-worktrees). Primeiro
  commit da branch é a pasta `docs/specs/<n>-<slug>/` gerada na Fase 2.5.
- **TDD estrito**: siga a ordem do `task.md` da spec técnica — para cada item
  da checklist, escreva o teste primeiro (vendo-o falhar), implemente o
  mínimo para passar, then refatore. Não escreva produção sem um
  teste vermelho guiando. Marque o item como concluído no `task.md` a cada
  commit (é o rastro de progresso da Task, mais granular que a coluna do
  board).
- **Logs de dev como guardrail**: rode `backend/scripts/watch.sh` durante o
  desenvolvimento de qualquer mudança de backend e acompanhe o log
  estruturado (JSON com `TrackId`, decodificado por `pretty-log.sh`) enquanto
  exercita o fluxo manualmente ou via teste — use o log real, não só o
  resultado do teste, para confirmar que o comportamento observado bate com
  o esperado pelo card (ex: um evento que devia ser logado e não apareceu é
  sinal de bug antes mesmo do teste apontar).
- **Commits**: Conventional Commits, um commit por mudança coesa com
  build+testes passando naquele commit (`CONTRIBUTING.md#commits`). Não
  acumule um commit gigante no fim.
- **Agentes em swarm — quando compensa**:
  - Se a Fase 2 gerou mais de um card **independente** (sem migration ou
    módulo compartilhado entre eles), rode um agente por card em paralelo,
    cada um em sua própria `git worktree`
    (`CONTRIBUTING.md#branches-e-worktrees). Não paralelize cards que tocam
    a mesma tabela/migration ou o mesmo componente — o custo de resolver
    conflito supera o ganho. Tasks de um mesmo Épico (ver
    [`padrao-de-issue.md#épico-e-task-features-grandes-ou-fullstack`](../backlog/padrao-de-issue.md#épico-e-task-features-grandes-ou-fullstack))
    seguem essa mesma regra — não ganham swarm automático só por
    pertencerem ao mesmo épico; o padrão para elas é rodar espaçadas, uma
    execução do fluxo por Task.
  - Dentro de um card que toca backend e frontend: implemente o backend
    primeiro até o contrato da API (rotas, DTOs) estabilizar; só então vale
    paralelizar — um agente fecha os testes/edge cases restantes do backend
    enquanto outro implementa o frontend contra o contrato já definido, em
    worktrees separadas.
  - Regra de bolso: só vale abrir agente(s) extra(s) quando a fatia de
    trabalho é grande o bastante (ordem de 20+ minutos de trabalho
    sequencial) e genuinamente isolada. Um CRUD pequeno de um card só roda
    sequencial, num único agente — swarm nesse caso custa mais em tokens e
    coordenação do que economiza em tempo.
  - Ao delegar, passe o caminho de `docs/specs/<n>-<slug>/` no prompt do
    agente em vez de reexplicar o desenho técnico inline — o agente lê
    `task.md`/`implementation.md` como fonte única de verdade.
- Antes de abrir o PR, rode os checks mecânicos de
  `CONTRIBUTING.md#antes-de-abrir-um-pr` (`dotnet format && dotnet test` /
  `npm run lint && npm run typecheck && npm test`) e só abra o PR
  (`gh pr create --body "Closes #N"`) se estiverem verdes.

## Fase 4 — Revisão (até 3 rodadas)

Cada rodada:

1. Rode `dev-review` e `qa-review` **em paralelo** (duas invocações
   independentes — uma é checklist mecânico sobre o diff, a outra é
   navegador real via Playwright; não têm dependência entre si) sobre o PR
   aberto na Fase 3.
2. Corrija tudo que voltou como bloqueante/falhou, um commit por correção
   coerente.
3. Se a rodada não encontrar nenhum achado bloqueante, pare — não force as 3
   rodadas.
4. Nas rodadas 1 e 2, **não** poste comentário no GitHub — os achados são
   usados internamente para corrigir. Só a rodada final consolida um
   comentário de revisão postado no PR (`gh pr comment`), já que este fluxo
   roda sem pausa de confirmação (ver "Autonomia: sem pausas" acima).
5. Se o teto de 3 rodadas for atingido e ainda sobrar algum achado de
   severidade baixa (não bloqueante), registre-o no relatório final (Fase 6)
   como débito conhecido em vez de travar o fluxo indefinidamente.

Os prints, checagens de acessibilidade/responsividade e resultados Gherkin
da **rodada final** de `qa-review` são a "comprovação de qualidade do fluxo"
usada no relatório da Fase 6 — preserve o diretório de screenshots gerado
(`frontend/e2e/.qa-review/screenshots/<slug-do-pr>/`) até compor o relatório,
mesmo que o `.spec.ts` seja apagado ao final da skill.

## Fase 5 — Merge

Com CI verde e a Fase 4 concluída, faça squash e merge sem esperar
confirmação:

```bash
gh pr merge <n> --squash --delete-branch
```

Mova o(s) card(s) para a coluna "Concluído" no board
(`gh project item-edit`).

## Fase 6 — Relatório final

Compile um relatório único cobrindo: link da issue original (e do épico e
das Tasks-irmãs, se houve quebra), link do PR mergeado, resumo do que foi
implementado, veredito da rodada final de `dev-review`, tabela de critérios
Gherkin x resultado da rodada final de `qa-review`, e os screenshots dessa
rodada (embutidos como `data:` URI, já que o relatório é publicado como
Artifact autocontido).

Publique com a ferramenta `Artifact` (carregando a skill `artifact-design`
antes de escrever o HTML) e envie uma notificação (`PushNotification`) com o
link — não há envio de e-mail automatizado neste ambiente (sem SMTP
configurado), essa é a alternativa combinada com o mantenedor. Depois de
publicar, apague os artefatos temporários (`.spec.ts` e screenshots locais)
se ainda não tiverem sido removidos pela `qa-review`.

## Divisão de modelo/agente por fase

| Fase | Quem executa | Por quê |
|---|---|---|
| 1 (issue semente) | Chamada direta de `gh`, sem modelo | Não há raciocínio nenhum aqui — é mecânico. |
| 2, rodada 1 de cada card (rascunho de RN a partir da história) | Subagente leve (Haiku), few-shot com o exemplo de `padrao-de-issue.md` | Segue um padrão já estabelecido — não precisa do modelo principal (mesma lógica já documentada em [`padrao-de-issue.md`](../backlog/padrao-de-issue.md#divisão-de-esforço-por-modeloagente)). |
| 2, reflexão de código/RN anteriores, polimento final, perguntas ao usuário | Modelo principal da sessão | É onde aparecem ligações não óbvias entre cards e julgamento sobre o que perguntar — não delega bem. |
| 2.5 (spec técnica: `task.md`/`implementation.md`) | Modelo principal | É a formalização em arquivo da mesma reflexão da fase 2 — mesmo julgamento, não delega a modelo leve. |
| 3 (implementação, thread sequencial principal) | Modelo principal | Correção de código tem custo de erro mais alto que rascunho de issue; não usar modelo leve aqui. |
| 3 (agentes de swarm, quando compensa) | Mesmo tier do modelo principal (não Haiku) | Swarm aqui é sobre paralelizar, não sobre baratear — a fatia de código de cada agente precisa do mesmo nível de julgamento do restante da implementação. |
| 3 (buscas pontuais de arquivo/padrão antes de implementar) | Agente `Explore` | Busca é mais barata como agente somente-leitura dedicado. |
| 4 (dev-review, qa-review) | Uma invocação de skill cada, em paralelo | Já são skills prontas com seus próprios passos; não reimplementar. |
| 6 (relatório) | Modelo principal | Síntese final, precisa juntar contexto de todas as fases anteriores. |

## Board e labels usados

Board `Synclass` (projeto GitHub, `gh project list`), colunas Backlog → Em
Desenvolvimento → Em Teste → Concluído. Labels: `feature`/`fix`/`epic`
(tipo), `priority:P0`..`priority:P3` (prioridade) — todas já existem no
repositório, não recrie. Progresso de um épico é lido direto do campo nativo
"Sub-issues progress" do Project, não de uma checklist manual.
