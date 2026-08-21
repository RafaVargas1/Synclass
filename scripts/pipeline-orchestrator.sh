#!/usr/bin/env bash
# Orquestrador mecânico do pipeline feature-flow (ADR-0003,
# docs/spec/decisions/ADR-0003-orquestrador-mecanico-sem-claude.md).
#
# Ao contrário de `~/.claude/cron-scripts/no_name-continue.sh` (que abre
# uma sessão `claude -p`), este script NUNCA invoca Claude — só bash, `gh`
# e `node scripts/deepseek-agent.mjs`. Cobre só o que é mecânico o
# bastante pra não exigir julgamento de LLM:
#
#   1. Continuar uma Task já especificada (docs/specs/<n>-<slug>/task.md
#      já existe, com itens não marcados, sem "## Inconsistências
#      encontradas") disparando o harness da DeepSeek direto na worktree.
#   2. Depois de um harness bem-sucedido, rodar o gate de
#      CONTRIBUTING.md#antes-de-abrir-um-pr e abrir o PR se ainda não
#      existir.
#   3. Mergear (squash) qualquer PR com CI verde e o marcador
#      `<!-- dev-review:status=aprovado -->` no comentário mais recente
#      de dev-review (ver .claude/skills/dev-review/SKILL.md, Passo 7).
#   4. Remover worktrees cujo PR já foi mergeado.
#
# Nunca escreve task.md/implementation.md do zero nem decide ambiguidade —
# isso continua exigindo uma sessão Claude (Etapa B do cron).
#
# Imprime, na última linha de stdout, um marcador que o cron usa pra
# decidir se vale a pena gastar uma sessão Claude na Etapa B:
#   PIPELINE_ORCHESTRATOR_RESULT: judgment_needed=<yes|no> reason="..."
#
# Uso: scripts/pipeline-orchestrator.sh

set -uo pipefail
cd "$(dirname "$0")/.."
REPO_ROOT="$(pwd)"

JUDGMENT_NEEDED="no"
JUDGMENT_REASON="nada pendente"

marcar_julgamento() {
  JUDGMENT_NEEDED="yes"
  JUDGMENT_REASON="$1"
}

log() {
  echo "[pipeline-orchestrator] $*"
}

# Roda o gate completo de CONTRIBUTING.md#antes-de-abrir-um-pr no diretório
# dado. Retorna 0 se tudo passou.
rodar_gate() {
  local dir="$1"
  local ok=0

  if [ -d "$dir/backend" ]; then
    (cd "$dir/backend" && dotnet format --verify-no-changes) >/tmp/po-dotnet-format.log 2>&1 || ok=1
    (cd "$dir/backend" && dotnet test) >/tmp/po-dotnet-test.log 2>&1 || ok=1
  fi
  if [ -d "$dir/frontend" ]; then
    (cd "$dir/frontend" && npm run lint) >/tmp/po-npm-lint.log 2>&1 || ok=1
    (cd "$dir/frontend" && npm run typecheck) >/tmp/po-npm-typecheck.log 2>&1 || ok=1
    (cd "$dir/frontend" && npm test) >/tmp/po-npm-test.log 2>&1 || ok=1
  fi
  return $ok
}

# --- 1. Continuar uma Task já especificada, uma por execução ------------

TASK_EM_ANDAMENTO=""
WORKTREE_EM_ANDAMENTO=""

