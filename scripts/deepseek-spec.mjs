#!/usr/bin/env node
/**
 * Rascunha a spec técnica (docs/spec/especificacao-tecnica.md) de uma
 * issue do GitHub via DeepSeek com tool-calling real (read_file/list_dir/
 * run_command/write_file), fecha a lacuna que
 * docs/spec/especificacao-tecnica.md#quem-escreve-e-a-aprovação-do-plano-adr-0001
 * ainda deixava presa a uma sessão Claude: "Nunca escreve task.md/
 * implementation.md do zero". Cria a worktree, escreve
 * docs/specs/<n>-<slug>/{task.md,implementation.md} e commita — depois
 * disso, scripts/pipeline-orchestrator.sh já descobre a Task sozinho e
 * segue o resto do pipeline (implementação, autorrevisão, merge) sem
 * nenhuma sessão Claude no caminho.
 *
 * Uso:
 *   node scripts/deepseek-spec.mjs <numero-da-issue> \
 *     [--model deepseek-chat] [--max-iterations 20]
 *
 * Recusa issues com label `epic` (quebra em Tasks é julgamento de
 * produto, não delegado aqui — ver
 * docs/backlog/padrao-de-issue.md#épico-e-task-features-grandes-ou-fullstack)
 * e issues fechadas/já especificadas (worktree/pasta de spec já existe).
 *
 * Lê DEEPSEEK_API_KEY de .env (raiz do repo) ou do ambiente, mesma
 * convenção de scripts/deepseek-call.sh/deepseek-agent.mjs.
 *
 * Códigos de saída: 0 = spec escrita e commitada. 1 = ambiguidade real
 * (vira "## Inconsistências encontradas" no task.md E uma issue de
 * bloqueio no GitHub, pra não depender de alguém notar em silêncio) ou
 * teto de iterações. 2 = erro de configuração/uso. 3 = limite/quota da
 * API DeepSeek (ADR-0002) — não é falha da Task, tentar de novo depois.
 */

import { readFileSync, writeFileSync, existsSync, mkdirSync, appendFileSync, readdirSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { execSync } from 'node:child_process';
import { caminhoDentroDoRepo } from './path-safe.mjs';

const DEEPSEEK_API_URL = 'https://api.deepseek.com/chat/completions';

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
  /rm\s+-rf\s+\/(?!\S)/,
  /rm\s+-rf\s+~/,
  /git\s+push\s+(--force|-f)\b/,
  /\bsudo\b/,
  /\bmkfs\b/,
  /dd\s+if=.*of=\/dev\//,
  /chmod\s+-R\s+777\s+\//,
  /:\(\)\s*\{\s*:\|:&\s*\};:/,
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
  const args = { model: process.env.DEEPSEEK_MODEL ?? 'deepseek-chat', maxIteracoes: 35 };
  for (let i = 0; i < argv.length; i += 1) {
    const atual = argv[i];
    if (atual === '--model') { args.model = argv[i + 1]; i += 1; }
    else if (atual === '--max-iterations') { args.maxIteracoes = Number(argv[i + 1]); i += 1; }
    else if (!atual.startsWith('--') && !args.issue) { args.issue = atual; }
  }
  if (!args.issue || !/^\d+$/.test(args.issue)) {
    throw new Error('Uso: deepseek-spec.mjs <numero-da-issue> [--model deepseek-chat] [--max-iterations 20]');
  }
  return args;
}

function slugificar(titulo) {
  const semPrefixo = titulo.replace(/^(feat|fix|docs|épico|epic)[:\s]*/i, '');
  const normalizado = semPrefixo
    .normalize('NFD').replace(/[̀-ͯ]/g, '')
    .toLowerCase()
    .replace(/[^a-z0-9\s-]/g, '')
    .trim()
    .split(/\s+/)
    .slice(0, 4)
    .join('-');
  return normalizado || 'issue';
}

function buscarIssue(numero) {
  const json = execSync(
    `gh issue view ${numero} --json title,body,number,labels,state`,
    { encoding: 'utf8' },
  );
  return JSON.parse(json);
}

