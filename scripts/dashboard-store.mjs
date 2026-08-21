/**
 * Store de observabilidade do dashboard — roda só dentro do processo
 * `dashboard-server.mjs`, nunca importado por `deepseek-agent.mjs`
 * diretamente (esse é o erro que existia antes: `event-tracker.mjs`
 * mantinha estado em memória de um jeito que só fazia sentido se harness
 * e dashboard rodassem no mesmo processo — nunca rodam, o harness é
 * disparado como `node scripts/deepseek-agent.mjs` separado, um processo
 * por Task/worktree, inclusive em paralelo via
 * scripts/pipeline-orchestrator.sh).
 *
 * Aqui a fonte da verdade é multi-execução: um `Map<executionId, execução>`
 * — cada processo `deepseek-agent.mjs` gera seu próprio `executionId` e
 * reporta pra cá via HTTP POST (`event-tracker.mjs` faz esse POST,
 * fire-and-forget, ver `notificarDashboard`). N execuções concorrentes
 * (N worktrees em paralelo) aparecem todas ao mesmo tempo.
 */

const execucoes = new Map();
let historico = [];
const MAX_HISTORICO = 20;
const MAX_EVENTOS_POR_EXECUCAO = 500;
const ouvintes = new Set();

function agora() {
  return new Date().toISOString();
}

function emitirParaOuvintes(payload) {
  for (const fn of ouvintes) {
    try {
      fn(payload);
    } catch {
      // nunca deixa ouvinte quebrar o store
    }
  }
}

function inscreverOuvinte(fn) {
  ouvintes.add(fn);
  return () => ouvintes.delete(fn);
}

/**
 * Ponto único de entrada — recebe o payload que `event-tracker.mjs`
 * (cliente) posta via HTTP. `type` decide a operação; o resto dos campos
 * varia por tipo (ver `notificarDashboard` no cliente para o formato
 * exato de cada um).
 */
function ingerir(payload) {
  const { type, executionId } = payload;
  if (!executionId) return;

  switch (type) {
    case 'execution.started':
      return iniciarExecucao(executionId, payload);
    case 'execution.finished':
      return finalizarExecucao(executionId, payload);
    case 'stage.started':
      return iniciarEtapa(executionId, payload);
    case 'stage.finished':
      return concluirEtapa(executionId, payload);
    case 'state.updated':
      return atualizarEstado(executionId, payload);
    case 'event':
      return registrarEvento(executionId, payload);
    default:
      return null;
  }
}

function iniciarExecucao(executionId, { feature, taskPath, cwd, pid, hostname }) {
  const execucao = {
    id: executionId,
    status: 'running',
    feature: feature ?? null,
    taskPath: taskPath ?? null,
    cwd: cwd ?? null,
    pid: pid ?? null,
    hostname: hostname ?? null,
    startedAt: agora(),
    finishedAt: null,
    elapsedMs: null,
    currentStage: null,
    currentTool: null,
    currentStatus: 'starting',
    currentModel: null,
    stages: [],
    events: [],
  };
  execucoes.set(executionId, execucao);
  emitirParaOuvintes({ type: 'snapshot' });
  return execucao;
}

function pegarOuIgnorar(executionId) {
  return execucoes.get(executionId) ?? null;
}

function finalizarExecucao(executionId, { status = 'success' } = {}) {
  const execucao = pegarOuIgnorar(executionId);
  if (!execucao) return null;

  execucao.status = status;
  execucao.finishedAt = agora();
  execucao.elapsedMs = Date.parse(execucao.finishedAt) - Date.parse(execucao.startedAt);
  execucao.currentTool = null;
  execucao.currentStatus = status;

  execucoes.delete(executionId);
  historico.unshift(execucao);
  if (historico.length > MAX_HISTORICO) historico.pop();

  emitirParaOuvintes({ type: 'snapshot' });
  return execucao;
}

function iniciarEtapa(executionId, { nome, provider, model }) {
  const execucao = pegarOuIgnorar(executionId);
  if (!execucao) return null;

  const anterior = execucao.stages[execucao.stages.length - 1];
  if (anterior && anterior.status === 'running') {
    anterior.status = 'completed';
    anterior.finishedAt = agora();
    anterior.duration = Date.parse(anterior.finishedAt) - Date.parse(anterior.startedAt);
  }

  execucao.stages.push({
    nome,
    provider: provider ?? null,
    model: model ?? null,
    status: 'running',
    startedAt: agora(),
    finishedAt: null,
    duration: null,
    error: null,
  });
  execucao.currentStage = nome;
  execucao.currentStatus = 'running';
  execucao.currentModel = model ?? null;
  emitirParaOuvintes({ type: 'snapshot' });
  return execucao;
}

function concluirEtapa(executionId, { nome, status = 'completed', error = null } = {}) {
  const execucao = pegarOuIgnorar(executionId);
  if (!execucao) return null;
  const etapa = execucao.stages.find((e) => e.nome === nome && e.status === 'running');
  if (!etapa) return null;
  etapa.status = status;
  etapa.finishedAt = agora();
  etapa.duration = Date.parse(etapa.finishedAt) - Date.parse(etapa.startedAt);
  if (error) etapa.error = error;
  emitirParaOuvintes({ type: 'snapshot' });
  return etapa;
}

function atualizarEstado(executionId, { currentTool, currentStatus, model } = {}) {
  const execucao = pegarOuIgnorar(executionId);
  if (!execucao) return null;
  if (currentTool !== undefined) execucao.currentTool = currentTool;
  if (currentStatus !== undefined) execucao.currentStatus = currentStatus;
  if (model !== undefined) execucao.currentModel = model;
  emitirParaOuvintes({ type: 'snapshot' });
  return execucao;
}

function registrarEvento(executionId, { evento }) {
  const execucao = pegarOuIgnorar(executionId);
  if (!execucao || !evento) return null;
  execucao.events.push(evento);
  if (execucao.events.length > MAX_EVENTOS_POR_EXECUCAO) execucao.events.shift();
  emitirParaOuvintes({ type: 'event', executionId, evento });
  return evento;
}

function pegarEstado() {
  const ativas = [...execucoes.values()].map((exec) => ({
    ...exec,
    elapsedMs: Date.now() - Date.parse(exec.startedAt),
  }));
  return {
    execucoesAtivas: ativas,
    historico: historico.slice(0, MAX_HISTORICO),
  };
}

function zerarTudo() {
  execucoes.clear();
  historico = [];
  ouvintes.clear();
}

export { ingerir, pegarEstado, inscreverOuvinte, zerarTudo };
