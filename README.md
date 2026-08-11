# Synclass

Plataforma de gerenciamento de horários e cobranças para professores particulares e seus alunos.

> Status: **fundação do projeto**. Os requisitos funcionais (agendamento, regras de
> cobrança, frequência, etc.) ainda **não** foram implementados — eles existem apenas
> como especificação em [`docs/backlog/requisitos-funcionais.md`](docs/backlog/requisitos-funcionais.md)
> para dimensionar a complexidade do sistema. Esta etapa entrega apenas a base
> não-funcional: infraestrutura, arquitetura, convenções e esqueleto de código.

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

Instruções detalhadas de setup local estão em `backend/README.md` e `frontend/README.md`.
Resumo rápido:

```bash
# Backend + banco de dados via Docker
docker compose up -d db api

# Frontend (Expo)
cd frontend
npm install
npm run web      # versão web
npm start         # QR code para Expo Go (mobile)
```
