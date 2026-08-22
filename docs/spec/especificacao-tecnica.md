# Especificação técnica por Task (`docs/specs/`)

Este documento define a pasta de especificação técnica que acompanha cada
Task (card de implementação, ver
[`padrao-de-issue.md`](../backlog/padrao-de-issue.md)) — a camada que fica
entre o card do GitHub (produto/UX) e o código.

## Por que existe

O card no GitHub segue o princípio "UX antes de implementação"
([`padrao-de-issue.md`](../backlog/padrao-de-issue.md#princípio-ux-antes-de-implementação)):
descreve o que o Professor/Aluno consegue fazer, não como o código está
organizado. Isso é certo para o card, mas deixa uma lacuna — a Fase 2 do
[fluxo de feature](fluxo-de-feature.md#fase-2--loop-de-reflexão-e-perguntas-até-3-rodadas)
já faz a reflexão técnica (que entidade muda, que camada é afetada, em que
ordem implementar) para escrever os **Critérios técnicos** do card, mas essa
reflexão fica só na conversa — não sobra nenhum artefato persistido e
consultável quando a implementação começa, especialmente quando ela é feita
por um agente em background (swarm, ver
[`fluxo-de-feature.md#fase-3--implementação`](fluxo-de-feature.md#fase-3--implementação))
que não tem acesso ao raciocínio da sessão principal.

A pasta de spec técnica formaliza essa reflexão em arquivo, na própria
branch/worktree da implementação, para servir de roteiro objetivo — tanto
para quem (ou qual agente) implementa quanto para quem revisa depois.

## Estrutura

Uma pasta por Task, criada na Fase 2.5 do fluxo de feature (depois do card
finalizado no GitHub, antes do primeiro commit de implementação):

```
docs/specs/<número-da-issue>-<slug-curto>/
├── task.md
└── implementation.md
```

`<slug-curto>`: 2-4 palavras do título do card, kebab-case (ex:
`18-login-professor-aluno`).

### `task.md` — checklist de execução TDD

Lista ordenada de tarefas, cada uma mapeando 1:1 para um commit. Nasce
diretamente dos **Critérios de aceite** (Gherkin) e dos **Critérios
técnicos** do card — não introduz informação nova, só ordena e faz a ponte
para "que teste eu escrevo agora":

```markdown
# Task: <título do card> (#<n>)

Card: <link da issue>

## Ordem de execução

- [ ] Teste unidade (Domain): <cenário Gherkin 1>
- [ ] Implementação mínima do cenário 1
- [ ] Teste unidade (Domain): <cenário Gherkin 2>
- [ ] Implementação mínima do cenário 2
- [ ] Migration: <tabela/coluna, se houver>
- [ ] Teste de fumaça (Api): <endpoint>
- [ ] Log estruturado: evento <X> (ver architecture.md#logs-estruturados-e-track-id)
- [ ] Componente frontend: <tela/organism>, contra o contrato já estabilizado
```

A ordem reflete a mesma prioridade já usada na Fase 3 (backend até o
contrato estabilizar, frontend depois) — este arquivo só a torna explícita e
checável.

#### Item de frontend não pode ser vago

`"Componente frontend: <tela/organism>, contra o contrato já estabilizado"`
é o esqueleto do item, nunca o item pronto — quem escreve o `task.md` (Claude
ou DeepSeek rascunhando, Passo "Quem escreve" abaixo) substitui por uma
versão concreta antes de liberar a Task para implementação, porque a
DeepSeek implementa exatamente o que o item diz e não tem contexto de
produto pra preencher a lacuna sozinha (foi assim que um "Componente
frontend: tela do Painel" virou um botão de "Meu perfil" do tamanho errado
numa tela vazia — o item não dizia onde o elemento deveria ficar nem contra
qual critério de UX checar). Um item de frontend concreto tem, no mínimo:

- **Onde exatamente** o elemento fica (dentro de qual componente/slot já
  existente, não só "na tela X") e **o que ele NÃO é** quando isso não for
  óbvio (ex: "ação secundária, mesmo peso visual dos demais itens do menu —
  não um CTA cheio isolado no corpo da tela").
- **Rótulo/copy exato** de qualquer texto novo visível ao usuário — não
  delegar a escolha da palavra pra DeepSeek.
- **Consistência terminológica**: se o item introduz um rótulo/mensagem pra
  um conceito que já aparece em outra tela/mensagem de erro do sistema
  (grep rápido antes de escrever o item), usa o mesmo termo — nunca dois
  nomes para a mesma coisa (ex: enum `Vago` no backend vs. rótulo "Livre" no
  formulário: a mensagem de erro que cita "Vago" pro usuário quebra
  reconhecimento porque ele nunca viu essa palavra em nenhuma tela).
- Referência explícita a `docs/spec/ux-heuristics.md` quando o item envolve
  hierarquia visual, alvo de toque, ou navegação — não basta citar o
  documento nos docs `--system` do harness (Passo "Quem escreve" abaixo),
  o item precisa dizer *qual* regra se aplica aqui.

Se ao escrever o item essas informações não estiverem claras a partir do
card + reflexão da Fase 2, isso é ambiguidade de produto igual qualquer
outra — trate como uma pergunta pendente da Fase 2 (`AskUserQuestion`) em
vez de escrever um item vago e empurrar a decisão pra DeepSeek.

### `implementation.md` — desenho técnico

Prosa curta + bullets, direcionado a quem vai ler o código, não o card. O
princípio geral (ver
[`fluxo-de-feature.md#princípio-especificação-densa-em-vez-de-mais-ciclos`](fluxo-de-feature.md#princípio-especificação-densa-em-vez-de-mais-ciclos)):
**este arquivo carrega o peso da decisão de design, não o loop de
implementação da DeepSeek.** Se ele deixa uma decisão de UI/arquitetura em
aberto, a DeepSeek preenche a lacuna com a própria decisão — imprevisível,
inconsistente entre Tasks, e o que já causou pelo menos duas rodadas
inteiras de retrabalho nesta sessão (menu de navegação, seleção de hora).

- **Entidades/classes afetadas** (novas ou modificadas), com a camada
  (`Domain`/`Infrastructure`/`Api`/componente frontend) e o **caminho de
  arquivo exato** (`frontend/src/components/organisms/MenuNavegacao.tsx`,
  não "o componente de menu").
- **Ponto de inserção exato**: para cada arquivo tocado, cite a
  função/componente que muda e, quando a mudança for de UI/estrutura (não
  só lógica), inclua o trecho de código **antes** (o que existe hoje, via
  leitura real do arquivo — não invente) e o trecho **depois** (a versão
  desejada, escrita por você). Não é obrigatório escrever o diff inteiro
  linha a linha de arquivos grandes, mas qualquer decisão de layout,
  hierarquia visual, ou nome de prop/classe nova precisa aparecer como
  código de exemplo, não como descrição em prosa ("ajuste o estilo para
  ficar mais parecido") — prosa sem código é exatamente o tipo de item
  vago que a seção anterior proíbe.
- **Padrão de estilo a seguir**: cite um arquivo/componente já existente e
  aprovado que resolve um problema visual/estrutural parecido, e diga
  explicitamente "siga o padrão de `<arquivo>:<linha>`" — nunca deixe a
  DeepSeek inventar um padrão novo quando um já existe no repositório (ex:
  "use `ChipSelector.tsx`, não crie um novo componente de chip/aba — a
  tela de valor devido já duplicou esse padrão uma vez, ver achado da
  Task #<n>"). Se não existir um padrão prévio pra seguir, diga isso
  explicitamente ("sem precedente no repo, decisão nova: ...") em vez de
  deixar a ausência de precedente implícita.
- **Contrato de API** (se houver): rota, DTO de entrada/saída — mesmo que
  ainda não implementado, escrito aqui primeiro para o backend e o frontend
  convergirem sem precisar conversar durante o desenvolvimento paralelo
  (ver critério de paralelização em
  [`fluxo-de-feature.md`](fluxo-de-feature.md#fase-3--implementação)).
- **Modelo de dados**: diff conceitual (tabela/coluna nova, relação nova),
  espelhando o que vai virar migration.
- **Edge points** não cobertos por um critério de aceite Gherkin (detalhe de
  implementação, não comportamento visível — mesma definição usada na seção
  "Critérios técnicos" de `padrao-de-issue.md`).
- **Dependência de outras Tasks** do mesmo Epic, se houver (ex: "depende do
  contrato de `POST /sessoes` definido na Task #12") — só quando genuína;
  não repita o Epic inteiro aqui.

Não duplique a Regra de Negócio do card — este arquivo assume que quem lê já
leu o card e está perguntando "como", não "por quê".

## Quem escreve, e a aprovação do plano (ADR-0001)

Desde [ADR-0001](decisions/ADR-0001-pipeline-claude-deepseek.md), autoria e
revisão dependem do [nível de rigor](fluxo-de-feature.md#níveis-de-rigor)
da Task:

- **Trivial**: Claude escreve um `task.md` mínimo (checklist mecânico
  derivado direto dos Critérios de aceite/técnicos do card) — sem
  `implementation.md`, sem rodada de aprovação.
- **Média/complexa**: a DeepSeek rascunha `task.md` **e**
  `implementation.md` (uma chamada de `scripts/deepseek-call.sh`, não o
  harness completo — é geração de texto a partir do card, não edição de
  arquivo/execução de comando). Claude revisa o rascunho antes de
  qualquer commit e responde com exatamente uma destas duas palavras no
  início da revisão:
  - `APPROVED` — a implementação (via `scripts/deepseek-agent.mjs`) pode
    começar a partir do plano como está.
  - `CHANGES_REQUESTED` — seguido dos pontos a corrigir; a DeepSeek
    atualiza o plano e Claude revisa de novo antes de liberar a
    implementação.
  - Para **complexa**, some-se a isso uma revisão de arquitetura do
    Claude (mesmo raciocínio da Fase 2, não uma segunda aprovação
    burocrática) antes do `APPROVED` valer.
  - **Sem sessão Claude disponível** (uso direto do terminal, fora deste
    fluxo): `scripts/deepseek-spec.mjs <número-da-issue>` faz o rascunho
    com tool-calling real (lê o repositório de verdade antes de escrever
    "antes"/"depois", não inventa) e já cria a worktree — sem o
    `APPROVED`/`CHANGES_REQUESTED` de Claude nesse caminho. Recusa
    épicos e, ao encontrar ambiguidade genuína, escreve `##
    Inconsistências encontradas` no `task.md` e abre uma issue de
    bloqueio em vez de adivinhar (mesmo padrão de
    `scripts/pipeline-orchestrator.sh`). `scripts/deepseek-develop.sh
    <número-da-issue>` encadeia isso com a implementação
    (`deepseek-agent.mjs`), o gate completo, PR e autorrevisão
    (`deepseek-review.sh`) — do zero ao PR aberto/mergeado num único
    comando, sem nenhuma sessão Claude no caminho.

## Ciclo de vida

- Escrita na **Fase 2.5** do fluxo de feature, como parte da branch/worktree
  da Task (primeiro commit, antes de qualquer teste/implementação — ver
  [`fluxo-de-feature.md`](fluxo-de-feature.md#fase-25--spec-técnica)).
- Atualizada durante a Fase 3 se a implementação revelar que o desenho
  mudou (ex: um edge point novo apareceu só ao codar) — mantenha o arquivo
  fiel ao que foi de fato implementado, não ao plano inicial se ele mudou.
  Ambiguidade encontrada pela DeepSeek durante a implementação vira uma
  seção `## Inconsistências encontradas` neste `task.md`, resolvida pelo
  Claude antes de a implementação continuar (a DeepSeek não decide sozinha
  — ver `AGENTS.md#antes-de-modificar-código`).
- **Não é descartada após o merge** (diferente dos prints de
  `qa-review`): fica em `docs/specs/` como documentação técnica de
  referência — útil para quem for tocar aquele código depois e quiser saber
  a decisão sem escavar o histórico de commits.
- A implementação (Fase 3) recebe o caminho da pasta de spec como
  argumento de `scripts/deepseek-agent.mjs` (`--task
  docs/specs/<n>-<slug>/task.md`), em vez de ter o desenho técnico
  reexplicado inline — reduz o custo de montar o prompt e mantém uma
  única fonte de verdade.
