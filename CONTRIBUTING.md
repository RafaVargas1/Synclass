# Como contribuir

Este projeto é fortemente orientado a Git: o histórico é uma ferramenta de
trabalho, não um subproduto. Este documento descreve como organizar branches,
commits e trabalho paralelo.

## Antes de começar: leia o log

Antes de iniciar qualquer trabalho, rode:

```bash
git log --oneline --graph --all -20
```

O objetivo é identificar se o que você está prestes a fazer já foi feito,
desfeito, ou está em andamento em outra branch — para evitar o ciclo
"faz X, depois Y, depois volta pra X" que desperdiça revisão e gera commits
de reversão. Se o histórico mostrar um caminho abandonado, entenda o porquê
(via `git log -p` ou `git show <sha>`) antes de repeti-lo.

## Branches e worktrees

- `main` é sempre estável e "verde" (build + testes passando).
- Trabalho novo acontece em branches `feature/<escopo-curto>`,
  `fix/<escopo-curto>` ou `chore/<escopo-curto>`.
- Quando for necessário desenvolver mais de uma frente **em paralelo** (ex:
  uma feature de backend longa enquanto uma correção urgente de frontend
  precisa sair), use `git worktree` em vez de fazer stash/checkout
  repetidamente na mesma pasta:

  ```bash
  # Cria uma nova worktree para uma feature, em uma pasta irmã do repo
  git worktree add ../synclass-regra-cobranca feature/regra-cobranca

  # Trabalha nela normalmente em outro terminal/janela do editor
  cd ../synclass-regra-cobranca

  # Ao terminar e já ter feito o merge/PR, remove a worktree
  git worktree remove ../synclass-regra-cobranca
  ```

  Isso evita perder contexto de build (ex: `node_modules`, `bin/obj` do .NET)
  ao alternar de tarefa, e permite ter duas branches abertas em editores
  diferentes ao mesmo tempo.

## Commits

- Siga [Conventional Commits](https://www.conventionalcommits.org/pt-br/):
  `tipo(escopo opcional): descrição no imperativo`.
  - Tipos usados no projeto: `feat`, `fix`, `docs`, `chore`, `refactor`,
    `test`, `ci`.
  - Exemplos: `feat(backend): adiciona middleware de track id`,
    `fix(frontend): corrige cálculo de duração da aula`.
- Um commit deve representar uma mudança coesa e completa (build + testes
  passando naquele commit). Não deixe commits intermediários quebrados no
  meio de uma branch que será mergeada — se necessário, faça squash antes do
  merge.
- Não force-push em `main`. Force-push em branch de feature própria é
  aceitável para reescrever histórico local antes de abrir o PR.

## Pull Requests e Issues

- Toda issue (`feature` ou `fix`) segue o padrão descrito em
  [`docs/backlog/padrao-de-issue.md`](docs/backlog/padrao-de-issue.md):
  título direto, história de usuário, Regra de Negócio, critérios de aceite
  testáveis e contexto/protótipo. Prioridade vai na label `priority:P0`..`P3`.
- O board segue as colunas Backlog → Em Desenvolvimento → Em Teste →
  Concluído, ordenadas por prioridade dentro de cada coluna.
- Todo PR referencia uma issue (`Closes #123`) usando os cards criados no
  GitHub a partir de [`docs/backlog/requisitos-funcionais.md`](docs/backlog/requisitos-funcionais.md)
  ou de bugs/débitos técnicos abertos durante o desenvolvimento.
- PRs pequenos e focados são preferíveis a PRs grandes — facilita revisão e
  reduz o risco de conflito com outras frentes paralelas.
- CI (`.github/workflows/`) deve passar antes do merge: build + testes de
  backend e frontend.

## Antes de abrir um PR

```bash
# Backend
cd backend && dotnet format && dotnet test

# Frontend
cd frontend && npm run lint && npm run typecheck && npm test
```
