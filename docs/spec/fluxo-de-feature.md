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

Este padrão é o default para **toda** execução do fluxo, não só as que já
nascem pensadas para durar horas — a Fase 3 (implementação) e a Fase 4
(revisão) sozinhas já produzem output bruto (`dotnet test`/`npm test`,
docker, Playwright) grande o bastante para estourar o contexto de uma
sessão única muito antes de chegar na Fase 6. Delegar implementação e
revisão a `Agent`s separados (ver Fase 3 e Fase 4 abaixo) não é só sobre
paralelismo — é o que mantém a sessão supervisora com contexto plano ao
longo das 6 fases, recebendo só o resultado compacto de cada uma.

## Níveis de rigor

Nem toda Task exige o mesmo overhead de processo — ver
[ADR-0001](decisions/ADR-0001-pipeline-claude-deepseek.md). Três níveis:

**Trivial**: `Issue → DeepSeek (harness) → Quality Gates`. Sem
`implementation.md`, sem rodada de aprovação de plano, `dev-review` ainda
roda (não é dispensado, só o plano prévio é).

**Média**: `Issue → DeepSeek rascunha implementation.md → Claude revisa
(APPROVED/CHANGES_REQUESTED) → DeepSeek implementa (harness) → Quality
Gates → Claude revisão final (dev-review)`.

**Complexa/crítica**: `Issue → DeepSeek rascunha implementation.md →
Claude revisão de arquitetura → DeepSeek implementa (harness) → Quality
Gates → DeepSeek depura falhas → Claude revisão final (dev-review) →
DeepSeek corrige → Quality Gates`.

Trate como complexa/crítica qualquer Task que envolva: autenticação/
autorização, pagamento, dado sensível, migration, mudança estrutural de
banco, API pública, concorrência, ou mudança grande em código legado. Na
dúvida entre média e complexa, trate como complexa — o custo de uma
rodada extra de revisão é bem menor que o custo de um bug nessas áreas.