function abrirIssueDeBloqueio(issueOriginal, motivo) {
  const titulo = `Bloqueio automático em deepseek-spec.mjs: issue #${issueOriginal.number} não virou spec`;
  const corpo = `Bloqueio detectado automaticamente por \`scripts/deepseek-spec.mjs\` — sem sessão Claude neste passo.\n\n**Issue original**: #${issueOriginal.number} — ${issueOriginal.title}\n\n**Motivo**:\n\n${motivo}\n\nEsta issue não virou spec técnica sozinha — precisa de decisão de produto/arquitetura antes de a implementação poder começar.`;
  try {
    execSync(`gh issue create --title ${JSON.stringify(titulo)} --body ${JSON.stringify(corpo)} --label bloqueio-pipeline`, { encoding: 'utf8', stdio: 'pipe' });
  } catch {
    try {
      execSync(`gh issue create --title ${JSON.stringify(titulo)} --body ${JSON.stringify(corpo)}`, { encoding: 'utf8', stdio: 'pipe' });
    } catch (erro) {
      console.error(`Falha ao abrir issue de bloqueio: ${erro.message}`);
    }
  }
}

function montarPromptDeSistema(cwd) {
  const docs = [
    'docs/backlog/padrao-de-issue.md',
    'docs/spec/especificacao-tecnica.md',
    'docs/spec/code-style.md',
    'docs/spec/business-rules.md',
    'docs/spec/testing-standards.md',
    'docs/spec/ux-heuristics.md',
  ];
  const blocos = docs.map((caminho) => {
    const conteudo = readFileSync(resolve(cwd, caminho), 'utf8');
    return `## ${caminho}\n\n${conteudo}`;
  });

  const instrucao = [
    'Você é a etapa de spec técnica do pipeline Synclass, rodando sem',
    'sessão Claude (fecha a lacuna descrita em',
    'docs/spec/especificacao-tecnica.md#quem-escreve-e-a-aprovação-do-plano-adr-0001).',
    'Sua única tarefa: a partir da issue do GitHub fornecida, produzir',
    'docs/specs/<n>-<slug>/task.md e docs/specs/<n>-<slug>/implementation.md',
    '(a pasta já existe, escreva os dois arquivos nela via write_file) —',
    'siga EXATAMENTE a estrutura e as regras dos documentos de sistema',
    'acima, com atenção especial a duas seções: "Item de frontend não pode',
    'ser vago" (rótulo/copy exato, onde exatamente o elemento fica, qual',
    'regra de ux-heuristics.md se aplica) e o corpo de implementation.md',
    '(entidades/arquivos exatos tocados, trecho de código antes/depois via',
    'read_file real — nunca invente o "antes" sem ler o arquivo — e um',
    'padrão de estilo existente citado explicitamente por arquivo:linha).',
    '',
    'Use list_dir/read_file/run_command (grep, git log, git grep) para',
    'investigar o repositório de verdade antes de escrever qualquer linha',
    'de spec — nunca assuma nome de arquivo, assinatura, ou convenção sem',
    'confirmar lendo o código atual. Se dois exemplos de spec já aprovados',
    'ajudarem de referência de tom/formato, leia',
    'docs/specs/159-identificador-auto-gerado/implementation.md e',
    'docs/specs/161-menu-lateral-desktop/implementation.md antes de',
    'escrever a sua.',
    '',
    'Se, depois de investigar o repositório, sobrar uma pergunta de',
    'produto/UX genuína que nenhum documento de sistema nem o código',
    'resolve (o mesmo padrão de ambiguidade que faria uma sessão Claude',
    'usar AskUserQuestion) — não adivinhe. Escreva no task.md uma seção',
    '"## Inconsistências encontradas" com a pergunta exata e pare,',
    'respondendo sem chamar nenhuma tool, começando a mensagem com',
    'exatamente "SPEC_BLOQUEADA". Isso é o resultado correto pra uma',
    'ambiguidade real — não é falha sua, é o mesmo comportamento que',
    'docs/spec/especificacao-tecnica.md já espera da Fase 2.',
    '',
    'Quando os dois arquivos estiverem completos e você já releu ambos',
    'contra as duas seções citadas acima (releia de verdade via read_file,',
    'não confie na sua própria memória do que escreveu), faça um único',
    'commit (`git add docs/specs/<n>-<slug> && git commit -m "docs(specs):',
    'spec técnica da issue #<n>"` via run_command) e responda sem chamar',
    'nenhuma tool, começando a mensagem com exatamente "SPEC_CONCLUIDA".',
    '',
    'Nunca edite nenhum arquivo fora de docs/specs/<n>-<slug>/ — esta etapa',
    'só planeja, não implementa.',
  ].join(' ');

  return [instrucao, ...blocos].join('\n\n---\n\n');
}

