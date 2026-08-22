#!/usr/bin/env node
/**
 * Harness de implementação via DeepSeek (ADR-0001,
 * docs/spec/decisions/ADR-0001-pipeline-claude-deepseek.md): dá à
 * DeepSeek tool-calling real (read_file/write_file/list_dir/run_command)
 * contra o repositório, num loop até a Task ser concluída ou o teto de
 * iterações estourar — sem manter Claude no loop de implementação.
 *
 * Uso:
 *   node scripts/deepseek-agent.mjs --task docs/specs/<n>-<slug>/task.md \
 *     [--system docs/spec/code-style.md,docs/spec/business-rules.md] \
 *     [--model deepseek-chat] [--max-iterations 40] [--cwd .]
 *
 * Lê DEEPSEEK_API_KEY de .env (raiz do repo) ou do ambiente já exportado —
 * mesma convenção de scripts/deepseek-call.sh, nunca aceita a chave como
 * argumento.
 *
 * Guard de segurança: docs/spec/security-rules.md#débitos-conhecidos —
 * não é sandbox completo, só bloqueia padrões obviamente destrutivos em
 * run_command e loga todo comando executado.
 *
 * Códigos de saída: 0 = Task concluída. 1 = falha genérica (teto de
 * iterações, ou parou em "## Inconsistências encontradas" — exige
 * julgamento de quem chamou). 3 = limite/quota da API DeepSeek (ADR-0002,
 * docs/spec/decisions/ADR-0002-continuidade-cruzada-limites.md) — não é
 * falha da Task, quem chamou deve tentar de novo mais tarde (o cron
 * horário já faz isso), nunca cair para fallback de implementação manual.
 *
 * Observabilidade: emite eventos para o event-tracker local
 * (scripts/event-tracker.mjs) que alimentam o dashboard em tempo real.
 * Nenhum prompt, resposta completa ou secret é exposto nos eventos.
 */

