#!/usr/bin/env node
/**
 * Dashboard local de observabilidade do pipeline Synclass.
 *
 * Serve a página web estática e mantém uma conexão SSE para cada navegador
 * aberto, transmitindo eventos do event-tracker em tempo real.
 *
 * Uso:
 *   node scripts/dashboard-server.mjs [porta]
 *
 * Apenas observacional — não aceita nenhum comando, não expõe secrets.
 */

import { createServer } from 'node:http';
import { readFileSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
  pegarEstado,
  inscreverOuvinte,
} from './event-tracker.mjs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const PORT = Number(process.argv[2] ?? process.env.DASHBOARD_PORT ?? 8085);

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

const server = createServer((req, res) => {
  const url = new URL(req.url ?? '/', `http://localhost:${PORT}`);

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

// Encaminha eventos do tracker para todos os clientes SSE.
// - { type: 'event', evento } → encaminha diretamente.
// - { type: 'state' } → envia um snapshot completo (frontend re-renderiza).
const removerOuvinte = inscreverOuvinte((payload) => {
  if (payload.type === 'state') {
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
  // 'event' — encaminha do jeito que veio.
  for (const cliente of sseClientes) {
    try {
      enviarParaCliente(cliente, payload);
    } catch {
      sseClientes.delete(cliente);
    }
  }
});

server.listen(PORT, () => {
  console.log(`Dashboard disponível em http://localhost:${PORT}`);
});

server.on('error', (erro) => {
  console.error(`Falha ao subir o dashboard: ${erro.message}`);
  process.exit(1);
});

function parar() {
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
