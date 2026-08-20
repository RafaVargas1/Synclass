# Implementation: Seletor de política no cadastro de horário, sem gate (#76)

## Entidades/classes afetadas

- `frontend/src/lib/api/horarios.ts`: novo `export enum TipoMarcacao { Livre = 0, Fixo = 1, Hibrido = 2 }`
  (mesmo padrão de `ModeloAgendamento` em `lib/api/configuracao.ts` — espelha
  `Synclass.Domain.Horarios.TipoMarcacao`, trafega como inteiro). `Horario` e
  `CriarHorarioInput` ganham `tipoMarcacao: TipoMarcacao`.
- `frontend/src/components/organisms/HorarioForm.tsx`: novo `ChipSelector`
  de política, reaproveitando o componente já usado para dia da semana neste
  mesmo arquivo e para modelo em `ModeloAgendamentoForm.tsx`. **Diferença do
  padrão de `diaSemana`**: `diaSemana` nasce com um default (`useState(1)`)
  porque não há requisito de obrigar escolha explícita; `tipoMarcacao` nasce
  `useState<TipoMarcacao | undefined>(undefined)` — nenhum chip vem
  pré-selecionado, e `validar` rejeita `undefined` com uma mensagem nova
  (`MensagemPoliticaObrigatoria = 'Escolha a política de marcação deste horário.'`),
  mesmo estilo das outras mensagens de validação já existentes no arquivo.
- `frontend/src/components/organisms/HorarioCard.tsx`: mostra o rótulo da
  política (`Livre`/`Fixo`/`Híbrido`) junto das outras informações do card —
  reaproveitar o mapa de rótulos que `HorarioForm`/`ModeloAgendamentoForm` já
  definem inline (`{ [TipoMarcacao.Livre]: 'Livre', ... }`), não precisa de
  um componente novo, só texto.
- `frontend/src/app/professor/[professorId]/horarios.tsx`: remove todo o
  bloco do gate — `HorariosProfessorScreen` passa a renderizar diretamente
  `Topbar` + `HorariosConteudo` (sem `useCarregamentoConfiguracao`,
  `EstadoCarregamento`, `ResultadoCarregamento`, `paraEstadoCarregamento`,
  `TelaCarregando`, `TelaErroConfiguracao`, `GateModeloAgendamento`,
  `useDefinirModelo`). Os imports de `obterConfiguracao`/
  `definirModeloAgendamento`/`ModeloAgendamento`/`ModeloAgendamentoForm`
  saem deste arquivo.

## Não remover nesta Task (decisão explícita do card)

`ModeloAgendamentoForm.tsx`, `ModeloAgendamentoForm.test.tsx`,
`lib/api/configuracao.ts` (`obterConfiguracao`/`definirModeloAgendamento`) e
o endpoint `ConfiguracoesController` de modelo no backend ficam órfãos
(sem nenhum consumidor no app) mas não são apagados aqui — `PrazoCancelamentoMinutos`
continua vivendo em `ConfiguracaoProfessor`/`ConfiguracoesController` (issue
#10) e não deve ser removido junto. Remoção do que sobrar órfão é decisão
separada, fora deste escopo.

## Contrato de API

Sem mudança de contrato — `POST /professores/{professorId}/horarios` já
aceita `tipoMarcacao` desde a Task #73 (backend). Esta Task só faz o
frontend passar a enviá-lo de fato (hoje nenhuma tela do app manda esse
campo, então toda criação de horário pela UI atual cairia no valor default
do binding — 0/`Livre` — sem essa Task; achado de dev-review do PR #82
mencionava exatamente essa lacuna, que esta Task fecha).

## Edge points (não cobertos por Gherkin)

- `horarios.tsx` deixa de ter qualquer estado de "carregando"/"erro" de
  configuração — a tela renderiza o formulário imediatamente ao montar,
  sem round-trip prévio. `HorariosConteudo` já lida com o próprio
  carregamento assíncrono da lista de horários (`useGerenciamentoHorarios`),
  isso não muda.
- Nenhuma migração de horários **já cadastrados via essa tela antes desta
  Task** é necessária aqui — isso já foi coberto pela Task #75 (migração de
  dados no backend). Este card só muda o formulário/tela.

## Dependência de outras Tasks

Depende de #73 (campo existe no contrato) e #74/#75 (comportamento e dados
já corretos no backend) — todas mergeadas em `main`.
