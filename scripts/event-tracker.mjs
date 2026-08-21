/**
 * Event tracker — estado e eventos da execução do pipeline, compartilhados
 * entre o dashboard (scripts/dashboard-server.mjs) e o harness
 * (scripts/deepseek-agent.mjs).
 *
 * Mantém em memória: execução atual (ou última, quando IDLE), estágios e
 * fila de eventos. Nada de secrets, prompts completos ou respostas
 * completas — apenas metadados de observabilidade.
 */

let execucaoAtual = null;
let ultimasExecucoes = [];
const MAX_HISTORICO = 10;
const ouvintes = new Set();

const ETAPAS_PIPELINE = [
  'GitHub Issue',
  'Refinement (Claude)',
  'Task Specification (DeepSeek)',
  'Implementation (DeepSeek)',
  'Tests',
  'QA / Playwright',
  'Review',
  'Completed',
];

function agora() {
  return new Date().toISOString();
}

function registrarEventoInterno(type, stage, provider, model, metadata = {}) {
  const evento = {
    timestamp: agora(),
    type,
    stage: stage ?? execucaoAtual?.currentStage ?? null,
    provider: provider ?? execucaoAtual?.currentModel ?? null,
    model: model ?? execucaoAtual?.currentModel ?? null,
    metadata,
  };
  if (execucaoAtual) {
    execucaoAtual.events.push(evento);
    if (execucaoAtual.events.length > 2000) execucaoAtual.events.shift();
  }
  emitirParaOuvintes({ type: 'event', evento });
  emitirParaOuvintes({ type: 'state' });
  return evento;
}

function novaExecucao({ feature, taskPath, cwd } = {}) {
  finalizarExecucaoAtualSeExistir('aborted');

  execucaoAtual = {
    status: 'running',
    feature: feature ?? null,
    taskPath: taskPath ?? null,
    cwd: cwd ?? null,
    startedAt: agora(),
    finishedAt: null,
    currentStage: null,
    currentTool: null,
    currentStatus: 'starting',
    currentModel: null,
    elapsedMs: null,
    stages: [],
    events: [],
  };

  registrarEventoInterno('execution.started', null, null, null, { taskPath, cwd });
  return execucaoAtual;
}

function finalizarExecucaoAtualSeExistir(status) {
  if (execucaoAtual && execucaoAtual.status === 'running') {
    execucaoAtual.status = status;
    execucaoAtual.finishedAt = agora();
    execucaoAtual.elapsedMs = Date.parse(execucaoAtual.finishedAt) - Date.parse(execucaoAtual.startedAt);
    ultimasExecucoes.unshift({ ...execucaoAtual, events: [...execucaoAtual.events] });
    if (ultimasExecucoes.length > MAX_HISTORICO) {
      ultimasExecucoes.pop();
    }
  }
}

function concluirExecucao({ status = 'success', error = null } = {}) {
  if (!execucaoAtual) return null;
  const stageAtual = execucaoAtual.currentStage;

  // Registra o evento final antes de arquivar — assim ele entra no
  // histórico da última execução.
  if (error) {
    registrarEventoInterno('execution.error', stageAtual, null, null, { status, error });
  } else {
    registrarEventoInterno('execution.finished', stageAtual, null, null, { status });
  }

  finalizarExecucaoAtualSeExistir(status);
  execucaoAtual = null;
  emitirParaOuvintes({ type: 'state' });
  return ultimasExecucoes[0] ?? null;
}

function iniciarEtapa(nome, provider, model) {
  if (!execucaoAtual) return null;

  const etapaAnterior = execucaoAtual.stages[execucaoAtual.stages.length - 1];
  if (etapaAnterior && etapaAnterior.status === 'running') {
    etapaAnterior.status = 'completed';
    etapaAnterior.finishedAt = agora();
    etapaAnterior.duration = Date.parse(etapaAnterior.finishedAt) - Date.parse(etapaAnterior.startedAt);
  }

  const etapa = {
    nome,
    provider: provider ?? null,
    model: model ?? null,
    status: 'running',
    startedAt: agora(),
    finishedAt: null,
    duration: null,
    error: null,
  };

  execucaoAtual.stages.push(etapa);
  execucaoAtual.currentStage = nome;
  execucaoAtual.currentStatus = 'running';
  execucaoAtual.currentModel = model ?? null;
  execucaoAtual.currentTool = null;

  registrarEventoInterno('stage.started', nome, provider, model);
  return etapa;
}

function concluirEtapa(nome, { status = 'completed', error = null } = {}) {
  if (!execucaoAtual) return null;
  const etapa = execucaoAtual.stages.find((e) => e.nome === nome && e.status === 'running');
  if (!etapa) return null;
  etapa.status = status;
  etapa.finishedAt = agora();
  etapa.duration = Date.parse(etapa.finishedAt) - Date.parse(etapa.startedAt);
  if (error) etapa.error = error;
  registrarEventoInterno('stage.finished', nome, etapa.provider, etapa.model, { status, duration: etapa.duration });
  return etapa;
}

function emitirEvento({ type, stage = null, provider = null, model = null, metadata = {} } = {}) {
  if (execucaoAtual) {
    return registrarEventoInterno(type, stage, provider, model, metadata);
  }
  return null;
}

function atualizarEstadoAtual({ status = null, currentTool = null, model = null, currentStatus = null } = {}) {
  if (!execucaoAtual) return null;
  if (status) execucaoAtual.status = status;
  if (currentTool !== null) execucaoAtual.currentTool = currentTool;
  if (model !== null) execucaoAtual.currentModel = model;
  if (currentStatus !== null) execucaoAtual.currentStatus = currentStatus;
  emitirParaOuvintes({ type: 'state' });
  return execucaoAtual;
}

function pegarEstado() {
  if (execucaoAtual) {
    execucaoAtual.elapsedMs = Date.now() - Date.parse(execucaoAtual.startedAt);
  }
  return {
    execution: execucaoAtual,
    pipelineStatus: execucaoAtual ? execucaoAtual.status : 'idle',
    últimaExecucao: ultimasExecucoes[0] ?? null,
    historico: ultimasExecucoes.slice(0, MAX_HISTORICO),
    etapasPipeline: ETAPAS_PIPELINE,
  };
}

function inscreverOuvinte(fn) {
  ouvintes.add(fn);
  return () => ouvintes.delete(fn);
}

function emitirParaOuvintes(payload) {
  for (const fn of ouvintes) {
    try {
      fn(payload);
    } catch {
      // nunca deixa ouvinte quebrar o tracker
    }
  }
}

function zerarTudo() {
  finalizarExecucaoAtualSeExistir('aborted');
  execucaoAtual = null;
  ultimasExecucoes = [];
  ouvintes.clear();
}

export {
  novaExecucao,
  concluirExecucao,
  iniciarEtapa,
  concluirEtapa,
  emitirEvento,
  atualizarEstadoAtual,
  pegarEstado,
  inscreverOuvinte,
  zerarTudo,
  ETAPAS_PIPELINE,
};
