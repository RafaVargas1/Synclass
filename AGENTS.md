# Synclass — instruções para agentes

Este arquivo é a camada operacional para qualquer agente (Claude,
DeepSeek, ou outro) trabalhando neste repositório. Ele não repete o
"Quality Contract" — só diz onde encontrá-lo e como operar.

## Antes de entender o projeto

Leia, nesta ordem:

1. [`docs/spec/architecture.md`](docs/spec/architecture.md) — stack,
   camadas, estrutura de monorepo, logs estruturados.
2. [`docs/spec/code-style.md`](docs/spec/code-style.md) — convenções de
   código, nomenclatura, DI, wrapper de terceiros.
3. [`docs/spec/business-rules.md`](docs/spec/business-rules.md) —
   invariantes de negócio transversais (identidade, matrícula, convites,
   horários) que valem independente da issue que você está resolvendo.
4. [`docs/spec/security-rules.md`](docs/spec/security-rules.md) —
   validação, autorização, segredos, dados sensíveis em log.
5. [`docs/spec/testing-standards.md`](docs/spec/testing-standards.md) —
   TDD, testes escopados vs. suíte completa, cobertura por camada.
6. [`docs/backlog/padrao-de-issue.md`](docs/backlog/padrao-de-issue.md) —
   como uma issue é estruturada; a issue do GitHub é o contrato funcional
   da tarefa.
7. [`docs/spec/fluxo-de-feature.md`](docs/spec/fluxo-de-feature.md) — o
   pipeline completo (ideia → merge → relatório), níveis de rigor, e a
   divisão Claude/DeepSeek por fase.

Esses seis/sete arquivos, junto com
[`docs/spec/decisions/`](docs/spec/decisions/) (ADRs), são o Quality
Contract do projeto — não existe uma pasta `.ai/` separada, este conjunto
já cumpre esse papel.

## Antes de modificar código

- Confira `git log --oneline --graph --all -20`
  ([`CONTRIBUTING.md`](CONTRIBUTING.md#antes-de-começar-leia-o-log)) —
  identifique se o que você vai fazer já foi feito, desfeito, ou está em
  andamento em outra branch.
- Se a Task tem `docs/specs/<n>-<slug>/{task.md,implementation.md}`
  (ver [`especificacao-tecnica.md`](docs/spec/especificacao-tecnica.md)),
  leia os dois antes de escrever qualquer código — são a fonte única de
  verdade da abordagem técnica, não re-derive o desenho a partir da issue
  sozinha.
- A issue do GitHub referenciada pela Task (`Closes #N`) é o contrato
  funcional: não expanda escopo, não refatore código não relacionado, não
  altere regra de negócio, não ignore critério de aceite por iniciativa
  própria. Ambiguidade ou inconsistência encontrada vira uma seção
  `## Inconsistências encontradas` no `task.md`, para o Claude resolver
  antes de prosseguir — não adivinhe.

## Comandos

```bash
# Backend (a partir de backend/)
dotnet restore
dotnet build
dotnet test                          # suíte completa
dotnet test --filter <Classe>        # escopado, durante o loop de TDD
dotnet format                        # formatação
dotnet format --verify-no-changes    # check de CI

# Frontend (a partir de frontend/)
npm run lint
npm run typecheck
npm test                             # suíte completa
npx jest <caminho ou nome>           # escopado, durante o loop de TDD
npm run format
```

Gate completo antes de abrir PR (não dispensado por nenhum nível de
rigor): [`CONTRIBUTING.md#antes-de-abrir-um-pr`](CONTRIBUTING.md#antes-de-abrir-um-pr).

## Como validar uma implementação concluída

Não considere uma tarefa concluída só porque o código compila. Precisa de
todos os itens:

- Critérios de aceite da issue atendidos.
- Testes relevantes passando (e cobrindo o que os critérios pedem).
- `dotnet format --verify-no-changes` / `npm run lint` sem erro.
- `dotnet build` / `npm run typecheck` sem erro.
- Gate completo de `CONTRIBUTING.md#antes-de-abrir-um-pr` verde.
- Sem regressão conhecida (rodou a suíte completa antes do PR, não só o
  teste escopado do último passo do TDD).

`dev-review` (`.claude/skills/dev-review/SKILL.md`) roda por padrão em
todo PR do pipeline automático. `qa-review` (Playwright) não roda por
padrão — só sob pedido explícito do usuário (ver ADR-0001).

## Trabalhando com código legado / mudanças estruturais

- **Nunca** edite uma migration já aplicada em `main`
  (`backend/src/Synclass.Infrastructure/Persistence/Migrations/`) — crie
  uma nova.
- **Nunca** commite `.env` (só `.env.example`, com a variável vazia).
- Mudança de arquitetura, de stack, ou reversão de uma decisão já
  documentada (ex: um ADR existente) exige um novo ADR em
  `docs/spec/decisions/ADR-<NNNN>-<slug>.md` (mesmo formato de
  `ADR-0001-pipeline-claude-deepseek.md`) **antes** do código — não depois,
  como justificativa retroativa.
- Toque em `backend/src/Synclass.Api/Program.cs` ou em
  `SynclassDbContext.cs` com cuidado redobrado quando houver mais de uma
  Task em andamento em paralelo (worktrees diferentes) — são pontos de
  conflito garantido entre migrations concorrentes
  (`docs/spec/fluxo-de-feature.md#fase-3--implementação`).

## Níveis de rigor

Ver [`fluxo-de-feature.md#níveis-de-rigor`](docs/spec/fluxo-de-feature.md#níveis-de-rigor)
para a tabela completa (trivial/média/complexa) e os gatilhos que fazem
uma Task ser tratada como complexa (auth, pagamento, dado sensível,
migration, API pública, concorrência).
