# Implementação: Aluno vê resumo de frequência recente direto no Painel (#167)

## Decisão de design (resolvida, não é pra DeepSeek escolher)

**Janela exata do resumo — últimos 30 dias rolantes** (não "últimas N
aulas", não mês corrente). O card pede na RN "últimos 30 dias... ou das
últimas N aulas" e o Critério técnico manda "definir a janela com base no
que a Api já devolve, sem exigir mudança de contrato se possível". A Api
**não tem** uma chamada de "últimas N aulas" — só período arbitrário
`[inicio, fim)` (que aceita `inicio`+`fim` na query, ver
`HistoricoFrequenciaController`) ou o default mês corrente (sem parâmetro).
"Últimas N aulas" exigiria mudança de contrato; **últimos 30 dias rolantes**
NÃO exige (basta calcular `inicio`/`fim` no frontend e passar como
`PeriodoConsultaInput`, que `listarHistoricoFrequenciaDoAluno` já aceita).
Portanto a janela é **hoje-29 dias até hoje inclusive** (período
`[hoje-29, próximoDia(hoje))` = 30 dias), calculada no frontend — não o
default mês corrente, porque o card fala de "frequência recente" (últimos
30 dias), não "deste mês". Fica uma fração de milissegundos de desvio entre
montar e a data mudar à meia-noite — irrelevante aqui, o resumo é apenas
uma fotografia daquele momento, ver edge points.

**O que o resumo conta — só `Presente` e `Ausente`.** O resumo mostra o par
"compareceu / faltou": `StatusHistoricoFrequencia.Presente` → compareceu,
`StatusHistoricoFrequencia.Ausente` → faltou. `NaoRegistrada` (aula no
período em que o Professor não registrou, ver `FrequenciaService.CalcularStatusAsync`)
e `Cancelada` NÃO entram no par central — não são comparecimento nem falta
do Aluno (cancelada é decisão/algo que tirou a aula; não-registrada é o
Professor que não marcou), consistente com a terminologia já rotulada em
`HistoricoFrequenciaCard` (`RotulosPorStatus`, linha 11). **Caso "sem
dados"** (cenário 2 do card): o resumo é `presentes === 0 && ausentes ===
0` — cobre tanto a lista vazia (zero matrículas, ou matrículas mas zero
datas de aula no período) quanto período em que todas as aulas são
`NaoRegistrada`/`Cancelada`. Nesse caso mostra a mensagem de vazio, não uma
seção em branco.

**Onde entra e o que NÃO é**: um cartão informativo no Painel
(`frontend/src/app/painel/index.tsx`), entre a saudação e o grid de
`CardDeAcao`, **visível só quando `papelAtivo === 'Aluno'`** (o card é do
Aluno; no papel Professor não aparece resumo nenhum). Não é um card de ação
(não navega), não é um CTA, não compete em peso com os `CardDeAcao` — usa o
mesmo esqueleto visual de `HistoricoFrequenciaCard`/`ValueDevidoCard` (fundo
`bg-background-element`, borda `border-background-selected`,
`rounded-medium`), ver referência de estilo abaixo. Segue
`docs/spec/ux-heuristics.md#agrupamento-visual-gestalt`: o resumo é um grupo
próprio separado do grid de ações, e por ser informação (não ação) nunca
ganha destaque de CTA (nenhum botão, nenhuma cor primária cheia).

## Entidades/arquivos afetados

- `frontend/src/lib/api/historicoFrequencia.ts` — ADICIONA `calcularPeriodoUltimosNDias(hoje, n)` (30 dias). A chamada `listarHistoricoFrequenciaDoAluno` e os tipos existentes NÃO mudam (mesmo contrato já usado pela tela de histórico).
- `frontend/src/components/organisms/ResumoFrequenciaCard.tsx` — NOVO organismo de apresentação pura (recebe contagens via props, não consulta).
- `frontend/src/app/painel/index.tsx` — quando `papelAtivo === 'Aluno'`, consulta o resumo e renderiza `ResumoFrequenciaCard` entre a saudação e as ações.
- Testes: `frontend/src/components/organisms/ResumoFrequenciaCard.test.tsx` (novo) e `frontend/src/app/painel/index.test.tsx` (estende).

**Sem mudança de contrato de Api, sem migration, sem backend** — mesmo dado já exposto por `GET /alunos/historico-frequencia`.

## Frontend

### `frontend/src/lib/api/historicoFrequencia.ts` — adiciona o cálculo do período

Segue o padrão de `calcularPeriodoTodos` em `frontend/src/lib/api/valorDevido.ts:79-82` (função exportada no módulo de API da feature, construindo datas deslocadas via `Date` e formatando com `paraDataISO`) — não crie um módulo novo nem duplique `paraDataISO`.

```ts
// após `caminhoComQuery` (fim do arquivo) — no mesmo módulo da chamada que usa o período:
/**
 * Período `[hoje-dias+1, proximoDia(hoje))` = "os últimos `dias` incluindo hoje",
 * usado pelo resumo de frequência recente do Painel (issue #167). Mesmo
 * racional de cálculo deslocado de datas de `calcularPeriodoTodos`
 * (`lib/api/valorDevido.ts`); `fim` exclusivo = dia seguinte a hoje, então
 * `proximoDia(hoje)` inclui o dia de hoje na consulta (mesmo contrato
 * `[inicio, fim)` da Api).
 */
export function calcularPeriodoUltimosNDias(hoje: Date, dias: number): PeriodoConsultaInput {
  const inicio = new Date(hoje.getFullYear(), hoje.getMonth(), hoje.getDate() - (dias - 1));
  return { inicio: paraDataISO(inicio), fim: proximoDia(paraDataISO(hoje)) };
}
```

O import atual de `lib/api/historicoFrequencia.ts` só traz `fetchComTimeout`/`MensagemErroConexao` de `httpClient` — adicione import de `paraDataISO` e `proximoDia` de `@/lib/formatarData` no topo.

### `frontend/src/components/organisms/ResumoFrequenciaCard.tsx` — novo organismo

Apresentação pura, sem efeitos nem fetch (a consulta mora no Painel, mesmo padrão de como `ValorDevidoCard`/`HistoricoFrequenciaCard` são passivos e a tela faz o hook). Props: `{ presentes: number; ausentes: number }`. Esqueleto visual idêntico a `HistoricoFrequenciaCard.tsx:39` (`flex-row items-center justify-between rounded-medium border border-background-selected bg-background-element px-four py-three ...`):

```tsx
import { Text, View } from 'react-native';

export type ResumoFrequenciaCardProps = { presentes: number; ausentes: number };

const MensagemSemDados =
  'Você ainda não tem frequência registrada nos últimos 30 dias. Suas aulas aparecem aqui assim que alguma presença ou falta for marcada.';

/**
 * Forma singular/plural correta do rótulo de uma contagem no resumo — copy
 * fixado aqui (não é pra DeepSeek escolher). "1 presença", "2 presenças";
 * "1 falta", "2 faltas".
 */
function rotularPlural(quantidade: number, singular: string, plural: string): string {
  return `${quantidade} ${quantidade === 1 ? singular : plural}`;
}

/**
 * Organismo: resumo de frequência recente do Aluno no Painel (issue #167) —
 * contagem de aulas `Presente`/`Ausente` nos últimos 30 dias, calculada a
 * partir do mesmo `GET /alunos/historico-frequencia` da tela de histórico
 * (issue #16), sem duplicar a lógica de cálculo (quem conta os status é o
 * `calcularResumo` no Painel, ver lá). Somente os dois status entram no par
 * "compareceu/faltou" — `NaoRegistrada`/`Cancelada` não são nem um nem outro
 * e mantêm o card no estado vazio. Mesmo esqueleto visual de
 * `HistoricoFrequenciaCard`/`ValueDevidoCard` (borda
 * `border-background-selected`, fundo `bg-background-element`).
 */
export function ResumoFrequenciaCard({ presentes, ausentes }: ResumoFrequenciaCardProps) {
  const semDados = presentes === 0 && ausentes === 0;

  return (
    <View className="w-full flex-row items-center justify-between rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
      {semDados ? (
        <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
          {MensagemSemDados}
        </Text>
      ) : (
        <>
          <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">Nos últimos 30 dias</Text>
          <Text className="text-sm font-semibold text-text dark:text-dark-text">
            {rotularPlural(presentes, 'presença', 'presenças')} ·{' '}
            {rotularPlural(ausentes, 'falta', 'faltas')}
          </Text>
        </>
      )}
    </View>
  );
}
```

Copy exato exibido ao Aluno:
- Com dados: `# presença(s) · # falta(s)` — ex: `3 presenças · 1 falta`; `0 presenças · 2 faltas`.
- Sem dados (`presentes === 0 && ausentes === 0`): `MensagemSemDados` =
  "Você ainda não tem frequência registrada nos últimos 30 dias. Suas aulas aparecem aqui assim que alguma presença ou falta for marcada."

### `frontend/src/app/painel/index.tsx` — consulta e inserção

Estado atual relevante (Painel, `painel/index.tsx:42-66`): `PainelScreen` já tem `carregando`, `token`, `papelAtivo` de `useSessao()`, e o corpo monta `{nome ? <Saudacao nome={nome} /> : null}` seguido de um `<View className="w-full flex-row flex-wrap gap-three">` com `acoes.map`. O trecho "antes" real é:

```tsx
      {nome ? <Saudacao nome={nome} /> : null}
      <View className="w-full flex-row flex-wrap gap-three">
        {acoes.map((acao) => (
          <CardDeAcao key={acao.label} acao={acao} />
        ))}
      </View>
```

O trecho "depois" — inserir o resumo entre a saudação e o grid, **apenas quando `papelAtivo === 'Aluno'`**:

```tsx
      {nome ? <Saudacao nome={nome} /> : null}
      {papelAtivo === 'Aluno' ? <ResumoDeFrequenciaDoAluno /> : null}
      <View className="w-full flex-row flex-wrap gap-three">
        {acoes.map((acao) => (
          <CardDeAcao key={acao.label} acao={acao} />
        ))}
      </View>
```

Adicione, no topo do arquivo, os imports novos:

```tsx
import { ResumoFrequenciaCard } from '@/components/organisms/ResumoFrequenciaCard';
import {
  calcularPeriodoUltimosNDias,
  listarHistoricoFrequenciaDoAluno,
  type HistoricoFrequenciaPorProfessor,
} from '@/lib/api/historicoFrequencia';
```

O hook (no mesmo arquivo, abaixo de `PainelScreen`, mesmo estilo de co-locação dos outros hooks de consulta do repo — `useConsultaHistoricoFrequenciaDoAluno` em `aluno/historico-frequencia.tsx`; aqui mais simples, período fixo, sem seleção, então um `useEffect` simples, sem `chaveAtual`):

```tsx
/**
 * Resumo de frequência recente do Aluno (issue #167): consulta os últimos
 * 30 dias do mesmo `GET /alunos/historico-frequencia` da tela de histórico e
 * deriva as contagens de `Presente`/`Ausente` — sem duplicar a lógica de
 * cálculo (quem define os status é o backend, via `FrequenciaService`).
 * `NaoRegistrada`/`Cancelada` não entram no par "compareceu/faltou", então
 * ambas zero → estado vazio (mensagem clara, não seção em branco). Só monta
 * quando `papelAtivo === 'Aluno'` (o cartão não existe para o Professor).
 */
function ResumoDeFrequenciaDoAluno() {
  const [resumo, setResumo] = useState<{ presentes: number; ausentes: number } | undefined>(undefined);
  const [erro, setErro] = useState(false);

  useEffect(() => {
    let cancelado = false;
    listarHistoricoFrequenciaDoAluno(calcularPeriodoUltimosNDias(new Date(), 30)).then((dados) => {
      if (cancelado) return;
      if (!dados.sucesso) {
        setErro(true);
        return;
      }
      const { presentes, ausentes } = calcularResumo(dados.historico);
      setResumo({ presentes, ausentes });
      setErro(false);
    });
    return () => {
      cancelado = true;
    };
  }, []);

  if (erro) {
    return null; // sem resumo em falha de rede; as ações do Painel continuam inteiras
  }
  if (resumo === undefined) {
    return (
      <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
        Carregando…
      </Text>
    );
  }
  return <ResumoFrequenciaCard presentes={resumo.presentes} ausentes={resumo.ausentes} />;
}

/**
 * Conta `Presente` e `Ausente` no histórico agregado por Professor — nunca
 * mistura Professores, só soma o mesmo status entre eles (mesmo racional da
 * RN de valor devido, issue #13). Pura e fácil de testar isoladamente.
 */
function calcularResumo(historico: HistoricoFrequenciaPorProfessor[]): { presentes: number; ausentes: number } {
  let presentes = 0;
  let ausentes = 0;
  for (const porProfessor of historico) {
    for (const aula of porProfessor.aulas) {
      if (aula.status === 'Presente') presentes += 1;
      if (aula.status === 'Ausente') ausentes += 1;
    }
  }
  return { presentes, ausentes };
}
```

Nota de imports no arquivo atual: `painel/index.tsx` NÃO importa nada de `'react'` hoje (o `PainelScreen` é sem estado local — só hooks de lib: `useSessao`, `usePerfilLogado`, `useRedirecionarSemSessao`). `Text` de `react-native` já está no topo (usado no `CardDeAcao`). Portanto o código novo precisa ADICIONAR o import de `react` trazendo `useState` e `useEffect` (ex: `import { useEffect, useState } from 'react';`), e importar `ResumoFrequenciaCard` e a função/`listarHistoricoFrequenciaDoAluno` de `@/lib/api/historicoFrequencia`.

### `frontend/src/app/painel/index.test.tsx` — estende

O teste do Painel já mocka `TopbarAutenticada` e `useSessao`. Adicione `jest.mock('@/lib/api/historicoFrequencia', ...)` e casos:

- Com `papelAtivo: 'Aluno'` e mock de `listarHistoricoFrequenciaDoAluno` resolvendo com 3 `Presente` + 1 `Ausente`, o resumo exibe "3 presenças · 1 falta".
- Com `papelAtivo: 'Aluno'` e todas as aulas `NaoRegistrada`/`Cancelada` (ou `historico: []`), exibe `MensagemSemDados`.
- Com `papelAtivo: 'Professor'`, o resumo NÃO aparece (o mock pode nem ser chamado — `queryByText(/presença/)` é `null`).
- Na falha de rede (`sucesso: false`), não quebra a tela e as ações continuam visíveis.
- Conferir que `listarHistoricoFrequenciaDoAluno` é chamado com o período `{ inicio, fim }` dos últimos 30 dias (o `inicio` termina 29 dias antes de `hoje`, `fim` termina no dia seguinte a hoje) — pode-se passar uma data fixa a `calcularPeriodoUltimosNDias` se preferir mocká-la (ela é exportada), ou validar por regex da string `yyyy-MM-dd`.

### Testes novos

- `frontend/src/components/organisms/ResumoFrequenciaCard.test.tsx` (novo):
  - `presentes=3, ausentes=1` → "3 presenças · 1 falta" (singular correto).
  - `presentes=0, ausentes=0` → `MensagemSemDados`.
  - `presentes=0, ausentes=2` (só faltas) → SAI do estado vazio e mostra "0 presenças · 2 faltas" (vazio só quando AMBOS são zero).
  - `presentes=1, ausentes=0` → "1 presença · 0 faltas" (singular de "presença").

## Padrão de estilo a seguir (por arquivo:linha)

- **Esqueleto de card informativo**: siga `frontend/src/components/organisms/HistoricoFrequenciaCard.tsx:39` (e o mesmo esqueleto em `ValueDevidoCard.tsx:15`) — `rounded-medium border border-background-selected bg-background-element px-four py-three dark:...` — para o `ResumoFrequenciaCard`. NÃO crie um padrão de card novo.
- **Cálculo de período deslocado**: siga `frontend/src/lib/api/valorDevido.ts:79-82` (`calcularPeriodoTodos`) — função exportada no módulo de API da feature, datas montadas via `Date` e formatadas com `paraDataISO`.
- **Consulta de API co-localizada na tela**: siga `frontend/src/app/aluno/historico-frequencia.tsx:84-137` (`useConsultaHistoricoFrequenciaDoAluno`) — hook no arquivo da tela chamando `listarHistoricoFrequenciaDoAluno`, com cleanup `cancelado`. Aqui é mais simples (período fixo), sem a máquina de `chaveAtual`.
- **Cores**: só as já definidas em `tailwind.config.js` (`text-text`, `text-text-secondary`), mesmo critério de `HistoricoFrequenciaCard` — não introduza token novo.

## Contrato de API

Nenhum novo — reusa `GET /alunos/historico-frequencia?inicio=...&fim=...` (issue #16) com `PeriodoConsultaInput` de 30 dias. A Api já aceita `inicio`/`fim` arbitrários (`HistoricoFrequenciaController.Consultar`) e resolve o Aluno pelo token da sessão (sem id de param).

## Modelo de dados

Nenhum — sem migration, sem tabela/coluna nova. Todo dado derivado em runtime a partir do histórico já existente.

## Edge points (não viraram Gherkin; são decisão de implementação)

- **Falha de rede / erro da Api no resumo**: renderiza `null` (o Painel segue com saudação + ações normais). Não mostra `ErrorMessage` — um resumo secundário não deve tomar a tela por uma falha de conexão; silencioso e degradado.
- **`NaoRegistrada`/`Cancelada` ignoradas na contagem**: não somam em "compareceu" nem "faltou"; se todas as aulas do período forem desses dois, o card mostra o estado vazio (mesma mensagem do sem-dados). Consistente com a RN ("compareceu/faltou") e com a tela de histórico, que já as rotula separadamente.
- **Aluno com múltiplos Professores**: `calcularResumo` soma os status entre Professores (um total único do Aluno, sem agrupar por Professor) — o resumo é agregado do Aluno, não por Professor (a tela de histórico detalha por Professor; o resumo resume).
- **Virada da data à meia-noite durante a sessão**: o período é calculado uma vez ao montar; se o relógio virar o dia com o app aberto, o resumo fica com o período do momento em que montou até a próxima montagem — aceitável para um resumo informativo (uma "fotografia").
- **`useIsTelaLarga`/layout**: o card de resumo entra em `flex-row` de largura cheia; em viewport estreita continua `w-full`, sem quebra (mesmo `flex-row` de `HistoricoFrequenciaCard`). Não depende da coluna lateral (issue #161).

## Dependência de outras Tasks

- Não depende de nenhuma Task do épico #127 além do próprio Painel existir (já está). Reusa o contrato e a chamada da issue #16 (`historico-frequencia`) — que já está estável.