const DEFINICAO_DAS_TOOLS = [
  { type: 'function', function: { name: 'read_file', description: 'Lê o conteúdo de um arquivo do repositório.', parameters: { type: 'object', properties: { path: { type: 'string' } }, required: ['path'] } } },
  { type: 'function', function: { name: 'write_file', description: 'Escreve (sobrescrevendo) o conteúdo de um arquivo do repositório.', parameters: { type: 'object', properties: { path: { type: 'string' }, content: { type: 'string' } }, required: ['path', 'content'] } } },
  { type: 'function', function: { name: 'list_dir', description: 'Lista os arquivos/pastas de um diretório do repositório.', parameters: { type: 'object', properties: { path: { type: 'string' } }, required: ['path'] } } },
  { type: 'function', function: { name: 'run_command', description: 'Roda um comando de shell no repositório (ex: grep -rn, git log, git grep).', parameters: { type: 'object', properties: { command: { type: 'string' } }, required: ['command'] } } },
];

function comandoEhPerigoso(comando) {
  return PADROES_PERIGOSOS.some((padrao) => padrao.test(comando));
}

function executarReadFile({ path }, { cwd }) {
  const caminhoAbsoluto = caminhoDentroDoRepo(cwd, path);
  if (!caminhoAbsoluto) return `Erro: caminho fora do repositório bloqueado: ${path}`;
  try { return readFileSync(caminhoAbsoluto, 'utf8'); } catch (erro) { return `Erro ao ler ${path}: ${erro.message}`; }
}