Ver [`especificacao-tecnica.md#quem-escreve-e-a-aprovação-do-plano-adr-0001`](especificacao-tecnica.md#quem-escreve-e-a-aprovação-do-plano-adr-0001)
para o detalhe de quem escreve/aprova cada artefato em cada nível.

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

## Issue como contrato

A partir daqui (Fase 2.5 em diante), a issue do GitHub é o **contrato
funcional** da Task — vale para quem quer que implemente, DeepSeek
incluída. Isso significa: não expandir escopo por iniciativa própria, não
fazer refatoração não relacionada, não alterar regra de negócio sem
justificativa registrada, não mudar arquitetura sem necessidade, não
ignorar critério de aceite. Uma inconsistência ou requisito ambíguo
encontrado durante a implementação vira uma seção
`## Inconsistências encontradas` no `task.md` daquela Task — registrada
antes de continuar, resolvida pelo Claude (não decidida pela DeepSeek
sozinha), como qualquer outra decisão de produto (mesmo espírito da Fase
2, "não adivinhe" — ver `AGENTS.md#antes-de-modificar-código`).

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

Desde [ADR-0001](decisions/ADR-0001-pipeline-claude-deepseek.md), quem
implementa é a **DeepSeek**, via `scripts/deepseek-agent.mjs` — não o
modelo principal da sessão. O agente `synclass-worker` (Claude, Sonnet)
**orquestra**: cria a worktree, dispara o harness, monitora o resultado,
roda o gate de CI, e só implementa diretamente como fallback explícito
(harness esgotou as tentativas, ou a Task foi marcada complexa demais pro
harness sozinho).

- Branch por card: `feature/<escopo-curto>` (ou `fix/...`), conforme
  [`CONTRIBUTING.md`](../../CONTRIBUTING.md#branches-e-worktrees). Primeiro
  commit da branch é a pasta `docs/specs/<n>-<slug>/` gerada na Fase 2.5.
- **Disparo do harness**:
  ```bash
  node scripts/deepseek-agent.mjs --task docs/specs/<n>-<slug>/task.md \
    --system docs/spec/code-style.md,docs/spec/business-rules.md,docs/spec/security-rules.md,docs/spec/testing-standards.md,docs/spec/ux-heuristics.md,docs/spec/engenharia-de-qualidade.md
  ```
  O harness segue **TDD estrito** por conta própria (mesma ordem do
  `task.md`: teste primeiro, vendo-o falhar, implementação mínima,
  refatora, marca o item, commit — orientado pelo Quality Contract passado
  em `--system`) e usa testes escopados durante o loop
  (`docs/spec/testing-standards.md#testes-escopados-durante-o-loop-suíte-completa-antes-do-pr`).
  `synclass-worker` acompanha a saída e o `deepseek-run.log` gerado ao lado
  do `task.md`.
- **Ambiguidade/inconsistência**: se o harness parar com uma seção
  `## Inconsistências encontradas` no `task.md` (em vez de concluir), não
  é a DeepSeek quem decide — Claude resolve a ambiguidade (pode envolver
  `AskUserQuestion` se for decisão de produto) e só então o harness roda
  de novo a partir dali.
- **Logs de dev como guardrail**: para mudanças de backend, confira o log
  estruturado (JSON com `TrackId`, `backend/scripts/watch.sh` +
  `pretty-log.sh`) depois que o harness terminar — o comportamento
  observado precisa bater com o esperado pelo card, não só o teste passar.
- **Commits**: Conventional Commits, um commit por mudança coesa com
  build+testes passando naquele commit (`CONTRIBUTING.md#commits`) — o
  harness já commita a cada item do `task.md`; não acumule um commit
  gigante por cima disso.
- **Tasks independentes em paralelo**: mesmo critério de antes — Tasks sem
  migration/módulo compartilhado entre si rodam em worktrees separadas,
  cada uma com seu próprio disparo do harness (não paralelize as que tocam
  a mesma tabela/migration ou o mesmo componente). Tasks de um mesmo Épico
  seguem essa mesma regra — não ganham paralelismo automático só por
  pertencerem ao mesmo épico.
- Antes de abrir o PR, `synclass-worker` roda os checks mecânicos de
  `CONTRIBUTING.md#antes-de-abrir-um-pr` (`dotnet format && dotnet test` /
  `npm run lint && npm run typecheck && npm test` — suíte **completa**,
  diferente dos testes escopados do harness) e só abre o PR
  (`gh pr create --body "Closes #N"`) se estiverem verdes.

## Fase 4 — Revisão (até 3 rodadas)

Desde [ADR-0001](decisions/ADR-0001-pipeline-claude-deepseek.md), **só
`dev-review` roda por padrão** neste fluxo automático — a rodada completa de
`qa-review` (Playwright/UX no navegador, roteiro Gherkin) sai do par de
agentes paralelos e só entra sob pedido explícito do usuário ("testar o PR",
"fazer QA do PR #N"), fora deste pipeline. Isso **não** inclui a verificação
visual: `dev-review` (Passo 3.5 da própria skill, ver
`.claude/skills/dev-review/SKILL.md`) tira e avalia screenshots
mobile+desktop de toda rota tocada sempre que o PR mexe em
`frontend/src/app/**`/`frontend/src/components/**` — é obrigatória e
bloqueante, não uma etapa opcional que só `qa-review` cobriria. Cada rodada:

1. Delegue `dev-review` a um `Agent` (`subagent_type: "synclass-worker"`)
   sobre o PR aberto na Fase 3, em vez de chamar `Skill()` direto na
   sessão supervisora: a skill gera output bruto (`dotnet test`/`npm
   test`) grande o bastante para estourar o contexto se acumulado por até
   3 rodadas. Avise o agente de que está "rodando como subagente" — a
   skill já sabe responder só com a tabela de achados nesse modo.
2. Corrija tudo que voltou como bloqueante/falhou, um commit por correção
   coerente, na sessão supervisora (que só recebeu a tabela compacta de
   volta, não o log bruto).
3. Se a rodada não encontrar nenhum achado bloqueante, pare — não force as 3
   rodadas.
4. Nas rodadas 1 e 2, **não** poste comentário no GitHub — os achados são
   usados internamente para corrigir. Só a rodada final consolida um
   comentário de revisão postado no PR (`gh pr comment`), já que este fluxo
   roda sem pausa de confirmação (ver "Autonomia: sem pausas" acima).
5. Se o teto de 3 rodadas for atingido e ainda sobrar algum achado de
   severidade baixa (não bloqueante), registre-o no relatório final (Fase 6)
   como débito conhecido em vez de travar o fluxo indefinidamente.

Se a Task tocou UI e alguém pedir `qa-review` depois (fora deste fluxo),
os prints/checagens de acessibilidade/responsividade/Gherkin dessa rodada
avulsa seguem o mesmo padrão de preservação de evidência descrito na
skill — não há mais uma "rodada final de qa-review" garantida dentro do
fluxo automático para alimentar o relatório da Fase 6.

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
implementado, veredito da rodada final de `dev-review`. Se `qa-review`
rodou nesta Task (sob pedido explícito, fora do padrão da Fase 4), inclua
também a tabela de critérios Gherkin x resultado e os screenshots dessa
rodada (embutidos como `data:` URI, já que o relatório é publicado como
Artifact autocontido) — caso contrário, o relatório segue só com o
veredito de `dev-review`.

Publique com a ferramenta `Artifact` (carregando a skill `artifact-design`
antes de escrever o HTML) e envie uma notificação (`PushNotification`) com o
link — não há envio de e-mail automatizado neste ambiente (sem SMTP
configurado), essa é a alternativa combinada com o mantenedor. Depois de
publicar, apague os artefatos temporários (`.spec.ts` e screenshots locais)
se ainda não tiverem sido removidos pela `qa-review`.

## Divisão de modelo/agente por fase

Atualizada por [ADR-0001](decisions/ADR-0001-pipeline-claude-deepseek.md)
— substitui a versão anterior, que pinava a Fase 3 no modelo principal.

| Fase | Quem executa | Por quê |
|---|---|---|
| 1 (issue semente) | Chamada direta de `gh`, sem modelo | Não há raciocínio nenhum aqui — é mecânico. |
| 2, rodada 1 de cada card (rascunho de RN a partir da história) | Subagente leve (Haiku), few-shot com o exemplo de `padrao-de-issue.md` | Segue um padrão já estabelecido — não precisa do modelo principal (mesma lógica já documentada em [`padrao-de-issue.md`](../backlog/padrao-de-issue.md#divisão-de-esforço-por-modeloagente)). |
| 2, reflexão de código/RN anteriores, polimento final, perguntas ao usuário | Modelo principal da sessão (Claude) | É onde aparecem ligações não óbvias entre cards e julgamento sobre o que perguntar — não delega bem. |
| 2.5, rascunho de `task.md`/`implementation.md` (média/complexa) | DeepSeek (`scripts/deepseek-call.sh`, chamada única de texto, não o harness) | Formalizar em arquivo o que a Fase 2 já decidiu é um passo mais mecânico que a reflexão em si — cabe delegar; fica mais barato que redigir no modelo principal. |
| 2.5, aprovação do plano (média/complexa) e escrita direta (trivial) | Claude (`APPROVED`/`CHANGES_REQUESTED`, ver `especificacao-tecnica.md`) | Julgamento sobre se o plano está certo antes de gastar uma implementação inteira em cima dele — não delega. |
| 3 (implementação, TDD completo) | **DeepSeek**, via `scripts/deepseek-agent.mjs` (harness com tool-calling — `read_file`/`write_file`/`list_dir`/`run_command`), disparado pelo agente `synclass-worker` (Claude, Sonnet, papel de orquestrador/fallback) | Objetivo explícito do mantenedor (ADR-0001): reduzir gasto de Claude no longo prazo tirando-o do loop de iteração de TDD, que é a fase mais longa/repetitiva. O harness roda fora do contexto da sessão supervisora — `synclass-worker` só dispara e monitora, não implementa linha a linha, exceto como fallback explícito. |
| 3 (buscas pontuais de arquivo/padrão antes de implementar) | Agente `Explore` | Busca é mais barata como agente somente-leitura dedicado. |
| 4 (dev-review) | Uma invocação de `Agent` (`synclass-worker`, fixo em Sonnet) delegando a skill | Já é skill pronta com seus próprios passos; não reimplementar — mas rodar via `Agent` (não `Skill()` direto) mantém o output bruto (`dotnet test`/`npm test`) fora do contexto da sessão supervisora, e o pin em Sonnet garante julgamento suficiente pra não deixar passar achado bloqueante. `qa-review` não entra por padrão aqui (ver Fase 4). |
| 6 (relatório) | Modelo principal (Claude) | Síntese final, precisa juntar contexto de todas as fases anteriores. |

## Board e labels usados

Board `Synclass` (projeto GitHub, `gh project list`), colunas Backlog → Em
Desenvolvimento → Em Teste → Concluído. Labels: `feature`/`fix`/`epic`
(tipo), `priority:P0`..`priority:P3` (prioridade) — todas já existem no
repositório, não recrie. Progresso de um épico é lido direto do campo nativo
"Sub-issues progress" do Project, não de uma checklist manual.
