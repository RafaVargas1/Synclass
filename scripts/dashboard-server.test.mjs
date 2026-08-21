/**
 * Testes do servidor local do dashboard (scripts/dashboard-server.mjs).
 * Usa o runner nativo node:test — sem dependência externa.
 *
 * Faz o módulo subir numa porta efêmera (DASHBOARD_PORT=0) para que o
 * bind de importação do módulo — a mesma chamada `server.listen` que roda
 * em produção — seja exercitado de verdade, em vez de o teste imitar
 * `listen` à mão num segundo momento (o que não exercitaria o host
 * escolhido no módulo).
 */
import { test } from 'node:test';
import assert from 'node:assert/strict';

process.env.DASHBOARD_PORT = '0';

const { server } = await import('./dashboard-server.mjs');

test('servidor escuta somente na interface loopback (127.0.0.1 / ::1)', async () => {
  // O bind acontece de forma assíncrona após o listen de importação do
  // módulo; espera o evento de listening se o socket ainda não subiu.
  await new Promise((resolve, reject) => {
    if (server.address()) {
      resolve();
      return;
    }
    server.once('listening', resolve);
    server.once('error', reject);
  });

  // `finally` é obrigatório aqui: se a asserção abaixo falhar (fase
  // vermelha do TDD, antes da correção), o `assert.ok` lança e pula
  // qualquer `server.close()` que viesse só depois dele — o socket de
  // escuta fica aberto e o processo do `node --test` nunca termina
  // sozinho (mantém o event loop vivo). Foi exatamente isso que travou
  // duas rodadas do harness DeepSeek nesta Task: o teste falhava
  // corretamente, mas o runner nunca saía.
  try {
    const endereco = server.address();
    assert.ok(endereco, 'esperava um endereço de bind');
    const host = endereco.address;

    assert.ok(
      host === '127.0.0.1' || host === '::1',
      `esperava bind somente em loopback (127.0.0.1 ou ::1), mas o servidor escutou em "${host}"`
    );
  } finally {
    await new Promise((resolve) => server.close(resolve));
  }
});