function executarWriteFile({ path, content }, { cwd }) {
  const caminhoAbsoluto = caminhoDentroDoRepo(cwd, path);
  if (!caminhoAbsoluto) return `Erro: caminho fora do repositório bloqueado: ${path}`;
  if (!/^docs\/specs\//.test(path)) return `Erro: esta etapa só escreve dentro de docs/specs/ — recusado: ${path}`;
  mkdirSync(dirname(caminhoAbsoluto), { recursive: true });
  writeFileSync(caminhoAbsoluto, content, 'utf8');
  return `Escrito: ${path}`;
}

function executarListDir({ path }, { cwd }) {
  const caminhoAbsoluto = caminhoDentroDoRepo(cwd, path ?? '.');
  if (!caminhoAbsoluto) return `Erro: caminho fora do repositório bloqueado: ${path}`;
  try { return readdirSync(caminhoAbsoluto).join('\n'); } catch (erro) { return `Erro ao listar ${path}: ${erro.message}`; }
}

function registrarNoLog(logPath, texto) {
  appendFileSync(logPath, `${new Date().toISOString()} ${texto}\n`);
}

function executarRunCommand({ command }, { cwd, logPath }) {
  if (comandoEhPerigoso(command)) {
    registrarNoLog(logPath, `BLOQUEADO (padrão perigoso): ${command}`);
    return `Comando bloqueado pelo guard de segurança: ${command}`;
  }
  registrarNoLog(logPath, `$ ${command}`);
  try {
    const saida = execSync(command, { cwd, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'], maxBuffer: 10 * 1024 * 1024 });
    registrarNoLog(logPath, saida);
    return saida || '(sem saída, exit code 0)';
  } catch (erro) {
    const saida = `${erro.stdout ?? ''}${erro.stderr ?? ''}`;
    registrarNoLog(logPath, `EXIT ${erro.status}\n${saida}`);
    return `Comando falhou (exit ${erro.status}):\n${saida}`;
  }
}

/**
 * Alguns retornos de tool_calls da API vêm com caracteres de controle
 * literais (newline/tab cru) dentro de string JSON — comum quando o
 * argumento `content` de write_file carrega texto multi-linha e o modelo
 * não escapa `\n` corretamente. JSON.parse estrito rejeita isso ("Bad
 * control character in string literal"); em vez de derrubar o processo
 * inteiro por causa de uma tool call, tentamos reparar escapando os
 * bytes de controle crus antes de reparsear.
 */
function parseArgsDaTool(argumentosJson) {
  const bruto = argumentosJson || '{}';
  try {
    return JSON.parse(bruto);
  } catch {
    const reparado = bruto.replace(/[ -]/g, (ch) => {
      if (ch === '\n') return '\\n';
      if (ch === '\r') return '\\r';
      if (ch === '\t') return '\\t';
      return '';
    });
    return JSON.parse(reparado);
  }
}

function executarTool(nome, argumentosJson, contexto) {
  let args;
  try {
    args = parseArgsDaTool(argumentosJson);
  } catch (erro) {
    return `Erro: argumentos da tool ${nome} vieram como JSON inválido mesmo após tentativa de reparo (${erro.message}). Tente de novo com o JSON bem formado.`;
  }
  switch (nome) {
    case 'read_file': return executarReadFile(args, contexto);
    case 'write_file': return executarWriteFile(args, contexto);
    case 'list_dir': return executarListDir(args, contexto);
    case 'run_command': return executarRunCommand(args, contexto);
    default: return `Tool desconhecida: ${nome}`;
  }
}

async function chamarDeepSeek({ model, messages }) {
  const resposta = await fetch(DEEPSEEK_API_URL, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${process.env.DEEPSEEK_API_KEY}` },
    body: JSON.stringify({ model, messages, tools: DEFINICAO_DAS_TOOLS, tool_choice: 'auto', stream: false }),
  });
  const corpo = await resposta.json();
  if (corpo.error) {
    const detalhe = { status: resposta.status, tipo: corpo.error.type, mensagem: corpo.error.message };
    if (ehErroDeLimite(detalhe)) throw new ErroDeLimiteDeepSeek(`Limite/quota da API DeepSeek: ${corpo.error.message}`);
    throw new Error(`Erro da API DeepSeek: ${corpo.error.message}`);
  }
  return corpo.choices[0].message;
}

function specEstaCompleta(pastaDaSpec) {
  const taskPath = join(pastaDaSpec, 'task.md');
  const implPath = join(pastaDaSpec, 'implementation.md');
  if (!existsSync(taskPath) || !existsSync(implPath)) return false;
  const task = readFileSync(taskPath, 'utf8');
  const impl = readFileSync(implPath, 'utf8');
  return task.length > 200 && impl.length > 300 && /^- \[ \]/m.test(task);
}

async function rodarLoop({ issue, pastaDaSpec, cwd, model, maxIteracoes, logPath }) {
  const promptDeSistema = montarPromptDeSistema(cwd);
  const mensagemDaIssue = `Issue #${issue.number}: ${issue.title}\n\nLabels: ${issue.labels.map((l) => l.name).join(', ')}\n\n${issue.body}\n\nPasta de destino: docs/specs/${resolveSlugPath(pastaDaSpec)}/`;
  const messages = [
    { role: 'system', content: promptDeSistema },
    { role: 'user', content: mensagemDaIssue },
  ];
  const contexto = { cwd, logPath };

  for (let iteracao = 0; iteracao < maxIteracoes; iteracao += 1) {
    const mensagemDaIa = await chamarDeepSeek({ model, messages });
    messages.push(mensagemDaIa);

    if (mensagemDaIa.tool_calls?.length) {
      for (const chamada of mensagemDaIa.tool_calls) {
        const resultado = executarTool(chamada.function.name, chamada.function.arguments, contexto);
        messages.push({ role: 'tool', tool_call_id: chamada.id, content: String(resultado).slice(0, 20000) });
      }
      continue;
    }

    registrarNoLog(logPath, `RESPOSTA SEM TOOL_CALLS: ${mensagemDaIa.content}`);
    // Não usa startsWith: na prática o modelo costuma escrever um
    // parágrafo de autovalidação ANTES da palavra-chave, não no início
    // exato da mensagem (achado ao rodar contra as issues #165/#166/#167
    // — o loop inteiro se esgotava reconfirmando um estado já correto em
    // disco porque a palavra-chave nunca "começava" a mensagem). A fonte
    // de verdade real é `specEstaCompleta` (arquivos em disco); a
    // palavra-chave só precisa aparecer em algum lugar da resposta.
    if (/\bSPEC_CONCLUIDA\b/.test(mensagemDaIa.content ?? '') && specEstaCompleta(pastaDaSpec)) {
      return { status: 'concluida' };
    }
    if (/\bSPEC_BLOQUEADA\b/.test(mensagemDaIa.content ?? '')) {
      return { status: 'bloqueada' };
    }
    messages.push({
      role: 'user',
      content: 'Continue investigando e escrevendo task.md/implementation.md, ou registre a ambiguidade e pare com SPEC_BLOQUEADA.',
    });
  }
  return { status: 'teto' };
}

function resolveSlugPath(pastaDaSpec) {
  return pastaDaSpec.split('/').pop();
}

async function main() {
  const raizDoRepo = resolve(new URL('.', import.meta.url).pathname, '..');
  carregarEnv(raizDoRepo);

  if (!process.env.DEEPSEEK_API_KEY) {
    console.error('DEEPSEEK_API_KEY não definida (esperada em .env ou no ambiente).');
    process.exit(2);
  }

  let args;
  try {
    args = lerArgumentos(process.argv.slice(2));
  } catch (erro) {
    console.error(erro.message);
    process.exit(2);
  }

  const issue = buscarIssue(args.issue);
  if (issue.state !== 'OPEN') {
    console.error(`Issue #${issue.number} não está aberta (state=${issue.state}).`);
    process.exit(2);
  }
  if (issue.labels.some((l) => l.name === 'epic')) {
    console.error(`Issue #${issue.number} é um épico — quebra em Tasks é julgamento de produto, não delegado a este script (ver docs/backlog/padrao-de-issue.md#épico-e-task-features-grandes-ou-fullstack).`);
    process.exit(2);
  }

  const slug = slugificar(issue.title);
  const nomeDaWorktree = `synclass-${slug}`;
  const worktreePath = resolve(raizDoRepo, '..', nomeDaWorktree);
  const isFix = issue.labels.some((l) => l.name === 'bug' || l.name === 'fix');
  const branch = `${isFix ? 'fix' : 'feature'}/${slug}`;

  if (existsSync(worktreePath)) {
    console.error(`Worktree ${worktreePath} já existe — issue provavelmente já tem spec/implementação em andamento.`);
    process.exit(2);
  }

  console.log(`Criando worktree ${worktreePath} (branch ${branch}) para a issue #${issue.number}...`);
  // Retoma uma branch órfã de uma execução anterior que crashou antes do
  // primeiro commit (worktree já removida, branch local ainda existe) —
  // evita exigir limpeza manual (git branch -D) a cada retry de um
  // pipeline que se pretende autônomo.
  const branchJaExiste = execSync(`git branch --list ${JSON.stringify(branch)}`, { cwd: raizDoRepo, encoding: 'utf8' }).trim() !== '';
  const comandoWorktree = branchJaExiste
    ? `git worktree add ${JSON.stringify(worktreePath)} ${JSON.stringify(branch)}`
    : `git worktree add ${JSON.stringify(worktreePath)} -b ${JSON.stringify(branch)} main`;
  execSync(comandoWorktree, { cwd: raizDoRepo, stdio: 'inherit' });
  if (existsSync(join(raizDoRepo, '.env'))) {
    execSync(`cp ${JSON.stringify(join(raizDoRepo, '.env'))} ${JSON.stringify(join(worktreePath, '.env'))}`);
  }

  const pastaDaSpec = join(worktreePath, 'docs', 'specs', `${issue.number}-${slug}`);
  mkdirSync(pastaDaSpec, { recursive: true });
  const logPath = join(pastaDaSpec, 'deepseek-spec-run.log');

  let resultado;
  try {
    resultado = await rodarLoop({ issue, pastaDaSpec, cwd: worktreePath, model: args.model, maxIteracoes: args.maxIteracoes, logPath });
  } catch (erro) {
    if (erro instanceof ErroDeLimiteDeepSeek) {
      console.error(`${erro.message} — tente de novo mais tarde (ADR-0002), não é falha da Task.`);
      process.exit(3);
    }
    console.error(erro.message);
    process.exit(1);
  }

  if (resultado.status === 'concluida') {
    console.log(`Spec técnica da issue #${issue.number} escrita e commitada em ${pastaDaSpec}.`);
    console.log('Próximo passo automático (sem Claude): scripts/pipeline-orchestrator.sh já descobre esta Task sozinho, ou rode direto:');
    console.log(`  node scripts/deepseek-agent.mjs --task docs/specs/${issue.number}-${slug}/task.md --system docs/spec/code-style.md,docs/spec/business-rules.md,docs/spec/security-rules.md,docs/spec/testing-standards.md,docs/spec/ux-heuristics.md,docs/spec/engenharia-de-qualidade.md --cwd ${worktreePath}`);
    process.exit(0);
  }

  if (resultado.status === 'bloqueada') {
    console.error(`Spec da issue #${issue.number} bloqueada por ambiguidade real — ver "## Inconsistências encontradas" em ${pastaDaSpec}/task.md.`);
    abrirIssueDeBloqueio(issue, `Ver \`docs/specs/${issue.number}-${slug}/task.md\` (seção "## Inconsistências encontradas") e \`${logPath}\` para o detalhe.`);
    process.exit(1);
  }

  console.error(`Teto de ${args.maxIteracoes} iterações atingido sem concluir a spec da issue #${issue.number}. Ver ${logPath}.`);
  process.exit(1);
}

main();