import { readFileSync, writeFileSync, existsSync, mkdirSync, appendFileSync, readdirSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { execSync } from 'node:child_process';
import {
  novaExecucao,
  concluirExecucao,
  iniciarEtapa,
  concluirEtapa,
  emitirEvento,
  atualizarEstadoAtual,
} from './event-tracker.mjs';
import { caminhoDentroDoRepo } from './path-safe.mjs';

const DEEPSEEK_API_URL = 'https://api.deepseek.com/chat/completions';

// ADR-0002: padrões conhecidos de erro de limite/quota da API DeepSeek —
// distinto de falha real da Task (ver comentário de topo do arquivo).
const PADROES_DE_LIMITE = [
  /rate.?limit/i,
  /too many requests/i,
  /insufficient.?balance/i,
  /saldo insuficiente/i,
  /quota/i,
  /\b429\b/,
];

class ErroDeLimiteDeepSeek extends Error {}

function ehErroDeLimite({ status, tipo, mensagem }) {
  if (status === 429) return true;
  const alvo = `${tipo ?? ''} ${mensagem ?? ''}`;
  return PADROES_DE_LIMITE.some((padrao) => padrao.test(alvo));
}

const PADROES_PERIGOSOS = [
  /rm\s+-rf\s+\/(?!\S)/, // rm -rf / (raiz, não um subpath)
  /rm\s+-rf\s+~/,
  /git\s+push\s+(--force|-f)\b/,
  /\bsudo\b/,
  /\bmkfs\b/,
  /dd\s+if=.*of=\/dev\//,
  /chmod\s+-R\s+777\s+\//,
  /:\(\)\s*\{\s*:\|:&\s*\};:/, // fork bomb
];

function carregarEnv(raizDoRepo) {
  const caminhoEnv = join(raizDoRepo, '.env');
  if (!existsSync(caminhoEnv)) return;
  for (const linha of readFileSync(caminhoEnv, 'utf8').split('\n')) {
    const match = /^([A-Z_][A-Z0-9_]*)=(.*)$/.exec(linha.trim());
    if (match && !(match[1] in process.env)) {
      process.env[match[1]] = match[2];
    }
  }
}

function lerArgumentos(argv) {
  const args = { model: process.env.DEEPSEEK_MODEL ?? 'deepseek-chat', maxIteracoes: 40, cwd: '.' };
  for (let i = 0; i < argv.length; i += 2) {
    const chave = argv[i].replace(/^--/, '');
    const valor = argv[i + 1];
    if (chave === 'task') args.task = valor;
    else if (chave === 'system') args.system = valor.split(',');
    else if (chave === 'model') args.model = valor;
    else if (chave === 'max-iterations') args.maxIteracoes = Number(valor);
    else if (chave === 'cwd') args.cwd = valor;
  }
  if (!args.task) {
    throw new Error('Uso: deepseek-agent.mjs --task docs/specs/<n>-<slug>/task.md [--system a.md,b.md]');
  }
  return args;
}

function montarPromptDeSistema(caminhosDeDoc, cwd) {
  const blocos = (caminhosDeDoc ?? []).map((caminho) => {
    const conteudo = readFileSync(resolve(cwd, caminho), 'utf8');
    return `## ${caminho}\n\n${conteudo}`;
  });
  const instrucao = [
    'Você é a etapa de implementação do pipeline Synclass (ADR-0001).',
    'Siga a ordem do task.md fornecido: para cada item, escreva o teste',
    'primeiro, veja-o falhar, implemente o mínimo pra passar, refatore,',
    'marque o item como `- [x]` no task.md via write_file. Rode só o',
    'teste do arquivo/classe tocado a cada passo (docs/spec/testing-standards.md)',
    '— a suíte completa é responsabilidade de quem chamou este harness,',
    'não sua. Não expanda escopo, não refatore código não relacionado, não',
    'altere regra de negócio, não ignore critério de aceite. Ambiguidade',
    'vira uma seção "## Inconsistências encontradas" no task.md, e você',
    'para ali — não adivinha. Um item de frontend vago (diz só a tela/',
    'componente, não onde exatamente o elemento fica, o rótulo exato, ou',
    'contra qual regra de docs/spec/ux-heuristics.md checar) é ambiguidade,',
    'não um convite pra você decidir hierarquia visual, posicionamento ou',
    'texto por conta própria — mesma regra: pare e registre em',
    '"## Inconsistências encontradas" em vez de escolher um layout. Antes de',
    'introduzir qualquer rótulo/mensagem novo visível ao usuário, rode',
    '`grep -rn` (via run_command) pelo conceito em outras telas/mensagens de',
    'erro do sistema — se o mesmo conceito já tem um nome (ex: um enum',
    'chamado `Vago` no backend, um rótulo "Livre" no formulário), use o',
    'termo que o usuário já vê em outro lugar, não o nome interno do código.',
    '',
    'implementation.md (se existir ao lado do task.md, leia-o antes do',
    'primeiro item) é a fonte da decisão de design, não uma sugestão — se',
    'ele mostra código de "antes"/"depois" ou cita um componente existente',
    'como padrão de estilo a seguir, implemente exatamente isso, não uma',
    'variação sua. Se um item de UI não tem esse nível de detalhe (nem',
    'trecho de código, nem referência de padrão) e a decisão não é óbvia só',
    'pela leitura do componente afetado, isso também é "## Inconsistências',
    'encontradas" — inventar hierarquia visual, nome de prop, ou estrutura',
    'nova quando já existe um padrão parecido no repositório (rode',
    '`grep -rn`/`list_dir` pra checar antes de assumir que não existe) é',
    'exatamente o tipo de erro que gerou retrabalho nesta mesma leva de',
    'Tasks (menu de navegação, seleção de hora, filtro de período).',
    'Quando todos os itens do task.md estiverem',
    '`- [x]`, responda sem chamar nenhuma tool, começando a mensagem com',
    'exatamente "TASK_CONCLUIDA".',
    '',
    'Commits (ADR-0004): faça um commit por item concluído do task.md via',
    '`run_command` (`git add <arquivos> && git commit -m "..."`), nunca um',
    'commit único acumulando tudo no final. Mensagem no formato',
    '"<tipo>(<escopo>): <o quê>" (ex: "feat(horarios): adiciona seletor de',
    'política no cadastro"), com um corpo curto explicando o porquê só',
    'quando não for óbvio pelo diff. Isso importa porque a revisão desta',
    'leva de Tasks é feita uma única vez no final, sobre vários PRs já',
    'mergeados — commits atômicos e descritivos são o que permite reverter',
    'um item pontual (`git revert <sha>`) sem desfazer o PR inteiro.',
    '',
    'Grafo de contexto (docs/spec/engenharia-de-qualidade.md): antes de',
    'editar uma função/componente/tipo que pode ser usado em mais de um',
    'lugar (exportado, prop pública, tipo compartilhado entre camadas),',
    'rode `grep -rn` (via run_command) pelo nome antes de mudar a',
    'assinatura — liste todos os call sites afetados de uma vez em vez de',
    'descobrir um por um enquanto o build reclama. Isso vale tanto pra',
    'código de produção quanto pra migration/contrato de Api tocado por',
    'mais de uma camada.',
    '',
    'Causa raiz: se uma tentativa óbvia de correção não resolver de',
    'primeira, pare antes da segunda tentativa e entenda por que o erro',
    'aconteceu (contrato errado, estado compartilhado, ordem de execução',
    'assumida) em vez de tentar variações às cegas — cada iteração às',
    'cegas consome o teto de 40 iterações sem gerar progresso real. Se a',
    'causa raiz não for óbvia pelo diff, registre-a na mensagem do commit',
    'que corrigir, não só "fix: corrige teste".',
    '',
    'Antes de escrever "TASK_CONCLUIDA": releia seu próprio diff',
    '(`git diff main...HEAD` via run_command) contra o checklist de',
    'docs/spec/code-style.md (tamanho de função/arquivo, nomes específicos,',
    'tipos explícitos, DI via construtor, wrapper de terceiro, early',
    'return, mensagens de exceção com valor+formato esperado, teste para',
    'toda função nova) e contra docs/spec/engenharia-de-qualidade.md',
    '(complexidade ciclomática, acoplamento, coesão — pergunte-se se cada',
    'função nova faz só uma coisa). Corrija o que encontrar antes de',
    'finalizar — é a sua própria autorrevisão; uma segunda passada da',
    'DeepSeek roda depois sobre o PR aberto (scripts/deepseek-review.sh),',
    'então não deixe achados óbvios para ela pegar.',
  ].join(' ');
  return [instrucao, ...blocos].join('\n\n---\n\n');
}

const DEFINICAO_DAS_TOOLS = [
  {
    type: 'function',
    function: {
      name: 'read_file',
      description: 'Lê o conteúdo de um arquivo do repositório.',
      parameters: {
        type: 'object',
        properties: { path: { type: 'string' } },
        required: ['path'],
      },
    },
  },
  {
    type: 'function',
    function: {
      name: 'write_file',
      description: 'Escreve (sobrescrevendo) o conteúdo de um arquivo do repositório.',
      parameters: {
        type: 'object',
        properties: { path: { type: 'string' }, content: { type: 'string' } },
        required: ['path', 'content'],
      },
    },
  },
  {
    type: 'function',
    function: {
      name: 'list_dir',
      description: 'Lista os arquivos/pastas de um diretório do repositório.',
      parameters: {
        type: 'object',
        properties: { path: { type: 'string' } },
        required: ['path'],
      },
    },
  },
  {
    type: 'function',
    function: {
      name: 'run_command',
      description: 'Roda um comando de shell no repositório (ex: dotnet test --filter X, npx jest arquivo).',
      parameters: {
        type: 'object',
        properties: { command: { type: 'string' } },
        required: ['command'],
      },
    },
  },
];

function comandoEhPerigoso(comando) {
  return PADROES_PERIGOSOS.some((padrao) => padrao.test(comando));
}

function executarTool(nome, argumentosJson, contexto) {
  const args = JSON.parse(argumentosJson || '{}');
  const inicio = Date.now();
  emitirEvento({ type: 'tool.started', metadata: { tool: nome, ...resumoArgs(nome, args) } });
  atualizarEstadoAtual({ currentTool: nome, currentStatus: 'running' });

  let resultado;
  try {
    switch (nome) {
      case 'read_file': resultado = executarReadFile(args, contexto); break;
      case 'write_file': resultado = executarWriteFile(args, contexto); break;
      case 'list_dir': resultado = executarListDir(args, contexto); break;
      case 'run_command': resultado = executarRunCommand(args, contexto); break;
      default:
        resultado = `Tool desconhecida: ${nome}`;
    }
    emitirEvento({
      type: 'tool.completed',
      metadata: { tool: nome, duracaoMs: Date.now() - inicio, success: !String(resultado).startsWith('Erro') && !String(resultado).startsWith('Comando bloqueado') },
    });
  } catch (erro) {
    emitirEvento({
      type: 'tool.error',
      metadata: { tool: nome, duracaoMs: Date.now() - inicio, error: erro.message.slice(0, 500) },
    });
    resultado = `Erro na tool ${nome}: ${erro.message}`;
  }
  atualizarEstadoAtual({ currentTool: null, currentStatus: 'running' });
  return resultado;
}

/** Resumo seguro dos args de uma tool para eventos — nunca conteúdo, só
 *  caminho e metadata relevante. */
function resumoArgs(nome, args) {
  if (nome === 'read_file' || nome === 'list_dir') {
    return { path: args.path ?? null };
  }
  if (nome === 'write_file') {
    return { path: args.path ?? null, tamanho: (args.content?.length ?? 0) };
  }
  if (nome === 'run_command') {
    return { command: args.command ?? null };
  }
  return {};
}

function executarReadFile({ path }, { cwd }) {
  const caminhoAbsoluto = caminhoDentroDoRepo(cwd, path);
  if (!caminhoAbsoluto) {
    return `Erro: caminho fora do repositório bloqueado: ${path}`;
  }
  try {
    return readFileSync(caminhoAbsoluto, 'utf8');
  } catch (erro) {
    return `Erro ao ler ${path}: ${erro.message}`;
  }
}

function executarWriteFile({ path, content }, { cwd }) {
  const caminhoAbsoluto = caminhoDentroDoRepo(cwd, path);
  if (!caminhoAbsoluto) {
    return `Erro: caminho fora do repositório bloqueado: ${path}`;
  }
  mkdirSync(dirname(caminhoAbsoluto), { recursive: true });
  writeFileSync(caminhoAbsoluto, content, 'utf8');
  return `Escrito: ${path}`;
}

function executarListDir({ path }, { cwd }) {
  const caminhoAbsoluto = caminhoDentroDoRepo(cwd, path ?? '.');
  if (!caminhoAbsoluto) {
    return `Erro: caminho fora do repositório bloqueado: ${path}`;
  }
  try {
    return readdirSync(caminhoAbsoluto).join('\n');
  } catch (erro) {
    return `Erro ao listar ${path}: ${erro.message}`;
  }
}

function executarRunCommand({ command }, { cwd, logPath }) {
  if (comandoEhPerigoso(command)) {
    registrarNoLog(logPath, `BLOQUEADO (padrão perigoso): ${command}`);
    emitirEvento({ type: 'command.blocked', metadata: { command } });
    return `Comando bloqueado pelo guard de segurança (ver docs/spec/security-rules.md#débitos-conhecidos): ${command}`;
  }
  registrarNoLog(logPath, `$ ${command}`);
  emitirEvento({ type: 'command.started', metadata: { command } });
  try {
    const saida = execSync(command, { cwd, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'], maxBuffer: 10 * 1024 * 1024 });
    registrarNoLog(logPath, saida);
    emitirEvento({
      type: 'command.completed',
      metadata: { command, exit: 0, saídaTamanho: saida.length },
    });
    return saida || '(sem saída, exit code 0)';
  } catch (erro) {
    const saida = `${erro.stdout ?? ''}${erro.stderr ?? ''}`;
    registrarNoLog(logPath, `EXIT ${erro.status}\n${saida}`);
    emitirEvento({
      type: 'command.failed',
      metadata: { command, exit: erro.status, saídaTamanho: saida.length },
    });
    return `Comando falhou (exit ${erro.status}):\n${saida}`;
  }
}

function registrarNoLog(logPath, texto) {
  appendFileSync(logPath, `${new Date().toISOString()} ${texto}\n`);
}

/**
 * ADR-0002: marca o bloqueio por limite da API no task.md, distinto de
 * "## Inconsistências encontradas" — isso não é ambiguidade de produto,
 * é a DeepSeek temporariamente indisponível. Quem chamou (worker/cron)
 * deve tentar de novo mais tarde, não cair para fallback manual.
 */
function escreverBloqueioPorLimite({ task, cwd, mensagem, logPath }) {
  const caminhoAbsoluto = resolve(cwd, task);
  const conteudoAtual = readFileSync(caminhoAbsoluto, 'utf8');
  const marcador = `\n\n## Bloqueado por limite da API DeepSeek\n\n${new Date().toISOString()} — ${mensagem}. Não é ambiguidade de produto: tente rodar o harness de novo mais tarde (ver ADR-0002).\n`;
  if (!conteudoAtual.includes('## Bloqueado por limite da API DeepSeek')) {
    writeFileSync(caminhoAbsoluto, conteudoAtual + marcador, 'utf8');
  }
  registrarNoLog(logPath, `BLOQUEADO POR LIMITE: ${mensagem}`);
  emitirEvento({ type: 'api.limit', metadata: { message: mensagem.slice(0, 300) } });
}

async function chamarDeepSeek({ model, messages, iteracao }) {
  emitirEvento({ type: 'api.request.started', metadata: { iteracao, model } });
  atualizarEstadoAtual({ currentStatus: 'waiting_api', model });
  try {
    const resposta = await fetch(DEEPSEEK_API_URL, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Authorization: `Bearer ${process.env.DEEPSEEK_API_KEY}`,
      },
      body: JSON.stringify({ model, messages, tools: DEFINICAO_DAS_TOOLS, tool_choice: 'auto', stream: false }),
    });
    const corpo = await resposta.json();
    atualizarEstadoAtual({ currentStatus: 'running' });
    emitirEvento({ type: 'api.response.received', metadata: { iteracao, model, status: resposta.status } });
    if (corpo.error) {
      const detalhe = { status: resposta.status, tipo: corpo.error.type, mensagem: corpo.error.message };
      emitirEvento({ type: 'api.error', metadata: { status: resposta.status, error: corpo.error.type ?? corpo.error.message?.slice(0, 300) } });
      if (ehErroDeLimite(detalhe)) {
        throw new ErroDeLimiteDeepSeek(`Limite/quota da API DeepSeek: ${corpo.error.message}`);
      }
      throw new Error(`Erro da API DeepSeek: ${corpo.error.message}`);
    }
    return corpo.choices[0].message;
  } catch (erro) {
    atualizarEstadoAtual({ currentStatus: 'error', currentTool: null });
    emitirEvento({ type: 'api.request.failed', metadata: { iteracao, model, error: erro.message.slice(0, 300) } });
    throw erro;
  }
}

function taskEstaConcluida(caminhoDoTask, cwd) {
  const conteudo = readFileSync(resolve(cwd, caminhoDoTask), 'utf8');
  const itens = conteudo.match(/^- \[[ x]\]/gim) ?? [];
  if (itens.length === 0) return false;
  return itens.every((item) => /\[x\]/i.test(item));
}

function montarContextoInicial({ task, system, cwd }) {
  const cwdAbsoluto = resolve(cwd);
  const pastaDaSpec = dirname(resolve(cwdAbsoluto, task));
  const logPath = join(pastaDaSpec, 'deepseek-run.log');
  mkdirSync(pastaDaSpec, { recursive: true });

  const promptDeSistema = montarPromptDeSistema(system, cwdAbsoluto);
  const conteudoDoTask = readFileSync(resolve(cwdAbsoluto, task), 'utf8');
  const messages = [
    { role: 'system', content: promptDeSistema },
    { role: 'user', content: `task.md (${task}):\n\n${conteudoDoTask}` },
  ];
  return { messages, contexto: { cwd: cwdAbsoluto, logPath } };
}

/**
 * Roda uma rodada do loop: chama a DeepSeek, aplica as tool calls
 * devolvidas (ou decide se a Task terminou, quando a resposta vem sem
 * tool call nenhuma). Retorna `{ concluida }` quando o loop deve parar.
 */
async function executarUmaIteracao({ model, messages, contexto, task, iteracao }) {
  const mensagemDaIa = await chamarDeepSeek({ model, messages, iteracao });
  messages.push(mensagemDaIa);

  if (mensagemDaIa.tool_calls?.length) {
    for (const chamada of mensagemDaIa.tool_calls) {
      emitirEvento({ type: 'tool.call', metadata: { tool: chamada.function.name } });
      const resultado = executarTool(chamada.function.name, chamada.function.arguments, contexto);
      messages.push({ role: 'tool', tool_call_id: chamada.id, content: String(resultado).slice(0, 20000) });
    }
    return { concluida: false };
  }

  registrarNoLog(contexto.logPath, `RESPOSTA SEM TOOL_CALLS: ${mensagemDaIa.content}`);
  emitirEvento({ type: 'api.no_tool_calls', metadata: { iteracao } });
  if (mensagemDaIa.content?.startsWith('TASK_CONCLUIDA') && taskEstaConcluida(task, contexto.cwd)) {
    return { concluida: true };
  }
  messages.push({
    role: 'user',
    content: 'Continue com o próximo item não concluído do task.md, ou explique o bloqueio numa seção "## Inconsistências encontradas".',
  });
  return { concluida: false };
}

async function rodarLoop({ task, system, model, maxIteracoes, cwd }) {
  const { messages, contexto } = montarContextoInicial({ task, system, cwd });

  for (let iteracao = 0; iteracao < maxIteracoes; iteracao += 1) {
    emitirEvento({ type: 'iteration.started', metadata: { iteracao: iteracao + 1, max: maxIteracoes } });
    const { concluida } = await executarUmaIteracao({ model, messages, contexto, task, iteracao: iteracao + 1 });
    if (concluida) return { sucesso: true, iteracoes: iteracao + 1 };
  }

  return { sucesso: false, iteracoes: maxIteracoes };
}

async function main() {
  const raizDoRepo = resolve(new URL('.', import.meta.url).pathname, '..');
  carregarEnv(raizDoRepo);

  if (!process.env.DEEPSEEK_API_KEY) {
    console.error('DEEPSEEK_API_KEY não definida (esperada em .env ou no ambiente).');
    process.exit(2);
  }

  const args = lerArgumentos(process.argv.slice(2));

  // Observabilidade: inicia a execução no tracker (dashboard).
  novaExecucao({ taskPath: args.task, cwd: args.cwd });
  iniciarEtapa('Implementation (DeepSeek)', 'DeepSeek', args.model);

  let resultado;
  try {
    resultado = await rodarLoop(args);
  } catch (erro) {
    if (erro instanceof ErroDeLimiteDeepSeek) {
      const cwdAbsoluto = resolve(args.cwd);
      const logPath = join(dirname(resolve(cwdAbsoluto, args.task)), 'deepseek-run.log');
      escreverBloqueioPorLimite({ task: args.task, cwd: cwdAbsoluto, mensagem: erro.message, logPath });
      concluirEtapa('Implementation (DeepSeek)', { status: 'failed', error: 'Limite da API DeepSeek' });
      concluirExecucao({ status: 'failed', error: 'Limite da API DeepSeek' });
      console.error(`${erro.message} — tente de novo mais tarde (ADR-0002), não é falha da Task.`);
      process.exit(3);
    }
    concluirEtapa('Implementation (DeepSeek)', { status: 'failed', error: erro.message });
    concluirExecucao({ status: 'failed', error: erro.message });
    throw erro;
  }

  if (resultado.sucesso) {
    concluirEtapa('Implementation (DeepSeek)', { status: 'completed' });
    concluirExecucao({ status: 'success' });
    console.log(`Task concluída em ${resultado.iteracoes} iteração(ões).`);
    process.exit(0);
  }
  console.error(`Teto de ${resultado.iteracoes} iterações atingido sem concluir a Task. Ver log em docs/specs/.../deepseek-run.log.`);
  concluirEtapa('Implementation (DeepSeek)', { status: 'failed', error: 'Teto de iterações atingido' });
  concluirExecucao({ status: 'failed', error: 'Teto de iterações' });
  process.exit(1);
}

main().catch((erro) => {
  console.error(erro.message);
  // Garante que a execução é encerrada no tracker mesmo com crash inesperado.
  try {
    concluirExecucao({ status: 'failed', error: erro.message });
  } catch { /* tracker indisponível — ignorar */ }
  process.exit(1);
});
