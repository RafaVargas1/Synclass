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

### `implementation.md` — desenho técnico

Prosa curta + bullets, direcionado a quem vai ler o código, não o card:

- **Entidades/classes afetadas** (novas ou modificadas), com a camada
  (`Domain`/`Infrastructure`/`Api`/componente frontend).
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

## Ciclo de vida

- Escrita na **Fase 2.5** do fluxo de feature, como parte da branch/worktree
  da Task (primeiro commit, antes de qualquer teste/implementação — ver
  [`fluxo-de-feature.md`](fluxo-de-feature.md#fase-25--spec-técnica)).
- Atualizada durante a Fase 3 se a implementação revelar que o desenho
  mudou (ex: um edge point novo apareceu só ao codar) — mantenha o arquivo
  fiel ao que foi de fato implementado, não ao plano inicial se ele mudou.
- **Não é descartada após o merge** (diferente dos prints de
  `qa-review`): fica em `docs/specs/` como documentação técnica de
  referência — útil para quem for tocar aquele código depois e quiser saber
  a decisão sem escavar o histórico de commits.
- Um agente de swarm (Fase 3) recebe o caminho da pasta de spec como parte
  do prompt de delegação, em vez de ter o desenho técnico reexplicado
  inline — reduz o custo de montar o prompt e mantém uma única fonte de
  verdade.
