#!/usr/bin/env bash
# Wrapper fino para chamar a API DeepSeek (endpoint compatível com OpenAI),
# usado pelos pontos de baixo julgamento do pipeline feature-flow (ver
# docs/spec/fluxo-de-feature.md, tabela "Divisão de modelo/agente por
# fase") em vez do modelo principal, para reduzir custo onde o erro é
# barato de corrigir numa rodada seguinte.
#
# Lê DEEPSEEK_API_KEY de .env (raiz do repo) ou do ambiente já exportado —
# nunca aceita a chave como argumento, pra não vazar em `ps`/histórico do
# shell. O prompt do usuário vem do stdin; o system prompt é opcional via
# --system.
#
# Uso:
#   scripts/deepseek-call.sh [--system "instrução de sistema"] <<< "prompt"
#   echo "prompt" | scripts/deepseek-call.sh --system "$(cat instrucoes.md)"
set -euo pipefail
cd "$(dirname "$0")/.."

if [ -f .env ]; then
  set -a
  # shellcheck disable=SC1091
  source .env
  set +a
fi

if [ -z "${DEEPSEEK_API_KEY:-}" ]; then
  echo "DEEPSEEK_API_KEY não definida (esperada em .env ou no ambiente)." >&2
  exit 1
fi

SYSTEM_PROMPT=""
MODEL="${DEEPSEEK_MODEL:-deepseek-chat}"

while [ $# -gt 0 ]; do
  case "$1" in
    --system)
      SYSTEM_PROMPT="$2"
      shift 2
      ;;
    --model)
      MODEL="$2"
      shift 2
      ;;
    *)
      echo "Argumento desconhecido: $1" >&2
      exit 1
      ;;
  esac
done

USER_PROMPT="$(cat)"

BODY="$(jq -n \
  --arg model "$MODEL" \
  --arg system "$SYSTEM_PROMPT" \
  --arg user "$USER_PROMPT" \
  '{
    model: $model,
    messages: (
      (if $system != "" then [{role: "system", content: $system}] else [] end)
      + [{role: "user", content: $user}]
    ),
    stream: false
  }')"

RESPONSE="$(curl -sS https://api.deepseek.com/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer ${DEEPSEEK_API_KEY}" \
  -d "$BODY")"

ERROR_MSG="$(echo "$RESPONSE" | jq -r '.error.message? // empty')"
if [ -n "$ERROR_MSG" ]; then
  echo "Erro da API DeepSeek: $ERROR_MSG" >&2
  exit 1
fi

echo "$RESPONSE" | jq -r '.choices[0].message.content'
