# Especificação técnica: Professor consulta o valor devido por cada Aluno (#12)

Card: https://github.com/RafaVargas1/Synclass/issues/12

## Decisão de domínio tomada nesta reflexão (sem usuário disponível para responder — pipeline em lote autônomo)

A issue #11 deixou `IRegraDeCobranca.CalcularValorDevido(int quantidadeDeAulasNoPeriodo)` com a
quantidade como **parâmetro explícito**, propositalmente não resolvido — ver
`docs/specs/11-regra-cobranca/task.md#fora-de-escopo-nesta-task`: "não algo que este Domain
calcula sozinho a partir de `Horario`/presença", deixado para as issues 12/13.

**Decisão**: `quantidadeDeAulasNoPeriodo` é a contagem de ocorrências semanais dos `Horario`s
(templates recorrentes) alocados à Matrícula (via `AlocacaoHorario.MatriculaId` →
`AlocacaoHorario.HorarioId` → `Horario.DiaSemana`) que caem dentro do período consultado —
**não** depende de frequência real/presença (item 14 do backlog, ainda não implementado).

Justificativa, apoiada só em texto já existente no repositório, sem inventar regra nova:

- RN da issue #11: "a regra de cobrança pode ou não depender [de presença real],
  dependendo do modelo escolhido (valor fixo mensal não depende de presença real; **uma
  regra futura** por 'aulas efetivamente frequentadas' dependeria)" — ou seja, as 3 regras
  concretas hoje existentes (`FixoMensal`, `FixoPorAula`, `ValorPorAula`) são, por definição
  atual do domínio, regras que **não** dependem de presença real. `FixoPorAula`/`ValorPorAula`
  multiplicam por "quantidade de aulas", que só pode vir da agenda (Horario recorrente), não
  de um registro de frequência que ainda não existe no sistema.
- Backlog (`requisitos-funcionais.md`, "Notas de modelagem"): "aula" é a ocorrência
  instanciada sob demanda de um "horário" (template recorrente) — para fins de contagem no
  período, isso equivale a contar quantas vezes o dia da semana do `Horario` cai dentro do
  intervalo `[dataInicio, dataFimExclusiva)`, sem precisar instanciar/persistir nenhuma aula.
- Edge point já documentado na #11: mudar a regra não recalcula retroativamente — cálculo é
  sempre sob demanda com a regra vigente, o que essa decisão preserva (a contagem também é
  sob demanda, sem cache/snapshot).

Se no futuro uma regra passar a depender de frequência real (item 14), essa contagem muda de
fonte sem alterar o contrato de `IRegraDeCobranca` — a mesma razão pela qual a #11 deixou
`quantidadeDeAulasNoPeriodo` como parâmetro em vez de calculá-lo internamente.

## Reaproveitamento pela issue #13 (Aluno consulta total devido)

