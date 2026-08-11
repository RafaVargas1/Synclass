# Arquitetura e decisões técnicas

Este documento registra as decisões estruturais tomadas para a fundação do
Synclass. Ele cobre apenas os **requisitos não-funcionais** do projeto — a base
de infraestrutura, convenções e esqueleto de código. Os requisitos funcionais
(agendamento, cobrança, frequência) estão descritos em
[`docs/backlog/requisitos-funcionais.md`](../backlog/requisitos-funcionais.md)
e ainda não foram implementados; eles servem apenas para dimensionar a
complexidade das decisões abaixo (ex: o modelo de dados precisa suportar regras
de cobrança flexíveis, então o schema não deve fechar portas cedo demais).

## Stack

| Camada | Escolha | Motivo |
|---|---|---|
| Backend | .NET 8 (LTS) + ASP.NET Core Web API | Migrations de primeira classe via EF Core, tipagem forte, LTS até nov/2026 |
| ORM / Migrations | Entity Framework Core + Npgsql | Requisito explícito do projeto (migrations versionadas) |
| Banco de dados | PostgreSQL 16 | Requisito explícito do projeto |
| Frontend | React Native + Expo (Expo Router) + TypeScript | Um único código-fonte gera app **web e mobile** (iOS/Android/Web) |
| Estilização | NativeWind (Tailwind para RN) + Atomic Design | Tokens de design centralizados, classes utilitárias consistentes entre web e mobile |
| Orquestração local | Docker + docker-compose | Requisito explícito do projeto |
| CI | GitHub Actions | Integra com o fluxo de issues/PRs do GitHub |

## Monorepo

Um único repositório git contém `backend/` e `frontend/`, para que o fluxo de
`git worktree` descrito em [`CONTRIBUTING.md`](../../CONTRIBUTING.md) funcione
de forma unificada (uma worktree por branch de feature, tocando os dois lados
quando necessário) e para que `docs/` sirva como fonte única de verdade para
ambos os times.

```
synclass/
├── backend/
│   ├── Synclass.sln
│   ├── src/
│   │   ├── Synclass.Api/            # ASP.NET Core Web API, controllers, middlewares
│   │   ├── Synclass.Domain/         # Entidades e regras de domínio (sem dependências externas)
│   │   └── Synclass.Infrastructure/ # EF Core DbContext, migrations, implementações de interfaces
│   └── tests/
│       └── Synclass.Api.Tests/      # xUnit
├── frontend/
│   ├── app/                          # Rotas (Expo Router)
│   └── src/
│       ├── components/{atoms,molecules,organisms,templates}/
│       ├── theme/                    # Design tokens (cores, espaçamento, tipografia)
│       └── lib/
├── docs/
│   ├── spec/                         # Este documento + code-style.md
│   └── backlog/                      # Requisitos funcionais (ainda não implementados)
├── docker-compose.yml
└── .github/workflows/
```

## Backend: camadas

- **Domain**: entidades e regras de negócio puras, sem depender de EF Core,
  ASP.NET ou qualquer biblioteca externa. É o núcleo que será exercitado pelos
  testes de unidade quando os requisitos funcionais forem implementados.
- **Infrastructure**: implementação do `DbContext`, migrations do EF Core e
  qualquer adaptador para serviços externos (ex: envio de convite via WhatsApp),
  sempre atrás de uma interface definida no `Domain`.
- **Api**: controllers, middlewares (incluindo o de *track id*, ver abaixo),
  configuração de injeção de dependência e composição da aplicação.

Essa separação existe para que os testes de regra de negócio (TDD) não
dependam de banco de dados, e para isolar troca de infraestrutura (ex: trocar
Postgres por outro provedor) sem tocar em regra de negócio.

## Logs estruturados e track id

- Logging via **Serilog**, configurado para emitir **JSON estruturado** em
  todos os ambientes (requisito explícito do projeto).
- Um middleware (`TrackIdMiddleware`) roda no início do pipeline HTTP:
  - Lê o header `X-Track-Id` da requisição, se presente (permite rastrear uma
    ação através de múltiplos serviços/chamadas do app mobile).
  - Caso ausente, gera um novo `Guid`.
  - Injeta o valor no `LogContext` do Serilog, de forma que **todo** log
    emitido durante aquela requisição carregue o mesmo `TrackId`.
  - Devolve o valor no header de resposta `X-Track-Id`, para o cliente
    correlacionar o log do app com o log do servidor.
- Esse é o mecanismo de "logs trackeáveis" pedido na especificação original:
  qualquer fluxo (ex: uma tentativa de marcação de aula) pode ser seguido
  ponta a ponta filtrando por um único `TrackId`, mesmo que passe por várias
  camadas/requisições.

## Frontend: Atomic Design

Componentes organizados em 4 níveis, seguindo Brad Frost:

- `atoms/`: menor unidade reutilizável (Button, Input, Text, Avatar). Não
  conhece regra de negócio.
- `molecules/`: composição de átomos com um propósito único (ex: `FormField`
  = `Label` + `Input` + mensagem de erro).
- `organisms/`: seções completas de UI, podem ter estado local (ex: um card
  de horário disponível).
- `templates/`: layout de página sem dados reais (esqueleto de tela).

Os tokens de design (cores, espaçamento, tipografia, raio de borda) vivem em
`frontend/src/theme/tokens.ts` e são a única fonte de verdade, consumida pelo
`tailwind.config.js` do NativeWind — nenhum átomo deve usar um valor de estilo
"mágico" fora dos tokens.

## Banco de dados e migrations

- O `DbContext` (`SynclassDbContext`) vive em `Synclass.Infrastructure`.
- Migrations são geradas com `dotnet ef migrations add <Nome>` a partir de
  `backend/src/Synclass.Infrastructure` e aplicadas automaticamente na
  inicialização do container `api` (ambiente de desenvolvimento) — ver
  `docker-compose.yml`.
- A migration inicial (`InitialCreate`) desta fundação é intencionalmente
  vazia: valida que o pipeline (SDK → EF Core → Postgres → aplicação da
  migration) funciona ponta a ponta antes de qualquer entidade de domínio
  ser desenhada.

## TDD / SDD

- Specs (este diretório `docs/spec/` e o backlog funcional) são escritas
  **antes** do código que implementam.
- Toda função nova nasce com um teste (ver
  [`code-style.md`](code-style.md#testes)).
- Nesta etapa de fundação, os únicos testes existentes são testes de fumaça
  (*smoke tests*) que provam que o esqueleto (API sobe, endpoint de health
  responde, app renderiza um átomo) funciona — nenhum requisito funcional foi
  testado ou implementado ainda, por decisão explícita do escopo desta etapa.

## Fora de escopo nesta etapa

Itens abaixo pertencem aos requisitos funcionais e **não** foram implementados
nesta fundação (apenas registrados como backlog):

- Autenticação/autorização de Professor e Aluno.
- Modelagem de entidades de domínio (Professor, Aluno, Horário, RegraDeCobrança,
  Frequência, Convite).
- Integração com WhatsApp para convites.
- Qualquer tela além da tela de exemplo (`Home`) e endpoint além de `/health`.
