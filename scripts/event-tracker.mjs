/**
 * Event tracker — cliente usado pelo harness (scripts/deepseek-agent.mjs).
 *
 * Mantém estado local (execução atual, histórico, fila de eventos) pra
 * funcionar sozinho mesmo sem nenhum dashboard rodando — nada aqui depende
 * de rede pra operar. Além disso, cada mudança de estado é também
 * reportada via HTTP POST fire-and-forget pra
 * scripts/dashboard-server.mjs (se estiver rodando em algum terminal),
 * que mantém o estado real de N execuções concorrentes (uma por
 * worktree/Task rodando em paralelo — ver scripts/dashboard-store.mjs).
 *
 * Isso é client + servidor rodando em processos `node` separados: o
 * harness não sabe (nem precisa saber) se alguém está olhando o
 * dashboard. Se `notificarDashboard` falhar (dashboard não está de pé,
 * porta ocupada por outra coisa, etc.) o erro é engolido silenciosamente
 * — isso nunca pode atrasar nem quebrar uma execução do harness.
 *
 * Nada de secrets, prompts completos ou respostas completas — apenas
 * metadados de observabilidade.
 */

import { randomUUID } from 'node:crypto';
import { hostname } from 'node:os';

let execucaoAtual = null;
let ultimasExecucoes = [];
const MAX_HISTORICO = 10;
const ouvintes = new Set();

const DASHBOARD_PORT = Number(process.env.DASHBOARD_PORT ?? 8085);
const DASHBOARD_URL = `http://localhost:${DASHBOARD_PORT}/api/ingest`;
const TIMEOUT_NOTIFICACAO_MS = 800;

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

/**
 * POST fire-and-forget pro dashboard-server, se ele estiver de pé. Nunca
 * `await`ado pelos chamadores (ver cada função abaixo) — não pode
 * adicionar latência real ao harness só porque alguém abriu o dashboard.
 * `AbortSignal.timeout` evita que uma porta que aceita conexão mas nunca
 * responde prenda o processo até o Node encerrar.
 */
function notificarDashboard(payload) {
  fetch(DASHBOARD_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload),
    signal: AbortSignal.timeout(TIMEOUT_NOTIFICACAO_MS),
  }).catch(() => {
    // Dashboard não está rodando, ou não está acessível — sem problema,
    // o tracker local continua funcionando normalmente.
  });
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
    notificarDashboard({ type: 'event', executionId: execucaoAtual.id, evento });
  }
  emitirParaOuvintes({ type: 'event', evento });
  emitirParaOuvintes({ type: 'state' });
  return evento;
}

function novaExecucao({ feature, taskPath, cwd } = {}) {
  finalizarExecucaoAtualSeExistir('aborted');

  const id = randomUUID();
  execucaoAtual = {
    id,
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

  notificarDashboard({
    type: 'execution.started',
    executionId: id,
    feature: feature ?? null,
    taskPath: taskPath ?? null,
    cwd: cwd ?? process.cwd(),
    pid: process.pid,
    hostname: hostname(),
  });
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
  const id = execucaoAtual.id;

  // Registra o evento final antes de arquivar — assim ele entra no
  // histórico da última execução.
  if (error) {
    registrarEventoInterno('execution.error', stageAtual, null, null, { status, error });
  } else {
    registrarEventoInterno('execution.finished', stageAtual, null, null, { status });
  }

  finalizarExecucaoAtualSeExistir(status);
  execucaoAtual = null;
  notificarDashboard({ type: 'execution.finished', executionId: id, status });
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

  notificarDashboard({ type: 'stage.started', executionId: execucaoAtual.id, nome, provider, model });
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
  notificarDashboard({ type: 'stage.finished', executionId: execucaoAtual.id, nome, status, error });
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
  notificarDashboard({
    type: 'state.updated',
    executionId: execucaoAtual.id,
    currentTool: execucaoAtual.currentTool,
    currentStatus: execucaoAtual.currentStatus,
    model: execucaoAtual.currentModel,
  });
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
