#!/usr/bin/env bash
# Autorrevisão pela DeepSeek (ADR-0004,
# docs/spec/decisions/ADR-0004-autorrevisao-deepseek-lote-final-claude.md):
# substitui a skill `dev-review` (sessão Claude) como gate automático de
# merge nesta leva — mesmo checklist mecânico de docs/spec/code-style.md,
# mesma tabela e o mesmo marcador de máquina que
# scripts/pipeline-orchestrator.sh já sabe ler
# (`<!-- dev-review:status=aprovado -->` /
# `<!-- dev-review:status=mudancas-solicitadas -->`) — o orquestrador não
# muda, só quem escreve o marcador.
#
# Uso: scripts/deepseek-review.sh <numero-do-pr>
#
# Saída: posta um comentário no PR via `gh pr comment` e sai 0 se o
# veredito foi "aprovado", 1 se foi "mudanças solicitadas" (o chamador
# decide se redispara o harness ou trata como bloqueio).
set -euo pipefail
cd "$(dirname "$0")/.."

PR_NUM="${1:?Uso: scripts/deepseek-review.sh <numero-do-pr>}"

DIFF="$(gh pr diff "$PR_NUM")"
if [ -z "$DIFF" ]; then
  echo "Diff vazio ou PR #$PR_NUM não encontrado." >&2
  exit 1
fi

SYSTEM_PROMPT="Você é a etapa de autorrevisão do pipeline Synclass (ADR-0004). Revise o diff de um Pull Request contra as regras de docs/spec/code-style.md: tamanho de função (4-20 linhas) e de arquivo (até 500 linhas), nomes específicos (não genéricos), tipos explícitos (sem any/dynamic/object novo), injeção de dependência via construtor (sem singleton estático/new direto de dependência concreta), wrapper próprio para biblioteca de terceiros (sem chamada direta de SDK externo para I/O), early return (máximo 2 níveis de indentação), mensagens de exceção com valor problemático + formato esperado, cobertura de teste para toda função nova/alterada, e ausência de duplicação de código.

$(cat docs/spec/business-rules.md 2>/dev/null | head -c 4000)

Responda em markdown: uma tabela com colunas Regra | Status (Falhou/Passou/Atenção) | Arquivo:linha | Observação, um resumo de 2-3 linhas com o veredito geral logo no topo, e termine a resposta com exatamente uma destas duas linhas (nunca as duas, nunca uma terceira variante):

<!-- dev-review:status=aprovado -->

ou

<!-- dev-review:status=mudancas-solicitadas -->

Use 'aprovado' só se não houver nenhum achado bloqueante (Falhou). Qualquer achado bloqueante, ou dúvida real, usa 'mudancas-solicitadas'."

RESULTADO="$(scripts/deepseek-call.sh --system "$SYSTEM_PROMPT" <<< "Diff do PR #$PR_NUM:

$DIFF")"

gh pr comment "$PR_NUM" --body "$RESULTADO

---
*Autorrevisão automática pela DeepSeek (ADR-0004) — sem sessão Claude neste PR. A revisão humana/Claude para esta leva de issues acontece uma única vez, no final, sobre todos os PRs juntos.*"

if echo "$RESULTADO" | grep -q "dev-review:status=aprovado"; then
  echo "Autorrevisão: aprovado."
  exit 0
fi

echo "Autorrevisão: mudanças solicitadas." >&2
exit 1
