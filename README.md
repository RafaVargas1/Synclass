# Synclass

Plataforma de gerenciamento de horários e cobranças para professores particulares e seus alunos.

> Status: **fundação do projeto**. Os requisitos funcionais (agendamento, regras de
> cobrança, frequência, etc.) ainda **não** foram implementados — eles existem apenas
> como especificação em [`docs/backlog/requisitos-funcionais.md`](docs/backlog/requisitos-funcionais.md)
> para dimensionar a complexidade do sistema. Esta etapa entrega apenas a base
> não-funcional: infraestrutura, arquitetura, convenções e esqueleto de código.
> o projeto atualmente está hospedado em https://github.com/RafaVargas1/Synclass

## Visão geral

- **Backend**: `.NET 8` (ASP.NET Core Web API) + `Entity Framework Core` + `PostgreSQL`.
- **Frontend**: `React Native` (Expo + Expo Router) com `TypeScript`, gerando app **web e mobile**
  a partir do mesmo código, seguindo **Atomic Design** com `NativeWind`.
- **Infra**: `Docker` / `docker-compose` para orquestrar API + banco de dados.
- **Observabilidade**: logs estruturados em JSON com *track id* (correlation id) propagado
  em toda a requisição.
- **Metodologia**: TDD e SDD — specs guiam o código, testes vêm antes da implementação.
- **Git**: histórico linear e rastreável, uso de `git worktree` para trabalho paralelo.

## Documentação

| Documento | Conteúdo |
|---|---|
| [`docs/spec/code-style.md`](docs/spec/code-style.md) | Guia central de estilo de código (norteador de todo o projeto) |
| [`docs/spec/architecture.md`](docs/spec/architecture.md) | Decisões de arquitetura e stack |
| [`docs/backlog/requisitos-funcionais.md`](docs/backlog/requisitos-funcionais.md) | Requisitos funcionais (ainda não implementados) |
| [`docs/backlog/padrao-de-issue.md`](docs/backlog/padrao-de-issue.md) | Padrão de escrita de issues/cards (as 6 seções obrigatórias) |
| [`docs/spec/fluxo-de-feature.md`](docs/spec/fluxo-de-feature.md) | Pipeline autônomo: ideia → issue → implementação → revisão → merge → relatório |
| [`CONTRIBUTING.md`](CONTRIBUTING.md) | Fluxo de git, commits, worktrees |

## Estrutura do repositório

```
synclass/
├── backend/     # API .NET (Synclass.Api, Synclass.Domain, Synclass.Infrastructure, testes)
├── frontend/    # App Expo + TypeScript (web + mobile)
├── docs/        # Specs, arquitetura, backlog
├── docker-compose.yml
└── .github/workflows/  # CI
```

## Como rodar (desenvolvimento)

Instruções completas em [`backend/README.md`](backend/README.md#setup-local) e
[`frontend/README.md`](frontend/README.md#setup-local). Resumo:

```bash
# Backend: banco no Docker + Api local com hot reload (porta 5005)
cd backend
./scripts/dev-up.sh              # sobe o banco e aplica migrations pendentes
dotnet watch run --project src/Synclass.Api

# Frontend (Expo) — em outro terminal; já aponta para a Api de dev acima por padrão
cd frontend
npm install
npm run web       # versão web (http://localhost:8081)
npm start          # QR code para Expo Go (mobile)
```

`docker compose up -d db api` (a Api inteira em container) **não** é o
comando de dev — é o caminho prod-like usado por QA (ver
[`scripts/qa-up.sh`](scripts/qa-up.sh)), que builda a imagem e roda migrations
à parte.
