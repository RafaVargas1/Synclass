#!/usr/bin/env bash
# Orquestrador mecânico do pipeline feature-flow (ADR-0003 + ADR-0004,
# docs/spec/decisions/ADR-0003-orquestrador-mecanico-sem-claude.md,
# docs/spec/decisions/ADR-0004-autorrevisao-deepseek-lote-final-claude.md).
#
# Ao contrário de `~/.claude/cron-scripts/no_name-continue.sh` (que abre
# uma sessão `claude -p`), este script NUNCA invoca Claude — só bash, `gh`
# e `node scripts/deepseek-agent.mjs`/`scripts/deepseek-review.sh`. Cobre:
#
#   1. Continuar TODAS as Tasks já especificadas e elegíveis, em paralelo
#      (uma por worktree independente) — não mais uma por execução.
#   2. Depois de um harness bem-sucedido, rodar o gate de
#      CONTRIBUTING.md#antes-de-abrir-um-pr, abrir o PR se ainda não
#      existir, e disparar a autorrevisão pela própria DeepSeek
#      (scripts/deepseek-review.sh) — sem Claude no caminho (ADR-0004).
#   3. Mergear (squash) qualquer PR com CI verde e o marcador
#      `<!-- dev-review:status=aprovado -->` no comentário mais recente
#      (não importa se foi a skill dev-review ou deepseek-review.sh que
#      escreveu).
#   4. Bloqueio (teto de iterações, "## Inconsistências encontradas", ou
#      autorrevisão reprovando 2 rodadas seguidas) vira `gh issue create`
#      automático + task.md marcado como bloqueado — nunca fica esperando
#      uma sessão Claude decidir; segue para a próxima Task da fila.
#   5. Remove worktrees cujo PR já foi mergeado.
#   6. Fecha epics automaticamente quando todas as sub-issues conhecidas
#      já estiverem fechadas.
#
# Nunca escreve task.md/implementation.md do zero — isso continua exigindo
# uma sessão Claude (Etapa B do cron, cada vez mais rara nesta leva).
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

