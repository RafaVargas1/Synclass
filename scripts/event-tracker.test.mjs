/**
 * Testes do event tracker (scripts/event-tracker.mjs). Usa o runner nativo
 * node:test — sem dependência externa.
 */
import { test } from 'node:test';
import assert from 'node:assert/strict';

import {
  novaExecucao,
  concluirExecucao,
  iniciarEtapa,
  concluirEtapa,
  emitirEvento,
  atualizarEstadoAtual,
  pegarEstado,
  inscreverOuvinte,
  zerarTudo,
} from './event-tracker.mjs';

test.beforeEach(() => {
  zerarTudo();
});

test('novaExecucao cria execução running com startedAt', () => {
  const exec = novaExecucao({ feature: 'Login do professor', taskPath: 'docs/specs/1-login/task.md' });

  assert.equal(exec.status, 'running');
  assert.equal(exec.feature, 'Login do professor');
  assert.equal(exec.taskPath, 'docs/specs/1-login/task.md');
  assert.ok(exec.startedAt);
  assert.ok(exec.stages.length === 0);
  // Execution.started é o primeiro evento registrado.
  assert.ok(exec.events.length === 1);
  assert.equal(exec.events[0].type, 'execution.started');

  const estado = pegarEstado();
  assert.equal(estado.pipelineStatus, 'running');
  assert.equal(estado.execution, exec);
});

test('pegarEstado sem execução retorna idle', () => {
  const estado = pegarEstado();
  assert.equal(estado.pipelineStatus, 'idle');
  assert.equal(estado.execution, null);
});

test('iniciarEtapa + concluirEtapa registra duração', () => {
  novaExecucao({ feature: 'teste' });
  iniciarEtapa('Implementation (DeepSeek)', 'DeepSeek', 'deepseek-chat');

  const estado = pegarEstado();
  assert.equal(estado.execution.currentStage, 'Implementation (DeepSeek)');
  assert.equal(estado.execution.currentStatus, 'running');
  assert.equal(estado.execution.currentModel, 'deepseek-chat');

  concluirEtapa('Implementation (DeepSeek)', { status: 'completed' });

  const etapa = pegarEstado().execution.stages[0];
  assert.equal(etapa.status, 'completed');
  assert.ok(etapa.finishedAt);
  assert.ok(etapa.duration >= 0);
});

test('concluirEtapa com erro registra a falha', () => {
  novaExecucao({ feature: 'falha' });
  iniciarEtapa('Tests');
  concluirEtapa('Tests', { status: 'failed', error: 'causa da falha' });

  const etapa = pegarEstado().execution.stages[0];
  assert.equal(etapa.status, 'failed');
  assert.equal(etapa.error, 'causa da falha');
});

test('emitirEvento adiciona eventos à execução atual', () => {
  novaExecucao({ feature: 'loop' });
  iniciarEtapa('Implementation (DeepSeek)', 'DeepSeek', 'deepseek-chat');

  emitirEvento({ type: 'api.request.started', metadata: { iteracao: 1 } });
  emitirEvento({ type: 'tool.call', metadata: { tool: 'read_file', path: 'src/x.ts' } });

  const exec = pegarEstado().execution;
  // execution.started + stage.started + 2 emitirEvento = 4 eventos
  const evts = exec.events;
  assert.equal(evts.length, 4);
  assert.equal(evts[2].type, 'api.request.started');
  assert.equal(evts[2].metadata.iteracao, 1);
  assert.equal(evts[2].stage, 'Implementation (DeepSeek)');
  assert.equal(evts[3].type, 'tool.call');
  assert.equal(evts[3].metadata.tool, 'read_file');
});

test('atualizarEstadoAtual atualiza tool/status/model', () => {
  novaExecucao({ feature: 'estado' });
  iniciarEtapa('Implementation (DeepSeek)', 'DeepSeek', 'deepseek-chat');

  atualizarEstadoAtual({ currentTool: 'write_file', currentStatus: 'waiting_api', model: 'deepseek-coder' });

  const estado = pegarEstado();
  assert.equal(estado.execution.currentTool, 'write_file');
  assert.equal(estado.execution.currentStatus, 'waiting_api');
  assert.equal(estado.execution.currentModel, 'deepseek-coder');
});

test('concluirExecucao success move para histórico e fica idle', () => {
  novaExecucao({ feature: 'sucesso' });
  iniciarEtapa('Implementation (DeepSeek)', 'DeepSeek');
  concluirEtapa('Implementation (DeepSeek)', { status: 'completed' });

  const resultado = concluirExecucao({ status: 'success' });

  const estado = pegarEstado();
  assert.equal(estado.pipelineStatus, 'idle');
  assert.equal(estado.execution, null);
  assert.ok(estado.últimaExecucao);
  assert.equal(estado.últimaExecucao.status, 'success');
  assert.ok(estado.últimaExecucao.finishedAt);
  assert.ok(estado.últimaExecucao.elapsedMs >= 0);
  // O evento execution.finished deve estar no histórico.
  assert.ok(estado.últimaExecucao.events.some((e) => e.type === 'execution.finished'));
});

test('concluirExecucao failed registra erro e histórico', () => {
  novaExecucao({ feature: 'falha total' });
  iniciarEtapa('Tests');

  concluirExecucao({ status: 'failed', error: 'erro total' });

  assert.equal(pegarEstado().últimaExecucao.status, 'failed');
  assert.equal(pegarEstado().últimaExecucao.events.some((e) => e.type === 'execution.error'), true);
});

test('ouvintes recebem eventos de state e event', async () => {
  const eventos = [];
  inscreverOuvinte((payload) => eventos.push(payload));

  novaExecucao({ feature: 'ouvinte' });
  emitirEvento({ type: 'tool.call', metadata: { tool: 'run_command' } });

  // listener recebe { type: 'event', evento: {...} } e { type: 'state' }
  assert.ok(eventos.some((e) => e.type === 'event' && e.evento?.type === 'execution.started'));
  assert.ok(eventos.some((e) => e.type === 'state'));
  assert.ok(eventos.some((e) => e.type === 'event' && e.evento?.type === 'tool.call'));

  const sub = inscreverOuvinte(() => {});
  sub(); // remove — deve parar de receber
});

test('histórico mantém até MAX_HISTORICO execuções', () => {
  for (let i = 0; i < 15; i++) {
    novaExecucao({ feature: `exec-${i}` });
    concluirExecucao({ status: 'success' });
  }

  const estado = pegarEstado();
  assert.ok(estado.historico.length <= 10);
  assert.equal(estado.historico[0].feature, 'exec-14');
});

test('novaExecucao aborta execução anterior ainda rodando', () => {
  novaExecucao({ feature: 'antiga' });
  novaExecucao({ feature: 'nova' });

  const estado = pegarEstado();
  assert.equal(estado.últimaExecucao.status, 'aborted');
  assert.equal(estado.execution.feature, 'nova');
});

test('secrets nunca estão nos eventos', () => {
  novaExecucao({ feature: 'secretos' });
  iniciarEtapa('Implementation (DeepSeek)', 'DeepSeek');

  emitirEvento({ type: 'api.response.received', metadata: { ok: true } });

  const serializado = JSON.stringify(pegarEstado());
  assert.ok(!serializado.includes('DEEPSEEK_API_KEY'));
  assert.ok(!serializado.includes('sk-'));
});
