/**
 * Histórico persistente de execuções do harness DeepSeek — diferente do
 * `historico` em memória do dashboard-store.mjs (que zera a cada restart
 * do servidor e só cobre execuções que reportaram enquanto o dashboard
 * estava de pé), este módulo lê direto do disco: cada execução do harness
 * já grava seu próprio `deepseek-run.log` ao lado do `task.md`
 * (scripts/deepseek-agent.mjs `registrarNoLog`), então o histórico
 * sobrevive a restart do dashboard e a execuções que rodaram sem o
 * dashboard aberto.
 *
 * Varre o repositório principal e as worktrees irmãs (`../synclass-*`,
 * convenção de nome usada por `CONTRIBUTING.md#branches-e-worktrees`) por
 * `docs/specs/* /deepseek-run.log`, sem depender de `git worktree list`
 * (mais barato, e funciona mesmo se uma worktree já foi removida mas o
 * log ainda existe em disco por algum motivo).
 */

import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, resolve, dirname, basename, sep } from 'node:path';

const NOME_DO_LOG = 'deepseek-run.log';

function raizesCandidatas(raizDoRepo) {
  const paiDoRepo = dirname(resolve(raizDoRepo));
  const raizes = [resolve(raizDoRepo)];
  try {
    for (const entrada of readdirSync(paiDoRepo, { withFileTypes: true })) {
      if (entrada.isDirectory() && entrada.name.startsWith('synclass-')) {
        raizes.push(join(paiDoRepo, entrada.name));
      }
    }
  } catch {
    // paiDoRepo inacessível — segue só com a raiz do repo principal.
  }
  return raizes;
}

function logsEm(raiz) {
  const pastaSpecs = join(raiz, 'docs', 'specs');
  let entradas;
  try {
    entradas = readdirSync(pastaSpecs, { withFileTypes: true });
  } catch {
    return [];
  }
  const logs = [];
  for (const entrada of entradas) {
    if (!entrada.isDirectory()) continue;
    const caminhoLog = join(pastaSpecs, entrada.name, NOME_DO_LOG);
    try {
      const stat = statSync(caminhoLog);
      logs.push({
        slug: entrada.name,
        raiz,
        path: caminhoLog,
        tamanho: stat.size,
        atualizadoEm: stat.mtime.toISOString(),
      });
    } catch {
      // sem deepseek-run.log nesta pasta de spec — não é histórico de execução.
    }
  }
  return logs;
}

/**
 * Lista todos os `deepseek-run.log` encontrados no repo principal e
 * worktrees irmãs, mais recente primeiro. Cada item expõe um `id` opaco
 * (base64 do caminho absoluto) — é o que o cliente manda de volta pra
 * `lerCaudaDoLog`, evitando expor o path bruto em querystring sem
 * necessidade.
 */
function listarLogsHistoricos(raizDoRepo) {
  const raizes = raizesCandidatas(raizDoRepo);
  const logs = raizes.flatMap(logsEm);
  logs.sort((a, b) => b.atualizadoEm.localeCompare(a.atualizadoEm));
  return logs.map((log) => ({
    id: Buffer.from(log.path).toString('base64url'),
    slug: log.slug,
    worktree: basename(log.raiz),
    tamanho: log.tamanho,
    atualizadoEm: log.atualizadoEm,
  }));
}

/**
 * Lê as últimas `linhas` de um log, validando que o `id` resolve pra um
 * caminho de fato dentro do repo principal ou de uma worktree irmã — o
 * endpoint HTTP não deve virar um jeito de ler arquivo arbitrário do disco
 * (docs/spec/security-rules.md).
 */
function lerCaudaDoLog(raizDoRepo, id, linhas = 300) {
  let caminho;
  try {
    caminho = Buffer.from(id, 'base64url').toString('utf8');
  } catch {
    return null;
  }
  const raizesPermitidas = raizesCandidatas(raizDoRepo).map((r) => resolve(r) + sep);
  const caminhoResolvido = resolve(caminho);
  const dentroDeRaizPermitida = raizesPermitidas.some((raiz) => caminhoResolvido.startsWith(raiz));
  if (!dentroDeRaizPermitida || basename(caminhoResolvido) !== NOME_DO_LOG) {
    return null;
  }
  try {
    const conteudo = readFileSync(caminhoResolvido, 'utf8');
    const todasAsLinhas = conteudo.split('\n');
    return todasAsLinhas.slice(-linhas).join('\n');
  } catch {
    return null;
  }
}

export { listarLogsHistoricos, lerCaudaDoLog };
