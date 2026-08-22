#!/usr/bin/env bash
# Desenvolve uma issue do início ao fim usando só a DeepSeek — nenhuma
# sessão Claude no caminho. Encadeia as três peças que já existiam
# separadas (scripts/deepseek-spec.mjs, scripts/deepseek-agent.mjs,
# scripts/pipeline-orchestrator.sh) num único comando síncrono, pra quem
# quer rodar o pipeline direto do terminal sem passar pelo Claude Code:
#
#   1. scripts/deepseek-spec.mjs <issue>   — rascunha task.md/
#      implementation.md a partir da issue do GitHub, cria a worktree.
#   2. scripts/deepseek-agent.mjs          — TDD real (read/write/run
#      command) até o task.md ficar todo marcado.
#   3. Gate completo de CONTRIBUTING.md#antes-de-abrir-um-pr.
#   4. `gh pr create` + scripts/deepseek-review.sh (autorrevisão,
#      ADR-0004) em loop, até 2 rodadas.
#   5. `gh pr merge --squash --delete-branch` se aprovado + CI verde.
#
# Qualquer bloqueio real (ambiguidade de produto, teto de iterações,
# autorrevisão reprovando 2x, gate de CI falhando) para o script e abre
# uma issue de bloqueio no GitHub (mesmo padrão do
# pipeline-orchestrator.sh) em vez de adivinhar — decisão de produto
# nunca é tomada por este script.
#
# Uso: scripts/deepseek-develop.sh <numero-da-issue>
#
# Épicos são recusados de propósito (mesmo motivo de deepseek-spec.mjs) —
# quebre em Tasks primeiro (issue simples por Task) e rode este comando
# uma vez por Task.
set -uo pipefail
cd "$(dirname "$0")/.."
REPO_ROOT="$(pwd)"

ISSUE_NUM="${1:?Uso: scripts/deepseek-develop.sh <numero-da-issue>}"

log() { echo "[deepseek-develop #$ISSUE_NUM] $*"; }

log "Etapa 1/5 — spec técnica (scripts/deepseek-spec.mjs)."
SPEC_OUTPUT="$(node scripts/deepseek-spec.mjs "$ISSUE_NUM" 2>&1)"
SPEC_EXIT=$?
echo "$SPEC_OUTPUT"

if [ "$SPEC_EXIT" -eq 3 ]; then
  log "Limite/quota da API DeepSeek (ADR-0002) — tente de novo mais tarde."
  exit 3
fi
if [ "$SPEC_EXIT" -ne 0 ]; then
  log "Etapa de spec não concluiu (ver saída acima e a issue de bloqueio, se houver). Parando."
  exit 1
fi

TASK_MD="$(echo "$SPEC_OUTPUT" | grep -oE 'docs/specs/[0-9]+-[a-z0-9-]+/task\.md' | head -1)"
if [ -z "$TASK_MD" ]; then
  log "Não consegui extrair o caminho do task.md gerado da saída da etapa 1 — parando."
  exit 1
fi
SPEC_DIR="$(dirname "$TASK_MD")"
SLUG="$(basename "$SPEC_DIR")"
# deepseek-spec.mjs nomeia a worktree "synclass-<slug>" (sem o prefixo
# "<n>-" que a pasta de spec tem) — git worktree list é a fonte confiável
# do path, procuramos pelo sufixo do nome do diretório em vez de
# reconstruir o path.
SLUG_SEM_NUMERO="${SLUG#*-}"
WORKTREE="$(git worktree list --porcelain | awk -v slug="$SLUG_SEM_NUMERO" '/^worktree /{path=$2} path && (path ~ "synclass-"slug"$"){print path; exit}')"
if [ -z "$WORKTREE" ]; then
  log "Não encontrei a worktree correspondente a $SLUG em 'git worktree list' — parando."
  exit 1
fi
log "Spec pronta em $TASK_MD (worktree $WORKTREE)."

log "Etapa 2/5 — implementação TDD (scripts/deepseek-agent.mjs)."
node scripts/deepseek-agent.mjs \
  --task "$TASK_MD" \
  --system docs/spec/code-style.md,docs/spec/business-rules.md,docs/spec/security-rules.md,docs/spec/testing-standards.md,docs/spec/ux-heuristics.md,docs/spec/engenharia-de-qualidade.md \
  --cwd "$WORKTREE"
IMPL_EXIT=$?

