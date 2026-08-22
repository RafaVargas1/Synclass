# Implementação: resumo do valor a receber do mês no Painel do Professor (#166)

Tarefa apenas de **frontend**: o backend já expõe o cálculo deste mês em
`GET /professores/{professorId}/valor-devido` com `inicio`/`fim` ausentes
(`ValorDevidoController.TentarResolverPeriodo` usa
`PeriodoConsulta.MesCorrente(_clock)` nesse caso), e a tela "Ver valor
devido" com o filtro "Este mês" (`modo: 'mes'`) chama exatamente essa
variante sem query string. O resumo reaproveita a mesma chamada e só soma
o que a Api já calculou — **nenhuma lógica de cálculo nova no frontend**.

## Contrato de dados (o que o Painel reutiliza)

- `listarValorDevido(professorId: string, periodo?: PeriodoConsultaInput)`
  em `frontend/src/lib/api/valorDevido.ts` (issue #12). Sem `periodo`, monta
  `/professores/{professorId}/valor-devido` **sem** `inicio`/`fim` — é isso
  que o filtro "Este mês" da tela `valor-devido.tsx` faz (`periodoDoModo`
  retorna `undefined` para `'mes'`, linha 132), e o backend responde o mês
  corrente. **Não passar `calcularPeriodoTodos` nem nenhum período pessoal**
  no resumo: isso mudaria o contrato para "todos os últimos 5 anos" e
  quebraria o cenário 3 (o valor deixaria de bater com "Este mês").
- O `professorId` no Painel é o próprio `usuarioId` do perfil logado (`GET
  /usuarios/me` via `usePerfilLogado`): não existe entidade `Professor`
  separada — ver comentário em `frontend/src/lib/secoesPorPapel.ts`
  (secoesProfessor): "`professorId` é o mesmo `Usuario.Id` do Professor
  logado". Logo, `listarValorDevido(usuarioId)` é o id correto do
  Professor.
- Cada item é `{ matriculaId, alunoUsuarioId, nome, valor: number | null,
  semRegraDefinida }`. `valor` é `null` quando `semRegraDefinida` é true,
  nunca `0` (issue #12). O total do mês = soma apenas dos `valor` não nulos;
  se a lista é vazia **ou** todos os `valor` são `null`, o total é `0` →
  estado "valor zerado" do cenário 2.

## Componente novo: `frontend/src/components/organisms/ResumoValorReceber.tsx`

Organismo **de leitura e não-tocável**: um `View` puro, **não** um
`Pressable`/`Link`. O resumo é um dado informativo, não uma ação — a ação
"Ver valor devido" já existe como `CardDeAcao` abaixo. Por isso **não se
aplica** a regra de alvo de toque (`docs/spec/ux-heuristics.md#alvos-de-toque`)
nem `AlvoDeToqueMinimo`: não há área tocável aqui, adicioná-la seria enganoso
(o card pareceria clicável sem sê-lo).

Props: `total: number` (já somado pelo Painel). O card renderiza dois
estados:

```tsx
// assinatura
export function ResumoValorReceber({ total }: { total: number }) {
```

Layout seguindo o padrão de card dos organisms (mesma família visual de
`ValorDevidoCard.tsx:19` — borda `background-selected`, fundo
`background-element`, raio `rounded-medium`, padding `px-four py-three`,
`w-full`; elevação por contraste de fundo + borda de 1px, sem sombra, ver
`docs/spec/design-system.md#forma-e-elevação`):

```tsx
<View className="w-full gap-one rounded-medium border border-background-selected bg-background-element px-four py-three dark:border-dark-background-selected dark:bg-dark-background-element">
  <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">A receber este mês</Text>
  {total > 0 ? (
    <Text className="text-2xl font-bold text-text dark:text-dark-text">
      {total.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
    </Text>
  ) : (
    <Text className="text-sm text-text-secondary dark:text-dark-text-secondary">
      Nenhum valor a receber neste mês.
    </Text>
  )}
</View>
```

- **Rótulo/copy exato** (não delegar pra DeepSeek):
  - Rótulo: `A receber este mês` (termo "receber" já é o do produto — a tela
    do Professor se chama "Valor devido por Aluno" mas o conceito do Painel
    é "quanto vou **receber**" (história de usuário #166 e épico #127); usar
    `a receber` no rótulo, consistente com o objetivo do card, não "devido").
  - Valor zerado: `Nenhum valor a receber neste mês.` (cenário 2 — mensagem
    clara, não seção vazia).
  - Formatação do valor: mesmo padrão já usado no produto em
    `ValorDevidoCard.tsx:32` — `toLocaleString('pt-BR', { style: 'currency',
    currency: 'BRL' })`. Reuse esse formato inline no novo organismo; **não**
    crie função utilitária `formatarValor` só pra isso (um único call site
    novo não justifica extrair um helper — regra de não duplicar infra antes
    da 2ª necessidade, ver `docs/spec/code-style.md#estrutura`; se uma 3ª
    necessidade surgir, aí sim extrair e atualizar os dois usos).
- **Hierarquia visual (por quê o rótulo fica acima e o valor grande embaixo)**:
  `docs/spec/ux-heuristics.md#agrupamento-visual-gestalt` — rótulo e valor
  ficam próximos (gap `gap-one`) formando um grupo; o valor em `text-2xl
  font-bold` lê primeiro que os `CardDeAcao` abaixo (que usam `text-base
  font-semibold`), o que comunica "destaque de dado" sem disputar peso de
  ação com os links (Nielsen #4, consistência: o resumo é card de dado como
  os demais cards, mas maior hierarquicamente — dado primeiro, ação depois).

## `frontend/src/app/painel/index.tsx` — ponto de inserção exato

Hoje (`painel/index.tsx`, corpo entre o `TopbarAutenticada` e o fim do
`SafeAreaView`):

```tsx
  <View
    className="w-full flex-1 self-center gap-five px-four py-five"
    style={{ maxWidth: MaxContentWidthPainel }}
  >
    {nome ? <Saudacao nome={nome} /> : null}
    <View className="w-full flex-row flex-wrap gap-three">
      {acoes.map((acao) => (
        <CardDeAcao key={acao.label} acao={acao} />
      ))}
    </View>
  </View>
```

Mudança: inserir o resumo **entre** `{nome ? <Saudacao nome={nome} /> :
null}` e o bloco `<View className="w-full flex-row flex-wrap gap-three">`
dos `CardDeAcao` — o dado informativo vem antes da lista de atalhos (dado
primeiro, ação depois, ver heurística acima). Condições para mostrar: só
quando `papelAtivo === 'Professor'` **e** `usuarioId` resolvido (o
`professorId` é o `usuarioId`; sem ele não há consulta possível — mesma
condição que `secoesProfessor` já usa em `secoesPorPapel.ts` para só expor
os links do Professor depois de `usuarioId`). Não mostrar em papel Aluno.

```tsx
  <View
    className="w-full flex-1 self-center gap-five px-four py-five"
    style={{ maxWidth: MaxContentWidthPainel }}
  >
    {nome ? <Saudacao nome={nome} /> : null}
    {papelAtivo === 'Professor' && usuarioId ? (
      <ResumoValorReceberComConsulta professorId={usuarioId} />
    ) : null}
    <View className="w-full flex-row flex-wrap gap-three">
      {acoes.map((acao) => (
        <CardDeAcao key={acao.label} acao={acao} />
      ))}
    </View>
  </View>
```

`ResumoValorReceberComConsulta` é o hook de estado do resumo no próprio
arquivo do Painel (mesma separação de `valor-devido.tsx`: a tela compõe o
estado de consulta, o card é um organismo puro de apresentação — padrão
`ValorDevidoCard`, que não recebe a chamada de rede):

```tsx
function ResumoValorReceberComConsulta({ professorId }: { professorId: string }) {
  const [resultado, setResultado] = useState<ListarValorDevidoResultado | undefined>(undefined);

  useEffect(() => {
    let cancelado = false;
    listarValorDevido(professorId).then((dados) => {
      if (!cancelado) setResultado(dados);
    });
    return () => {
      cancelado = true;
    };
  }, [professorId]);

  if (!resultado) {
    return <ActivityIndicator accessibilityLabel="Carregando" />;
  }
  if (!resultado.sucesso) {
    return <ErrorMessage>{resultado.mensagem}</ErrorMessage>;
  }
  const total = resultado.valoresDevidos.reduce(
    (soma, item) => (item.valor === null ? soma : soma + item.valor),
    0,
  );
  return <ResumoValorReceber total={total} />;
}
```

A chamada **sem** `periodo` garante o cenário 3: é exatamente o que o filtro
"Este mês" da tela `valor-devido.tsx` gera (`periodoDoModo('mes', ...)` →
`undefined`), então o total somado aqui é a soma dos mesmos itens exibidos
lá com o filtro "Este mês". Importar `listarValorDevido` e
`ListarValorDevidoResultado` de `@/lib/api/valorDevido`; `useState`/`useEffect`
de react; `ActivityIndicator` de `react-native`; `ErrorMessage` de
`@/components/atoms/ErrorMessage`. Estados de carregamento/erro seguem o
mesmo tratamento de `ConteudoDaConsulta` em `valor-devido.tsx:84-92`
(`ActivityIndicator` com `accessibilityLabel="Carregando"`, `ErrorMessage`),
sem skeleton (guardrail 21, `design-system.md`).

## Testes

`frontend/src/app/painel/index.test.tsx` já mocka `useSessao` e `buscarPerfil`
(define `usuarioId`/`nome`; o `beforeEach` atual defaulta
`buscarPerfilMock` para `{ sucesso: false }` e o describe de saudação seta
`{ sucesso: true, usuarioId: 'prof-1', nome: 'Ana' }`). Adicione mock de
`@/lib/api/valorDevido` (`listarValorDevido` como `jest.fn()`). Cenários:

1. **Com valor**: `useSessao` com `papelAtivo: 'Professor'`, `buscarPerfil`
   devolve `usuarioId: 'prof-1'`, `listarValorDevido` resolve com
   `[{ valor: 300, semRegraDefinida: false }, { valor: 450,
   semRegraDefinida: false }]` → tela mostra `R$ 750,00` e o rótulo
   `A receber este mês`.
2. **Lista vazia**: `listarValorDevido` resolve `[]` → tela não mostra valor
   nem `R$ 0,00`; mostra `Nenhum valor a receber neste mês.`.
3. **Todos sem regra**: itens com `semRegraDefinida: true` e `valor: null`
   → mesma mensagem do cenário 2 (não soma `null` como 0 exibido).
4. **Contrato "Este mês" (cenário 3)**: assert que `listarValorDevido` é
   chamado com `('prof-1')` e que nenhuma chamada inclui query string — o
   mesmo comportamento do filtro "Este mês". (Já coberto em
   `valorDevido.test.ts` para a lib; aqui garante que o Painel não passa
   período próprio.)
5. **Papel Aluno**: `useSessao` com `papelAtivo: 'Aluno'` → não aparece
   resumo (`queryByText('A receber este mês')` é `null`) — ver descrição de
   teste no describe de saudação que já exercita Aluno.

Padrão de estilo a seguir: o próprio `painel/index.test.tsx` (mock de
`useSessao`/`buscarPerfil`/rotas e renders da tela real) é o precedente do
repo pra testar tela com estado; o mock de `listarValorDevido` segue o
mesmo molde do mock de `buscarPerfil` já presente (`jest.mock('@/lib/api/
valorDevido', () => ({ listarValorDevido: jest.fn() }))`).

## Padrão de estilo a seguir

- Card: siga o padrão de **`ValorDevidoCard.tsx:19`** (borda
  `background-selected` + fundo `background-element` + `rounded-medium` +
  `px-four py-three` + `w-full`) — não crie um estilo de card novo.
- Formatação de valor: siga **`ValorDevidoCard.tsx:32`**
  (`toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })`).
- Consulta com estado na tela: siga o padrão de `valor-devido.tsx`
  (`ConteudoDaConsulta`/hook `useConsultaValorDevido`) — reutilize o mesmo
  tratamento de carregando/erro. Para este caso pequeno, o hook pode ser
  enxuto (como o bloco acima) em vez de replicar `useConsultaValorDevido`
  inteiro — chamada única sem filtros, guarda por `cancelado`/cleanup na
  desmontagem é o essencial.

## Edge points

- **Quando o resumo SOME do layout, ele desmonta** (condição no JSX)
  — não há estado global a limpar; o cleanup do `useEffect` é suficiente.
- **Alternância de papel Professor↔Aluno**: ao trocar para Aluno, o resumo
  deixa de renderizar (condição `papelAtivo === 'Professor'`); ao voltar
  para Professor, remonta e refaz a consulta. Aceitável — chamada barata,
  sem cache pretendido.
- **`total === 0` com Alunos com regra mas valor calculado 0**: um Aluno
  com regra `FixoMensal` de R$ 0,00 no mês tem `valor: 0` (não `null`). Soma
  `0` e o total fica `0` → cai na mensagem de zerado, coerente (não há
  realmente valor a receber). Não é o mesmo caso de `semRegraDefinida` —
  ali `valor` é `null` e também não soma, resultado idêntico pra soma.
- **Professor recém-cadastrado sem Alunos**: `valoresDevidos` vem `[]` do
  backend (não `null` — `buscarValorDevido` normaliza
  `(corpo ?? [])`), cai no estado de zerado com mensagem. OK.

## Fora de escopo

- Não tocar `ValorDevidoCard.tsx`, `valor-devido.tsx` nem `valorDevido.ts` —
  são fonte do dado, só reutilizados.
- Não criar endpoint nem alterar contrato no backend.
- Não implementar resumo de "próximos horários" nem "frequência recente"
  (outras Tasks do épico #127, não esta).
- Não mostrar resumo para o papel Aluno (esta Task é do Professor; o
  resumo de "quanto devo" do Aluno é outra Task do épico).
- Sem precedente de "resumo/dashboard de valor" no Painel hoje — este é o
  primeiro card de dado informativo do Painel (até agora o Painel só tinha
  saudação + `CardDeAcao` de navegação); decisão de layout nova, mas apoiada
  no padrão de card de dado já existente em `ValorDevidoCard`.
