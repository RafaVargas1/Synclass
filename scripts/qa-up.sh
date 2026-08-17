#!/usr/bin/env bash
# Sobe a stack local (Postgres + Api) de forma reprodutível para qa-review,
# sem depender de descoberta por tentativa e erro:
#   - builda a imagem da Api antes do `up`, senão o container pode rodar um
#     commit velho (a imagem só é reconstruída sob demanda pelo compose);
#   - aplica as migrations manualmente, porque `RunMigrationsOnStartup` é
#     `false` em produção (ver docker-compose.yml) e o container não aplica
#     migration nenhuma sozinho.
# Uso: scripts/qa-up.sh
set -euo pipefail
cd "$(dirname "$0")/.."

docker compose build api
docker compose up -d db api

until curl -sf "http://localhost:${API_PORT:-8080}/health" >/dev/null; do
  sleep 1
done

export PATH="$PATH:$HOME/.dotnet/tools"
(cd backend/src/Synclass.Api && dotnet ef database update \
  --project ../Synclass.Infrastructure --startup-project .)

echo "Stack pronta: Postgres em :${POSTGRES_PORT:-5432}, Api em http://localhost:${API_PORT:-8080}."
