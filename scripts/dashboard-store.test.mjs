/**
 * Testes do store multi-execução do dashboard (scripts/dashboard-store.mjs).
 * Usa o runner nativo node:test — sem dependência externa.
 */
import { test } from 'node:test';
import assert from 'node:assert/strict';

import { ingerir, pegarEstado, inscreverOuvinte, zerarTudo } from './dashboard-store.mjs';

test.beforeEach(() => {
  zerarTudo();
});

test('ingerir execution.started cria execução ativa', () => {
  ingerir({ type: 'execution.started', executionId: 'e1', feature: 'Login', taskPath: 'docs/specs/1/task.md' });

  const estado = pegarEstado();
  assert.equal(estado.execucoesAtivas.length, 1);
  assert.equal(estado.execucoesAtivas[0].id, 'e1');
  assert.equal(estado.execucoesAtivas[0].feature, 'Login');
  assert.equal(estado.execucoesAtivas[0].status, 'running');
});

test('duas execuções concorrentes aparecem as duas ao mesmo tempo', () => {
  ingerir({ type: 'execution.started', executionId: 'e1', feature: 'Login', cwd: '/wt-a' });
  ingerir({ type: 'execution.started', executionId: 'e2', feature: 'Horários', cwd: '/wt-b' });

  const estado = pegarEstado();
  assert.equal(estado.execucoesAtivas.length, 2);
  const ids = estado.execucoesAtivas.map((e) => e.id).sort();
  assert.deepEqual(ids, ['e1', 'e2']);
});

test('stage.started/stage.finished registra duração por execução', () => {
  ingerir({ type: 'execution.started', executionId: 'e1' });
  ingerir({ type: 'stage.started', executionId: 'e1', nome: 'Implementation (DeepSeek)', model: 'deepseek-chat' });

  let estado = pegarEstado();
  assert.equal(estado.execucoesAtivas[0].currentStage, 'Implementation (DeepSeek)');
  assert.equal(estado.execucoesAtivas[0].currentModel, 'deepseek-chat');

  ingerir({ type: 'stage.finished', executionId: 'e1', nome: 'Implementation (DeepSeek)', status: 'completed' });

  estado = pegarEstado();
  const etapa = estado.execucoesAtivas[0].stages[0];
  assert.equal(etapa.status, 'completed');
  assert.ok(etapa.duration >= 0);
});

test('state.updated atualiza tool/status/model sem afetar outras execuções', () => {
  ingerir({ type: 'execution.started', executionId: 'e1' });
  ingerir({ type: 'execution.started', executionId: 'e2' });

  ingerir({ type: 'state.updated', executionId: 'e1', currentTool: 'write_file', currentStatus: 'running' });

  const estado = pegarEstado();
  const e1 = estado.execucoesAtivas.find((e) => e.id === 'e1');
  const e2 = estado.execucoesAtivas.find((e) => e.id === 'e2');
  assert.equal(e1.currentTool, 'write_file');
  assert.equal(e2.currentTool, null);
});

test('event anexa ao histórico de eventos da execução certa', () => {
  ingerir({ type: 'execution.started', executionId: 'e1' });
  ingerir({ type: 'execution.started', executionId: 'e2' });

  ingerir({ type: 'event', executionId: 'e1', evento: { type: 'tool.call', metadata: { tool: 'read_file' } } });

  const estado = pegarEstado();
  const e1 = estado.execucoesAtivas.find((e) => e.id === 'e1');
  const e2 = estado.execucoesAtivas.find((e) => e.id === 'e2');
  assert.equal(e1.events.length, 1);
  assert.equal(e1.events[0].type, 'tool.call');
  assert.equal(e2.events.length, 0);
});

test('execution.finished move a execução pro histórico e some das ativas', () => {
  ingerir({ type: 'execution.started', executionId: 'e1', feature: 'sucesso' });
  ingerir({ type: 'execution.finished', executionId: 'e1', status: 'success' });

  const estado = pegarEstado();
  assert.equal(estado.execucoesAtivas.length, 0);
  assert.equal(estado.historico.length, 1);
  assert.equal(estado.historico[0].status, 'success');
  assert.ok(estado.historico[0].finishedAt);
  assert.ok(estado.historico[0].elapsedMs >= 0);
});

test('histórico mantém no máximo 20 execuções', () => {
  for (let i = 0; i < 25; i++) {
    ingerir({ type: 'execution.started', executionId: `e${i}`, feature: `exec-${i}` });
    ingerir({ type: 'execution.finished', executionId: `e${i}`, status: 'success' });
  }

  const estado = pegarEstado();
  assert.equal(estado.historico.length, 20);
  assert.equal(estado.historico[0].feature, 'exec-24');
});

test('ingerir sem executionId é ignorado sem lançar', () => {
  assert.doesNotThrow(() => ingerir({ type: 'execution.started', feature: 'sem id' }));
  assert.equal(pegarEstado().execucoesAtivas.length, 0);
});

test('ingerir tipo desconhecido é ignorado sem lançar', () => {
  ingerir({ type: 'execution.started', executionId: 'e1' });
  assert.doesNotThrow(() => ingerir({ type: 'algo.inventado', executionId: 'e1' }));
});

test('ouvintes recebem snapshot em mudança de estado e event por execução', () => {
  const recebidos = [];
  inscreverOuvinte((payload) => recebidos.push(payload));

  ingerir({ type: 'execution.started', executionId: 'e1' });
  ingerir({ type: 'event', executionId: 'e1', evento: { type: 'tool.call' } });

  assert.ok(recebidos.some((p) => p.type === 'snapshot'));
  assert.ok(recebidos.some((p) => p.type === 'event' && p.executionId === 'e1'));

  const remover = inscreverOuvinte(() => {});
  remover();
});

test('secrets nunca estão no estado', () => {
  ingerir({ type: 'execution.started', executionId: 'e1' });
  ingerir({ type: 'event', executionId: 'e1', evento: { type: 'api.response.received', metadata: { ok: true } } });

  const serializado = JSON.stringify(pegarEstado());
  assert.ok(!serializado.includes('DEEPSEEK_API_KEY'));
  assert.ok(!serializado.includes('sk-'));
});