while IFS= read -r worktree_path; do
  [ -z "$worktree_path" ] && continue
  # Nunca rodar o harness na worktree principal (REPO_ROOT) — ela pode ter
  # WIP não commitado alheio ao pipeline (ex: docs/specs/ órfão de uma
  # issue já fechada). Task de verdade sempre vive numa worktree dedicada
  # criada pelo próprio fluxo (ex: ../synclass-<slug>).
  [ "$worktree_path" = "$REPO_ROOT" ] && continue
  for task_md in "$worktree_path"/docs/specs/*/task.md; do
    [ -f "$task_md" ] || continue
    if grep -q "## Inconsistências encontradas" "$task_md"; then
      continue
    fi
    if grep -qE '^- \[ \]' "$task_md"; then
      TASK_EM_ANDAMENTO="$task_md"
      WORKTREE_EM_ANDAMENTO="$worktree_path"
      break 2
    fi
  done
done < <(git worktree list --porcelain | awk '/^worktree /{print $2}')

if [ -n "$TASK_EM_ANDAMENTO" ]; then
  # Guard defensivo: se a issue referenciada no task.md já estiver fechada,
  # é uma spec órfã/duplicada (trabalho já mergeado por outro caminho) —
  # não roda o harness de novo, só sinaliza pra alguém decidir o que fazer
  # com a worktree/spec parada.
  ISSUE_NUM="$(grep -oE '#[0-9]+' "$TASK_EM_ANDAMENTO" | head -1 | tr -d '#')"
  if [ -n "$ISSUE_NUM" ]; then
    ISSUE_STATE="$(gh issue view "$ISSUE_NUM" --json state --jq '.state' 2>/dev/null || true)"
    if [ "$ISSUE_STATE" = "CLOSED" ]; then
      marcar_julgamento "task.md em $TASK_EM_ANDAMENTO referencia a issue #$ISSUE_NUM, que já está fechada — spec órfã/duplicada, precisa de decisão (limpar worktree ou investigar por que não foi limpa antes)"
      TASK_EM_ANDAMENTO=""
    fi
  fi
fi

if [ -n "$TASK_EM_ANDAMENTO" ]; then
  log "Continuando Task em $TASK_EM_ANDAMENTO (worktree $WORKTREE_EM_ANDAMENTO)"
  TASK_REL="${TASK_EM_ANDAMENTO#"$WORKTREE_EM_ANDAMENTO"/}"

  node "$REPO_ROOT/scripts/deepseek-agent.mjs" \
    --task "$TASK_REL" \
    --system docs/spec/code-style.md,docs/spec/business-rules.md,docs/spec/security-rules.md,docs/spec/testing-standards.md \
    --cwd "$WORKTREE_EM_ANDAMENTO"
  EXIT_CODE=$?

  case "$EXIT_CODE" in
    0)
      log "Harness concluiu a Task. Rodando gate de CI antes do PR."
      if rodar_gate "$WORKTREE_EM_ANDAMENTO"; then
        log "Gate verde."
        BRANCH="$(git -C "$WORKTREE_EM_ANDAMENTO" branch --show-current)"
        EXISTING_PR="$(gh pr list --head "$BRANCH" --json number --jq '.[0].number' 2>/dev/null || true)"
        if [ -z "$EXISTING_PR" ]; then
          (cd "$WORKTREE_EM_ANDAMENTO" && git push -u origin "$BRANCH" && gh pr create --fill) \
            || marcar_julgamento "harness terminou e gate passou, mas 'gh pr create' falhou em $BRANCH — checar manualmente"
        else
          log "PR #$EXISTING_PR já existe para $BRANCH — nada a abrir."
        fi
      else
        marcar_julgamento "harness marcou task.md como concluído mas o gate completo de CI falhou em $WORKTREE_EM_ANDAMENTO (DeepSeek só roda testes escopados) — precisa de investigação"
      fi
      ;;
    3)
      log "Harness saiu com código 3 (limite/quota da API DeepSeek, ADR-0002) — não é falha da Task, tenta de novo na próxima execução."
      ;;
    1)
      marcar_julgamento "harness saiu com código 1 (teto de iterações ou '## Inconsistências encontradas') em $TASK_EM_ANDAMENTO"
      ;;
    *)
      marcar_julgamento "harness saiu com código inesperado ($EXIT_CODE) em $TASK_EM_ANDAMENTO"
      ;;
  esac
else
  log "Nenhuma worktree com Task pendente para continuar."
fi

# --- 2. Mergear PRs já aprovados por dev-review + CI verde ---------------

PR_NUMBERS="$(gh pr list --state open --json number --jq '.[].number' 2>/dev/null || true)"

for PR_NUM in $PR_NUMBERS; do
  MARCADOR="$(gh pr view "$PR_NUM" --json comments \
    --jq '[.comments[] | select(.body | test("<!-- dev-review:status="))] | last | .body' 2>/dev/null || true)"

  if [ -z "$MARCADOR" ] || [ "$MARCADOR" = "null" ]; then
    marcar_julgamento "PR #$PR_NUM aberto sem marcador de dev-review ainda — precisa rodar a skill"
    continue
  fi

  if ! echo "$MARCADOR" | grep -q "dev-review:status=aprovado"; then
    marcar_julgamento "PR #$PR_NUM tem dev-review com mudanças solicitadas — precisa de correção/decisão"
    continue
  fi

  CHECKS_OK="$(gh pr checks "$PR_NUM" --json state --jq \
    '[.[] | select(.state != "SUCCESS" and .state != "SKIPPED")] | length == 0' 2>/dev/null || echo "false")"
  if [ "$CHECKS_OK" != "true" ]; then
    log "PR #$PR_NUM aprovado por dev-review, mas checks de CI não estão todos verdes ainda — aguardando."
    continue
  fi

  log "PR #$PR_NUM: dev-review aprovado + CI verde. Fazendo squash-merge."
  if gh pr merge "$PR_NUM" --squash --delete-branch; then
    log "PR #$PR_NUM mergeado."
  else
    marcar_julgamento "squash-merge do PR #$PR_NUM falhou (conflito ou proteção de branch) — checar manualmente"
  fi
done

# --- 3. Limpar worktrees cujo PR já foi mergeado -------------------------

while IFS= read -r worktree_path; do
  [ -z "$worktree_path" ] && continue
  [ "$worktree_path" = "$REPO_ROOT" ] && continue
  BRANCH="$(git -C "$worktree_path" branch --show-current 2>/dev/null || true)"
  [ -z "$BRANCH" ] && continue
  MERGED="$(gh pr list --head "$BRANCH" --state merged --json number --jq '.[0].number' 2>/dev/null || true)"
  if [ -n "$MERGED" ]; then
    log "Removendo worktree $worktree_path (branch $BRANCH, PR #$MERGED já mergeado)."
    git worktree remove "$worktree_path" --force 2>/dev/null || true
  fi
done < <(git worktree list --porcelain | awk '/^worktree /{print $2}')

echo "PIPELINE_ORCHESTRATOR_RESULT: judgment_needed=${JUDGMENT_NEEDED} reason=\"${JUDGMENT_REASON}\""