A issue #13 (próximo lote) precisa da mesma lógica de "calcular valor devido por vínculo dado
um período", só filtrando por `AlunoUsuarioId` em vez de `ProfessorId` — RN da própria #13
confirma isso ("mesma leitura de `Matriculas`/`RegrasDeCobranca` da issue #12, filtrada por
`AlunoUsuarioId`"). Por isso o serviço de consulta **não é nomeado nem estruturado em torno do
Professor**:

- `Synclass.Domain.Cobrancas.ConsultaCobrancaService` — nome e namespace não amarrados a
  "Professor". Método desta Task: `ConsultarPorProfessorAsync(Guid professorId, PeriodoConsulta
  periodo, CancellationToken)`. A issue #13 deve adicionar
  `ConsultarPorAlunoAsync(Guid alunoUsuarioId, PeriodoConsulta periodo, CancellationToken)` na
  mesma classe, reaproveitando os métodos privados `CalcularParaMatriculaAsync` e
  `ContarAulasNoPeriodoAsync` — a única diferença entre as duas consultas é qual lista de
  `Matricula` alimenta o loop (`IMatriculaRepository.ListarPorProfessorAsync` vs. um novo
  `ListarPorAlunoAsync`, a ser adicionado na #13).
- `Synclass.Domain.Cobrancas.PeriodoConsulta` — value object de período (não pertence só à
  consulta do Professor), com `MesCorrente(IClock)` e `Criar(DateOnly, DateOnly)` +
  `ContarOcorrencias(DiaSemana)`. Reaproveitável tal e qual pela #13.
- `Synclass.Domain.Cobrancas.ValorDevidoPorMatricula` — DTO de domínio por vínculo (não por
  Professor nem por Aluno), reaproveitável nas duas direções.

## Entidades/contratos envolvidos (nenhuma migration nova)

- `IMatriculaRepository.ListarPorProfessorAsync` (já existe, issue #3/#8).
- `IRegraDeCobrancaRepository.BuscarPorMatriculaAsync` (já existe, issue #11).
- `IAlocacaoHorarioRepository` — **novo método** `ListarPorMatriculaAsync(Guid matriculaId,
  CancellationToken)`, análogo a `ListarPorHorarioAsync` já existente, só invertendo o lado da
  busca. Implementado em `Synclass.Infrastructure.Persistence.AlocacaoHorarioRepository`.
- `IHorarioRepository.BuscarPorIdAsync` (já existe, issue #6) — usado para obter `DiaSemana` de
  cada `Horario` alocado. N+1 é aceitável na escala atual (poucos horários por matrícula); não
  otimizado nesta Task (edge point, ver abaixo).

## Contrato de API

`GET /professores/{professorId:guid}/valor-devido?inicio=yyyy-MM-dd&fim=yyyy-MM-dd`

- `[Authorize(Roles = "Professor")]`, mesmo padrão de autorização das issues #3/#8/#11 —
  `professorId` vem da rota, não de claim (decisão já existente, não revisitada aqui).
- `inicio`/`fim` opcionais (query string, `DateOnly`). Se ausentes, usa `PeriodoConsulta.MesCorrente`
  (primeiro dia do mês corrente, UTC, até o primeiro dia do mês seguinte, exclusivo). Se só um
  dos dois vier, trata como request inválido (400) — não há "meio-padrão".
- Resposta 200:
  ```json
  [
    {
      "matriculaId": "guid",
      "alunoUsuarioId": "guid|null",
      "nome": "string",
      "valor": "decimal|null",
      "semRegraDefinida": "bool"
    }
  ]
  ```
  `valor` é `null` quando `semRegraDefinida = true` (nunca `0` — critério de aceite 3).
  `nome` segue o mesmo "melhor esforço" de `AlunosProvisoriosController.ParaResponse`:
  `matricula.NomeProvisorio ?? string.Empty` (matrícula plena sem nome de Usuario resolvido
  ainda é debito técnico pré-existente, fora do escopo desta Task).
- Lista vazia (não 404) quando o Professor não tem nenhuma Matrícula — consistente com
  `AlunosProvisoriosController.Listar`.

## Logs

Evento `ConsultaValorDevidoRealizada` (Information) emitido pelo controller, uma vez por
chamada (não por matrícula, para não logar volume proporcional a alunos nem valores
monetários): `TrackId`, `ProfessorId`, `PeriodoInicio`, `PeriodoFim`.

## Edge points

- Vínculo sem `RegraDeCobranca`: `SemRegraDefinida = true`, `Valor = null`, nunca `0`
  (critério de aceite 3, já refletido no DTO acima).
- Período: só `inicio` ou só `fim` informado é request inválido (400), não preenchido com
  default parcial.
- `ContarOcorrencias` no `PeriodoConsulta` é O(dias do período) — período de um mês é barato;
  não otimizado com fórmula fechada nesta Task (poderia usar aritmética modular, mas o range é
  pequeno e a legibilidade do loop compensa).
- N+1 em `IHorarioRepository.BuscarPorIdAsync` por alocação: aceitável na escala atual (poucos
  Horarios por Professor); não é um novo método em lote nesta Task — se virar gargalo real,
  fica para uma issue de performance dedicada.
- Isolamento entre Professores (critério de aceite 4): garantido porque
  `IMatriculaRepository.ListarPorProfessorAsync` já escopa por `professorId` — o mesmo Aluno
  vinculado a dois Professores gera duas linhas de `Matricula` distintas (uma por vínculo), e
  a consulta nunca cruza `professorId`.

## Dependência de outras Tasks

Nenhuma — depende só da issue #11 (mergeada). A issue #13 (próximo lote) reaproveita
`ConsultaCobrancaService`/`PeriodoConsulta`/`ValorDevidoPorMatricula` sem precisar mudar o
contrato desta Task (só adiciona um método novo + `IMatriculaRepository.ListarPorAlunoAsync`).