if [ "$IMPL_EXIT" -eq 3 ]; then
  log "Limite/quota da API DeepSeek durante a implementação — tente de novo mais tarde (a Task fica pendente, scripts/pipeline-orchestrator.sh retoma sozinho)."
  exit 3
fi
if [ "$IMPL_EXIT" -ne 0 ]; then
  log "Harness não concluiu (teto de iterações ou '## Inconsistências encontradas'). Ver $SPEC_DIR/deepseek-run.log e $TASK_MD."
  exit 1
fi

log "Etapa 3/5 — gate completo (CONTRIBUTING.md#antes-de-abrir-um-pr)."
GATE_OK=1
if [ -d "$WORKTREE/backend" ]; then
  (cd "$WORKTREE/backend" && dotnet format --verify-no-changes && dotnet test) || GATE_OK=0
fi
if [ -d "$WORKTREE/frontend" ]; then
  (cd "$WORKTREE/frontend" && npm ci --silent && npm run lint && npm run typecheck && npm test) || GATE_OK=0
fi
if [ "$GATE_OK" -ne 1 ]; then
  log "Gate de CI falhou — não abro PR sozinho. Corrija manualmente ou rode de novo scripts/deepseek-agent.mjs sobre a mesma Task."
  exit 1
fi
log "Gate verde."

log "Etapa 4/5 — PR + autorrevisão DeepSeek (ADR-0004)."
BRANCH="$(git -C "$WORKTREE" branch --show-current)"
PR_NUM="$(gh pr list --head "$BRANCH" --json number --jq '.[0].number' 2>/dev/null || true)"
if [ -z "$PR_NUM" ]; then
  (cd "$WORKTREE" && git push -u origin "$BRANCH" && gh pr create --fill) || { log "gh pr create falhou."; exit 1; }
  PR_NUM="$(gh pr list --head "$BRANCH" --json number --jq '.[0].number')"
fi
log "PR #$PR_NUM."

APROVADO=0
for TENTATIVA in 1 2; do
  if scripts/deepseek-review.sh "$PR_NUM"; then
    APROVADO=1
    break
  fi
  log "Autorrevisão pediu mudanças (tentativa $TENTATIVA/2)."
  if [ "$TENTATIVA" -lt 2 ]; then
    printf '\n\n## Achados da autorrevisão (rodada %s)\n\n- [ ] Corrigir os achados do comentário mais recente de autorrevisão no PR #%s (ver `gh pr view %s --comments`)\n' \
      "$TENTATIVA" "$PR_NUM" "$PR_NUM" >> "$WORKTREE/$TASK_MD"
    node scripts/deepseek-agent.mjs \
      --task "$TASK_MD" \
      --system docs/spec/code-style.md,docs/spec/business-rules.md,docs/spec/security-rules.md,docs/spec/testing-standards.md \
      --cwd "$WORKTREE" || true
  fi
done

if [ "$APROVADO" -ne 1 ]; then
  log "Autorrevisão reprovou 2 rodadas seguidas — abrindo issue de bloqueio, não mergeando sozinho."
  gh issue create \
    --title "Bloqueio automático em deepseek-develop.sh: PR #$PR_NUM reprovado 2x na autorrevisão" \
    --body "Issue original: #$ISSUE_NUM. PR: #$PR_NUM. Ver comentários de autorrevisão no PR (\`gh pr view $PR_NUM --comments\`)." \
    --label bloqueio-pipeline 2>/dev/null || true
  exit 1
fi

log "Etapa 5/5 — merge."
CHECKS_OK="$(gh pr checks "$PR_NUM" --json state --jq '[.[] | select(.state != "SUCCESS" and .state != "SKIPPED")] | length == 0' 2>/dev/null || echo "false")"
if [ "$CHECKS_OK" != "true" ]; then
  log "PR #$PR_NUM aprovado mas CI ainda não está toda verde — não mergeando agora. Rode 'gh pr merge $PR_NUM --squash --delete-branch' manualmente (ou scripts/pipeline-orchestrator.sh) quando o CI terminar."
  exit 0
fi

if gh pr merge "$PR_NUM" --squash --delete-branch; then
  log "PR #$PR_NUM mergeado. Issue #$ISSUE_NUM deve fechar automaticamente (Closes #$ISSUE_NUM no corpo do PR)."
  git worktree remove "$WORKTREE" --force 2>/dev/null || true
  log "Concluído — do zero ao merge sem nenhuma sessão Claude."
  exit 0
fi

log "Squash-merge falhou (conflito ou proteção de branch) — resolver manualmente."
exit 1
