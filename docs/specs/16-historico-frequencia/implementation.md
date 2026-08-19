# Desenho técnico — Aluno acompanha seu histórico de frequência (#16)

## Entidades/classes afetadas

**`Synclass.Domain.Cobrancas.PeriodoConsulta`** (issue #12/#13, já
reaproveitável tal e qual — ver seu próprio doc-comment): ganha
`GerarDatas(DiaSemana diaSemana)`, irmão de `ContarOcorrencias` — mesmo
laço `for (data = Inicio; data < FimExclusivo; data = data.AddDays(1))`,
mas retorna `IEnumerable<DateOnly>` (`yield return`) em vez de contar.

**`Synclass.Domain.Frequencias`**:

- `StatusHistoricoFrequencia` (enum novo): `NaoRegistrada`, `Presente`,
  `Ausente`, `Cancelada`. Distinto de `StatusFrequencia` (issue #14, só
  `Presente`/`Ausente`, sem `null` explícito no enum) — o histórico
  precisa dos 4 estados visíveis ao Aluno, então não reaproveita o enum
  existente.
- `AulaFrequenciaHistorico` (record): `HorarioId`, `Data`, `DiaSemana`,
  `HoraInicio`, `Status`.
- `HistoricoFrequenciaPorProfessor` (record): `ProfessorId`, `NomeProfessor`,
  `Aulas` (`IReadOnlyCollection<AulaFrequenciaHistorico>`) — mesma forma de
  agregação por Professor de `ValorDevidoPorMatricula` (issue #13), RN
  idêntica (nunca somar/misturar entre Professores).
- `FrequenciaService.ListarHistoricoAsync(alunoUsuarioId, periodo, ct)`:
  1. `IMatriculaRepository.ListarPorAlunoAsync(alunoUsuarioId, ct)`
     (reaproveitado de `ConsultaCobrancaService.ConsultarPorAlunoAsync`,
     issue #13) — só matrículas plenas (`AlunoUsuarioId` setado).
  2. Para cada `Matricula`: `IUsuarioRepository.BuscarPorIdAsync(matricula.ProfessorId,
     ct)` para o nome (mesmo padrão de `ResolverNomeProfessorAsync`, issue
     #13); `IAlocacaoHorarioRepository.ListarPorMatriculaAsync(matricula.Id,
     ct)` para os horários alocados (reaproveitado de
     `AulaService.ListarProximasAsync`, issue #10).
  3. Para cada `AlocacaoHorario`: busca o `Horario`
     (`IHorarioRepository.BuscarPorIdAsync`) e gera as datas do período via
     `periodo.GerarDatas(horario.DiaSemana)`.
  4. Para cada data: `CalcularStatusAsync(horarioId, data, matriculaId, ct)`
     — leitura pura, **nunca** chama `ObterOuCriarAulaAsync` (histórico não
     instancia `Aula` como efeito colateral de uma consulta):
     - `IAulaRepository.BuscarPorHorarioEDataAsync` → se `null`,
       `NaoRegistrada` (nenhuma fonte pode ter registrado nada sem a
       `Aula` existir).
     - Se a `Aula` existe: `ICancelamentoAulaRepository.BuscarAsync(aula.Id,
       matriculaId, ct)` → se existir, `Cancelada` (edge point do card —
       teste antes de olhar `RegistroFrequencia`, cancelamento tem
       prioridade sobre qualquer outro status).
     - Senão: `IRegistroFrequenciaRepository.BuscarAsync(aula.Id,
       matriculaId, ct)` → `StatusProfessor is null` (linha não existe ou
       existe só com confirmação do Aluno) → `NaoRegistrada`;
       `StatusProfessor == Presente` → `Presente`; `StatusProfessor ==
       Ausente` → `Ausente`. `ConfirmadoPeloAluno` nunca é lido aqui — só
       `StatusProfessor` decide (RN do card: "é o registro do Professor
       que prevalece").
  5. Agrupa por `Matricula`/Professor, retorna a lista de
     `HistoricoFrequenciaPorProfessor`.

**Decisão de implementação (custo aceito)**: a combinação "uma `Aula`
existente + um `CancelamentoAula` existente + um `RegistroFrequencia`
existente" por data gerada é O(N) buscas individuais (sem lote), mesmo
padrão já aceito em `ConsultaCobrancaService.ContarAulasNoPeriodoAsync`
(issue #12, comentário "aceitável na escala atual") — nenhum dos três
repositórios expõe hoje um método de listagem em lote por período. Não
introduzir esses métodos de lote agora extrapola o escopo Gherkin deste
card; registrar como débito técnico se a paginação virar problema real de
performance.

**`Synclass.Api`**: novo controller `HistoricoFrequenciaController`
(mesmo padrão de `ValorDevidoAlunoController`, issue #13):
- `[Authorize(Roles = "Aluno")]`, `[Route("alunos/historico-frequencia")]`.
- `GET` com `[FromQuery] DateOnly? inicio, DateOnly? fim` — mesmo
  `TentarResolverPeriodo`/`PeriodoConsulta.MesCorrente` default de
  `ValorDevidoAlunoController` (copiar o método privado, não extrair para
  compartilhado — é pequeno e a duplicação já existe entre
  `ValorDevidoController`/`ValorDevidoAlunoController`, mesmo padrão
  aceito no projeto).
- Log `HistoricoFrequenciaConsultado` (Information,
  `{TrackId} {UsuarioId} {PeriodoInicio} {PeriodoFim}`).
- Registro de DI: `FrequenciaService` já registrado (issues #14/#15) ganha
  as novas dependências (`IMatriculaRepository`, `IUsuarioRepository`,
  `IHorarioRepository`, `IAulaRepository` já devem estar registrados por
  outros services — só adicionar ao construtor).

**Frontend**: `src/lib/api/historicoFrequencia.ts`
(`listarHistoricoFrequenciaDoAluno`, mesmo envelope `{sucesso, mensagem}`
e contrato de período de `valorDevido.ts`). Tela
`src/app/aluno/historico-frequencia.tsx` (sem segmento `[professorId]`,
mesmo padrão de `valor-devido.tsx`) usando `SeletorDePeriodo` extraído
(ver task.md) e organism novo `HistoricoFrequenciaCard.tsx` (aula + status,
cor/texto por `StatusHistoricoFrequencia`).

## Contrato de API

- `GET /alunos/historico-frequencia?inicio=&fim=` (ambos opcionais, mesmo
  contrato de `/alunos/valor-devido`)
  - 200: `[{ "professorId": guid, "nomeProfessor": string, "aulas": [{
    "horarioId": guid, "data": "yyyy-MM-dd", "diaSemana": int,
    "horaInicio": "HH:mm:ss", "status": "NaoRegistrada" | "Presente" |
    "Ausente" | "Cancelada" }] }]`.
  - 400: `{ "mensagem": string }` — período incompleto ou invertido.

## Modelo de dados

Nenhuma migration nova — leitura pura sobre `RegistrosFrequencia` (issue
#14), `Aulas`/`CancelamentosAula` (issue #10), `AlocacoesHorario`
(issue #8) e `Matriculas`.

## Edge points (não cobertos por Gherkin)

- Uma aula agendada sem `Aula` nem `RegistroFrequencia` nenhum → `NaoRegistrada`,
  nunca `Ausente` por padrão (RN explícita do card, AC5).
- Aula cancelada pelo próprio Aluno → `Cancelada`, estado à parte do enum
  presente/ausente/não registrada, checado antes de olhar
  `RegistroFrequencia` (passo 4 acima).
- Consulta é 100% leitura: nunca chama `ObterOuCriarAulaAsync` — uma data
  sem `Aula` instanciada é simplesmente `NaoRegistrada`, sem criar
  side-effect no banco por causa de uma consulta.
- Datas futuras dentro do período consultado (ex.: Aluno consulta o mês
  corrente antes dele terminar): aparecem como `NaoRegistrada` também,
  sem tratamento especial — mesma leitura de "nenhuma fonte registrou
  nada ainda", que é literalmente verdade para o futuro.

## Dependência de issues anteriores

Depende de `RegistroFrequencia` (issue #14), `ConfirmadoPeloAluno` (issue
#15, embora não seja lido aqui — só documentado por que não é), `Aula`/
`CancelamentoAula` (issue #10), `PeriodoConsulta`/`ConsultaCobrancaService`
(issues #12/#13, reaproveitados por padrão, não por dependência de dados).