# Abre uma issue de bloqueio no GitHub e marca o task.md correspondente,
# em vez de deixar a Task pendurada esperando julgamento (ADR-0004 item 3).
# $1 = task.md, $2 = worktree, $3 = motivo/conteúdo do bloqueio
abrir_issue_de_bloqueio() {
  local task_md="$1" worktree="$2" motivo="$3"
  local issue_num branch
  issue_num="$(grep -oE '#[0-9]+' "$task_md" | head -1 | tr -d '#')"
  branch="$(git -C "$worktree" branch --show-current 2>/dev/null || echo desconhecida)"

  local titulo="Bloqueio automático no pipeline: Task de ${issue_num:+#$issue_num }não concluída pela DeepSeek"
  local corpo
  corpo="$(cat <<EOF
Bloqueio detectado automaticamente por \`scripts/pipeline-orchestrator.sh\` (ADR-0004) — sem sessão Claude neste passo.

**Task**: \`$task_md\`
**Branch/worktree**: \`$branch\` ($worktree)
**Issue original**: ${issue_num:+#$issue_num}

**Motivo**:

$motivo

Esta Task não avança sozinha — precisa de decisão (ambiguidade de produto/código, ou correção manual). O orquestrador seguiu para a próxima Task disponível na fila em vez de esperar.
EOF
)"

  local nova_issue
  nova_issue="$(gh issue create --title "$titulo" --body "$corpo" --label "bloqueio-pipeline" 2>/dev/null | grep -oE '[0-9]+$' || true)"
  if [ -z "$nova_issue" ]; then
    # label pode não existir ainda — tenta de novo sem label.
    nova_issue="$(gh issue create --title "$titulo" --body "$corpo" 2>/dev/null | grep -oE '[0-9]+$' || true)"
  fi

  if [ -n "$nova_issue" ]; then
    log "Issue de bloqueio criada: #$nova_issue (Task $task_md)."
    printf '\n\n## Bloqueado — ver issue #%s\n\n%s — %s\n' "$nova_issue" "$(date -Iseconds)" "$motivo" >> "$task_md"
  else
    marcar_julgamento "Task $task_md bloqueada e 'gh issue create' falhou — abrir manualmente"
  fi
}

# --- 1. Descobrir todas as Tasks pendentes e elegíveis (paralelo) --------

declare -a TASKS_PENDENTES=()
declare -a WORKTREES_PENDENTES=()

while IFS= read -r worktree_path; do
  [ -z "$worktree_path" ] && continue
  # Nunca rodar o harness na worktree principal — pode ter WIP alheio ao
  # pipeline. Task de verdade sempre vive numa worktree dedicada.
  [ "$worktree_path" = "$REPO_ROOT" ] && continue
  for task_md in "$worktree_path"/docs/specs/*/task.md; do
    [ -f "$task_md" ] || continue
    grep -q "## Inconsistências encontradas" "$task_md" && continue
    grep -q "## Bloqueado — ver issue" "$task_md" && continue
    grep -qE '^- \[ \]' "$task_md" || continue

    issue_num="$(grep -oE '#[0-9]+' "$task_md" | head -1 | tr -d '#')"
    if [ -n "$issue_num" ]; then
      issue_state="$(gh issue view "$issue_num" --json state --jq '.state' 2>/dev/null || true)"
      if [ "$issue_state" = "CLOSED" ]; then
        marcar_julgamento "task.md em $task_md referencia a issue #$issue_num, que já está fechada — spec órfã/duplicada"
        continue
      fi
    fi
    TASKS_PENDENTES+=("$task_md")
    WORKTREES_PENDENTES+=("$worktree_path")
  done
done < <(git worktree list --porcelain | awk '/^worktree /{print $2}')

if [ "${#TASKS_PENDENTES[@]}" -eq 0 ]; then
  log "Nenhuma worktree com Task pendente para continuar."
else
  log "Disparando ${#TASKS_PENDENTES[@]} Task(s) em paralelo: ${TASKS_PENDENTES[*]}"
  declare -a PIDS=()
  for i in "${!TASKS_PENDENTES[@]}"; do
    task_md="${TASKS_PENDENTES[$i]}"
    worktree="${WORKTREES_PENDENTES[$i]}"
    task_rel="${task_md#"$worktree"/}"
    (
      node "$REPO_ROOT/scripts/deepseek-agent.mjs" \
        --task "$task_rel" \
        --system docs/spec/code-style.md,docs/spec/business-rules.md,docs/spec/security-rules.md,docs/spec/testing-standards.md,docs/spec/ux-heuristics.md \
        --cwd "$worktree"
    ) &
    PIDS+=("$!")
  done

  declare -a EXIT_CODES=()
  for pid in "${PIDS[@]}"; do
    wait "$pid"
    EXIT_CODES+=("$?")
  done

  for i in "${!TASKS_PENDENTES[@]}"; do
    task_md="${TASKS_PENDENTES[$i]}"
    worktree="${WORKTREES_PENDENTES[$i]}"
    exit_code="${EXIT_CODES[$i]}"

    case "$exit_code" in
      0)
        log "Harness concluiu $task_md. Rodando gate de CI antes do PR."
        if rodar_gate "$worktree"; then
          log "Gate verde ($worktree)."
          branch="$(git -C "$worktree" branch --show-current)"
          existing_pr="$(gh pr list --head "$branch" --json number --jq '.[0].number' 2>/dev/null || true)"
          if [ -z "$existing_pr" ]; then
            if (cd "$worktree" && git push -u origin "$branch" && gh pr create --fill); then
              existing_pr="$(gh pr list --head "$branch" --json number --jq '.[0].number' 2>/dev/null || true)"
            else
              marcar_julgamento "harness terminou e gate passou, mas 'gh pr create' falhou em $branch"
              continue
            fi
          else
            log "PR #$existing_pr já existe para $branch."
          fi

          if [ -n "$existing_pr" ]; then
            # ADR-0004: autorrevisão pela própria DeepSeek, sem Claude.
            tentativas_file="$(dirname "$task_md")/.autorreview-tentativas"
            tentativas="$(cat "$tentativas_file" 2>/dev/null || echo 0)"
            if scripts/deepseek-review.sh "$existing_pr"; then
              log "PR #$existing_pr aprovado na autorrevisão."
              rm -f "$tentativas_file"
            else
              tentativas=$((tentativas + 1))
              echo "$tentativas" > "$tentativas_file"
              if [ "$tentativas" -ge 2 ]; then
                abrir_issue_de_bloqueio "$task_md" "$worktree" "Autorrevisão pela DeepSeek (scripts/deepseek-review.sh) pediu mudanças por 2 rodadas seguidas no PR #$existing_pr. Ver comentários do PR para os achados."
              else
                log "PR #$existing_pr: autorrevisão pediu mudanças (tentativa $tentativas/2) — anexando achados ao task.md para a próxima rodada corrigir."
                printf '\n\n## Achados da autorrevisão (rodada %s)\n\n- [ ] Corrigir os achados do comentário mais recente de autorrevisão no PR #%s (ver `gh pr view %s --comments`)\n' \
                  "$tentativas" "$existing_pr" "$existing_pr" >> "$task_md"
              fi
            fi
          fi
        else
          marcar_julgamento "harness marcou task.md como concluído mas o gate completo de CI falhou em $worktree (DeepSeek só roda testes escopados)"
        fi
        ;;
      3)
        log "Harness saiu com código 3 (limite/quota da API DeepSeek) em $task_md — tenta de novo na próxima execução."
        ;;
      1)
        abrir_issue_de_bloqueio "$task_md" "$worktree" "Harness saiu com código 1 (teto de iterações, ou seção '## Inconsistências encontradas' no task.md). Ver \`$(dirname "$task_md")/deepseek-run.log\` e o próprio task.md para o motivo detalhado."
        ;;
      *)
        marcar_julgamento "harness saiu com código inesperado ($exit_code) em $task_md"
        ;;
    esac
  done
fi

# --- 2. Autorrevisão para PRs abertos que ainda não têm marcador ---------
# (cobre PRs abertos antes desta versão do orquestrador rodar, ou por
# execução manual da Fase 3.)

PR_NUMBERS="$(gh pr list --state open --json number --jq '.[].number' 2>/dev/null || true)"

for PR_NUM in $PR_NUMBERS; do
  MARCADOR_EXISTENTE="$(gh pr view "$PR_NUM" --json comments \
    --jq '[.comments[] | select(.body | test("<!-- dev-review:status="))] | last | .body' 2>/dev/null || true)"
  if [ -z "$MARCADOR_EXISTENTE" ] || [ "$MARCADOR_EXISTENTE" = "null" ]; then
    log "PR #$PR_NUM sem marcador de revisão ainda — rodando autorrevisão pela DeepSeek."
    scripts/deepseek-review.sh "$PR_NUM" || true
  fi
done

# --- 3. Mergear PRs já aprovados (por dev-review OU deepseek-review) ----

for PR_NUM in $PR_NUMBERS; do
  MARCADOR="$(gh pr view "$PR_NUM" --json comments \
    --jq '[.comments[] | select(.body | test("<!-- dev-review:status="))] | last | .body' 2>/dev/null || true)"

  if [ -z "$MARCADOR" ] || [ "$MARCADOR" = "null" ]; then
    marcar_julgamento "PR #$PR_NUM aberto sem marcador de revisão ainda"
    continue
  fi

  if ! echo "$MARCADOR" | grep -q "dev-review:status=aprovado"; then
    log "PR #$PR_NUM com mudanças solicitadas — tratado no bloco de autorrevisão/bloqueio acima, não mergeando."
    continue
  fi

  CHECKS_OK="$(gh pr checks "$PR_NUM" --json state --jq \
    '[.[] | select(.state != "SUCCESS" and .state != "SKIPPED")] | length == 0' 2>/dev/null || echo "false")"
  if [ "$CHECKS_OK" != "true" ]; then
    log "PR #$PR_NUM aprovado, mas CI não está toda verde ainda — aguardando."
    continue
  fi

  log "PR #$PR_NUM: aprovado + CI verde. Fazendo squash-merge."
  if gh pr merge "$PR_NUM" --squash --delete-branch; then
    log "PR #$PR_NUM mergeado."
  else
    marcar_julgamento "squash-merge do PR #$PR_NUM falhou (conflito ou proteção de branch)"
  fi
done

# --- 4. Limpar worktrees cujo PR já foi mergeado -------------------------

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

# --- 5. Fechar epics cujas sub-issues já estejam todas fechadas ----------

EPIC_NUMBERS="$(gh issue list --label epic --state open --json number --jq '.[].number' 2>/dev/null || true)"
for EPIC_NUM in $EPIC_NUMBERS; do
  SUB_STATES="$(gh api "repos/RafaVargas1/Synclass/issues/$EPIC_NUM/sub_issues" --jq '.[].state' 2>/dev/null || true)"
  if [ -z "$SUB_STATES" ]; then
    continue
  fi
  if echo "$SUB_STATES" | grep -qv '^closed$'; then
    continue
  fi
  log "Epic #$EPIC_NUM: todas as sub-issues fechadas — fechando o epic."
  gh issue close "$EPIC_NUM" --comment "Fechando automaticamente (pipeline-orchestrator.sh, ADR-0004): todas as sub-issues conhecidas já estão fechadas." \
    || marcar_julgamento "epic #$EPIC_NUM tinha todas as sub-issues fechadas mas 'gh issue close' falhou"
done

echo "PIPELINE_ORCHESTRATOR_RESULT: judgment_needed=${JUDGMENT_NEEDED} reason=\"${JUDGMENT_REASON}\""
