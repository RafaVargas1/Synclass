#!/usr/bin/env bash
# Sobe, lado a lado, um ambiente de "produção" (container Docker, build
# Release, ASPNETCORE_ENVIRONMENT=Production) e um de desenvolvimento
# (dotnet watch run local, hot reload), com os logs de ambos formatados e
# misturados no mesmo terminal — cada linha se identifica pelo campo
# Environment (ver scripts/pretty-log.sh).
#
# Uso: backend/scripts/watch.sh
# Ctrl+C encerra o dev e o acompanhamento de logs do container; o container
# em si continua de pé (rode `docker compose down` para derrubá-lo).

set -euo pipefail
cd "$(dirname "$0")/.."

docker compose up -d --build db api

pid_tail=""
cleanup() {
  [[ -n "$pid_tail" ]] && kill "$pid_tail" 2>/dev/null || true
}
trap cleanup EXIT

docker compose logs -f --no-log-prefix --since 0s api \
  | ./scripts/pretty-log.sh &
pid_tail=$!

dotnet watch run --project src/Synclass.Api \
  | ./scripts/pretty-log.sh
