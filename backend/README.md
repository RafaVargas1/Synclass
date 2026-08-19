# Synclass.Api

API .NET 8 (ASP.NET Core) do Synclass. Ver decisões de arquitetura em
[`../docs/spec/architecture.md`](../docs/spec/architecture.md).

## Setup local

Este é o fluxo padrão do dia a dia: banco no Docker, Api local com hot reload.

```bash
cd backend
dotnet restore
./scripts/dev-up.sh              # sobe o banco (Docker) e aplica migrations pendentes
dotnet watch run --project src/Synclass.Api
```

A Api sobe em `http://localhost:5005` (`https://localhost:7033` no perfil
`https`, ver `src/Synclass.Api/Properties/launchSettings.json`) com Swagger
em `/swagger` (ambiente Development). `GET /health` retorna
`{"status":"ok","trackId":"..."}`.

> Rodar a Api inteira em container (`docker compose up -d db api`) é o
> caminho usado para QA/prod-like (`scripts/qa-up.sh`, na raiz do repo), não
> o fluxo de dev — esse container roda `ASPNETCORE_ENVIRONMENT=Production` e
> não aplica migration sozinho, então builda a imagem e roda as migrations
> manualmente antes de esperar dado nele.

## Debug: logs de desenvolvimento e "produção" juntos

Não há ambiente de produção implantado ainda (ver
[`../docs/spec/architecture.md`](../docs/spec/architecture.md)). Para depurar,
o container `api` do `docker-compose.yml` roda o mesmo build que rodaria em
produção (`dotnet publish -c Release`, `ASPNETCORE_ENVIRONMENT=Production`),
enquanto o desenvolvimento roda local via `dotnet watch run` (hot reload,
`ASPNETCORE_ENVIRONMENT=Development`). Os dois podem ficar de pé ao mesmo
tempo — não competem por porta (container em `:8080`, dev em `:5005`) nem por
banco (dev usa migrations automáticas; produção não, ver
`RunMigrationsOnStartup` em `appsettings.json`).

Todo log (Serilog, JSON estruturado) carrega um campo `Environment`
(`Development`/`Production`), então dá para identificar a origem mesmo com os
dois streams misturados no mesmo terminal.

```bash
# Sobe os dois de uma vez, com os logs mesclados, coloridos por nível e
# identificados por ambiente no mesmo terminal:
./scripts/watch.sh

# Ou manualmente, em terminais separados:
docker compose up -d db api
docker compose logs -f --no-log-prefix api | ./scripts/pretty-log.sh   # "produção"
dotnet watch run --project src/Synclass.Api | ./scripts/pretty-log.sh  # dev
```

`scripts/pretty-log.sh` decodifica o JSON compacto do Serilog (nível,
timestamp, `TrackId`, mensagem já renderizada com as propriedades) para uma
linha legível — útil porque o JSON bruto é ótimo para agregadores mas ruim
para ler direto no terminal durante o debug.

Ctrl+C em `watch.sh` encerra o dev e o acompanhamento de logs do container; o
container em si continua de pé (`docker compose down` para derrubá-lo).

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
