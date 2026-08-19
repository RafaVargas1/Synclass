#!/usr/bin/env bash
# Sobe só o banco (dev roda a Api local via `dotnet watch run`, não em
# container — ver README.md#debug) e aplica as migrations pendentes. É o
# único comando que um dev precisa rodar antes de `dotnet watch run`.
# Uso: backend/scripts/dev-up.sh
set -euo pipefail
cd "$(dirname "$0")/.."

docker compose up -d --wait db

export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef database update --project src/Synclass.Infrastructure --startup-project src/Synclass.Api

echo "Banco pronto em :${POSTGRES_PORT:-5432}. Rode a Api com: dotnet watch run --project src/Synclass.Api"
