# Synclass.Api

API .NET 8 (ASP.NET Core) do Synclass. Ver decisões de arquitetura em
[`../docs/spec/architecture.md`](../docs/spec/architecture.md).

## Setup local

```bash
# Sobe apenas o banco via Docker
docker compose up -d db

# Restaura pacotes e aplica migrations
cd backend
dotnet restore
dotnet ef database update --project src/Synclass.Infrastructure --startup-project src/Synclass.Api

# Roda a API
dotnet run --project src/Synclass.Api
```

A API sobe em `https://localhost:5001` (ou porta configurada) com Swagger em
`/swagger` (ambiente Development). `GET /health` retorna `{"status":"ok","trackId":"..."}`.

## Testes

```bash
dotnet test
```

## Migrations

```bash
# Criar uma nova migration
dotnet ef migrations add NomeDaMigration \
  --project src/Synclass.Infrastructure \
  --startup-project src/Synclass.Api \
  --output-dir Persistence/Migrations

# Aplicar migrations pendentes
dotnet ef database update \
  --project src/Synclass.Infrastructure \
  --startup-project src/Synclass.Api
```

## Estrutura

- `src/Synclass.Domain`: entidades e regras de negócio puras (sem dependências externas).
- `src/Synclass.Infrastructure`: EF Core, `DbContext`, migrations, adaptadores externos.
- `src/Synclass.Api`: controllers, middlewares (`TrackIdMiddleware`), configuração de DI e logging.
- `tests/Synclass.Api.Tests`: testes de integração (xUnit + `WebApplicationFactory`).
