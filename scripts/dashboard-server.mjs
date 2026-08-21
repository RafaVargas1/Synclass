#!/usr/bin/env node
/**
 * Dashboard local de observabilidade do pipeline Synclass.
 *
 * Serve a página web estática, aceita POST /api/ingest (é assim que cada
 * processo `deepseek-agent.mjs` — potencialmente vários em paralelo, um
 * por worktree — reporta o que está fazendo, ver scripts/event-tracker.mjs
 * `notificarDashboard` e scripts/dashboard-store.mjs) e mantém uma conexão
 * SSE por navegador aberto, transmitindo o estado de todas as execuções
 * ativas em tempo real.
 *
 * Uso:
 *   node scripts/dashboard-server.mjs [porta]
 *   npm run dashboard
 *
 * Só observacional a partir do navegador — a única entrada de escrita é
 * o POST /api/ingest, que só os processos locais do harness conhecem
 * (não expõe nenhum comando, não expõe secrets).
 */

import { createServer } from 'node:http';
import { readFileSync } from 'node:fs';
import { join, dirname, resolve, sep } from 'node:path';
import { fileURLToPath } from 'node:url';
import { ingerir, pegarEstado, inscreverOuvinte } from './dashboard-store.mjs';
import { listarLogsHistoricos, lerCaudaDoLog } from './dashboard-historico.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const RAIZ_DO_REPO = resolve(__dirname, '..');
const PORT = Number(process.argv[2] ?? process.env.DASHBOARD_PORT ?? 8085);
const LIMITE_CORPO_BYTES = 200_000;
const INTERVALO_HEARTBEAT_MS = 10_000;

let paginaHtml = '';
try {
  paginaHtml = readFileSync(join(__dirname, 'dashboard.html'), 'utf8');
} catch {
  paginaHtml = '<!doctype html><html><body><h1>dashboard.html não encontrado</h1></body></html>';
}

const sseClientes = new Set();

function enviarParaCliente(cliente, payload) {
  cliente.write(`data: ${JSON.stringify(payload)}\n\n`);
}

function enviarEstadoCompleto(cliente) {
  enviarParaCliente(cliente, { type: 'snapshot', ...pegarEstado() });
}

function lerCorpoJson(req) {
  return new Promise((resolveCorpo, rejectCorpo) => {
    let bytes = 0;
    const partes = [];
    req.on('data', (chunk) => {
      bytes += chunk.length;
      if (bytes > LIMITE_CORPO_BYTES) {
        rejectCorpo(new Error('corpo grande demais'));
        req.destroy();
        return;
      }
      partes.push(chunk);
    });
    req.on('end', () => {
      try {
        resolveCorpo(partes.length ? JSON.parse(Buffer.concat(partes).toString('utf8')) : {});
      } catch (erro) {
        rejectCorpo(erro);
      }
    });
    req.on('error', rejectCorpo);
  });
}

const server = createServer((req, res) => {
  const url = new URL(req.url ?? '/', `http://localhost:${PORT}`);

  if (url.pathname === '/api/ingest' && req.method === 'POST') {
    lerCorpoJson(req)
      .then((payload) => {
        ingerir(payload);
        res.writeHead(204);
        res.end();
      })
      .catch(() => {
        res.writeHead(400, { 'Content-Type': 'text/plain' });
        res.end('payload inválido');
      });
    return;
  }

  if (url.pathname === '/api/state' && req.method === 'GET') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify(pegarEstado()));
    return;
  }

  if (url.pathname === '/api/logs' && req.method === 'GET') {
    res.writeHead(200, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify(listarLogsHistoricos(RAIZ_DO_REPO)));
    return;
  }

  if (url.pathname === '/api/logs/tail' && req.method === 'GET') {
    const id = url.searchParams.get('id') ?? '';
    const conteudo = lerCaudaDoLog(RAIZ_DO_REPO, id);
    if (conteudo === null) {
      res.writeHead(404, { 'Content-Type': 'text/plain' });
      res.end('log não encontrado');
      return;
    }
    res.writeHead(200, { 'Content-Type': 'text/plain; charset=utf-8' });
    res.end(conteudo);
    return;
  }

  if (url.pathname === '/events') {
    res.writeHead(200, {
      'Content-Type': 'text/event-stream',
      'Cache-Control': 'no-cache',
      Connection: 'keep-alive',
      'Access-Control-Allow-Origin': '*',
    });
    res.write(': connected\n\n');
    enviarEstadoCompleto(res);
    sseClientes.add(res);

    const limpar = () => {
      sseClientes.delete(res);
    };
    res.on('close', limpar);
    res.on('error', limpar);
    return;
  }

  if (url.pathname === '/' || url.pathname === '/index.html') {
    res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
    res.end(paginaHtml);
    return;
  }

  res.writeHead(404, { 'Content-Type': 'text/plain' });
  res.end('Not Found');
});

// Encaminha eventos do store para todos os clientes SSE.
// - { type: 'snapshot' } → busca o estado completo (todas execuções
//   ativas + histórico) e manda pra cada cliente conectado.
// - { type: 'event', executionId, evento } → encaminha direto, o
//   frontend decide em qual card renderizar pelo executionId.
const removerOuvinte = inscreverOuvinte((payload) => {
  if (payload.type === 'snapshot') {
    const snapshot = { type: 'snapshot', ...pegarEstado() };
    for (const cliente of sseClientes) {
      try {
        enviarParaCliente(cliente, snapshot);
      } catch {
        sseClientes.delete(cliente);
      }
    }
    return;
  }
  for (const cliente of sseClientes) {
    try {
      enviarParaCliente(cliente, payload);
    } catch {
      sseClientes.delete(cliente);
    }
  }
});

// Heartbeat: sem isso, o navegador não tem como distinguir "nada
// aconteceu ainda" de "a conexão SSE morreu" — manda um pulso periódico
// pra todo cliente conectado, mesmo sem nenhuma execução ativa.
const heartbeatIntervalId = setInterval(() => {
  const pulso = { type: 'heartbeat', now: new Date().toISOString() };
  for (const cliente of sseClientes) {
    try {
      enviarParaCliente(cliente, pulso);
    } catch {
      sseClientes.delete(cliente);
    }
  }
}, INTERVALO_HEARTBEAT_MS);
heartbeatIntervalId.unref();

// Bind só em loopback: ferramenta interna, só usada localmente por quem
// roda scripts/deepseek-agent.mjs na própria máquina — não precisa estar
// acessível por outros hosts da rede (docs/spec/security-rules.md).
server.listen(PORT, '127.0.0.1', () => {
  console.log(`Dashboard disponível em http://localhost:${PORT}`);
});

server.on('error', (erro) => {
  console.error(`Falha ao subir o dashboard: ${erro.message}`);
  process.exit(1);
});

function parar() {
  clearInterval(heartbeatIntervalId);
  removerOuvinte();
  for (const cliente of sseClientes) {
    try { cliente.end(); } catch { /* ignore */ }
  }
  sseClientes.clear();
  server.close(() => process.exit(0));
}

process.on('SIGINT', parar);
process.on('SIGTERM', parar);

export { server };
