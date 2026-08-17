#!/usr/bin/env bash
# Derruba tudo que scripts/qa-up.sh e scripts/qa-web-static.sh subiram,
# liberando as portas fixas do qa-review (5432, 8080, 8081) para a próxima
# rodada. Idempotente — seguro rodar mesmo se nada estiver de pé.
# Uso: scripts/qa-down.sh
set -euo pipefail
cd "$(dirname "$0")/.."

docker compose down
fuser -k "${WEB_PORT:-8081}/tcp" 2>/dev/null || true

echo "Stack e servidor web derrubados."
