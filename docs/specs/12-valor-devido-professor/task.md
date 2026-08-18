# Task: Professor consulta o valor devido por cada Aluno (#12)

Card: https://github.com/RafaVargas1/Synclass/issues/12

Ver `implementation.md` para o contrato de API, decisão de domínio sobre
`quantidadeDeAulasNoPeriodo` e a estrutura pensada para reaproveitamento pela issue #13.

## Ordem de execução

- [x] Teste unidade (Domain): `PeriodoConsulta.Criar` com `inicio >= fim` rejeita com
      `PeriodoConsultaInvalidoException`
- [x] Teste unidade (Domain): `PeriodoConsulta.MesCorrente(clock)` retorna primeiro dia do mês
      corrente até primeiro dia do mês seguinte (exclusivo), usando `FixedClock`
- [x] Teste unidade (Domain): `PeriodoConsulta.ContarOcorrencias(diaSemana)` conta corretamente
      quantas vezes um dia da semana cai num período de um mês (casos: mês com 4 e com 5
      ocorrências do mesmo dia da semana)
- [x] Implementação mínima: `PeriodoConsulta` (`Synclass.Domain.Cobrancas`) + exceção
- [x] Teste unidade (Domain): `ConsultaCobrancaService.ConsultarPorProfessorAsync` retorna
      `ValorDevidoPorMatricula` com `SemRegraDefinida = true` e `Valor = null` para matrícula
      sem `RegraDeCobranca` (nunca `0`)
- [x] Teste unidade (Domain): `ConsultaCobrancaService.ConsultarPorProfessorAsync` com matrícula
      com `RegraFixoMensal` retorna `Valor` fixo, independente de quantas `AlocacaoHorario`
      existem
- [x] Teste unidade (Domain): `ConsultaCobrancaService.ConsultarPorProfessorAsync` com matrícula
      com `RegraFixoPorAula` e 2 `Horario`s alocados (ex: terça e quinta) retorna
      `Valor * quantidadeDeOcorrenciasNoPeriodo` somada dos dois horários
- [x] Teste unidade (Domain): `ConsultaCobrancaService.ConsultarPorProfessorAsync` só retorna
      Matrículas do `professorId` pedido — outra Matrícula de outro Professor (mesmo
      `AlunoUsuarioId`, vínculo N:N) não aparece nem influencia o valor (critério de aceite 4)
- [x] Implementação mínima: `ValorDevidoPorMatricula` (record) + `ConsultaCobrancaService`
      (`Synclass.Domain.Cobrancas`) usando `IMatriculaRepository`, `IRegraDeCobrancaRepository`,
      `IAlocacaoHorarioRepository`, `IHorarioRepository`
- [x] Implementação: `IAlocacaoHorarioRepository.ListarPorMatriculaAsync` (interface +
      `AlocacaoHorarioRepository` em `Synclass.Infrastructure.Persistence`)
- [x] Teste de fumaça (Api): `GET /professores/{professorId}/valor-devido` sem `inicio`/`fim`
      usa mês corrente e devolve 200 com a lista de Alunos do Professor
- [x] Teste de fumaça (Api): matrícula sem regra aparece com `semRegraDefinida: true` e
      `valor: null` no corpo da resposta
- [x] Teste de fumaça (Api): só `inicio` informado (sem `fim`) devolve 400
- [x] Teste de fumaça (Api): Aluno vinculado a dois Professores diferentes — cada Professor
      consulta e vê só o próprio valor, calculado pela própria regra (isolamento, critério de
      aceite 4)
- [x] Implementação mínima: `ValorDevidoController` (rota acima), DTOs de request/response
- [x] Log estruturado: evento `ConsultaValorDevidoRealizada` (Information, `TrackId`,
      `ProfessorId`, `PeriodoInicio`, `PeriodoFim`) emitido pelo controller — ver
      architecture.md#logs-estruturados-e-track-id
- [x] Registro de DI em `Program.cs`: `ConsultaCobrancaService`,
      `IAlocacaoHorarioRepository`/`AlocacaoHorarioRepository` (se ainda não scoped)
- [ ] Integração `lib/api/valorDevido.ts` (cliente HTTP do contrato acima)
- [ ] Componente frontend: tela de listagem de Alunos do Professor com valor devido +
      seletor de período (mês corrente por padrão), mesmo padrão de estados
      (carregando/falha/carregada) da tela `regra-de-cobranca.tsx`, com teste
- [ ] Rota `frontend/src/app/professor/[professorId]/valor-devido.tsx`, com teste

## Fora de escopo nesta Task (documentado, não esquecido)

- Consulta do lado do Aluno (issue #13, próximo lote) — reaproveita
  `ConsultaCobrancaService`/`PeriodoConsulta`/`ValorDevidoPorMatricula` sem mudar o contrato
  desta Task (ver `implementation.md#reaproveitamento-pela-issue-13`).
- Resolver nome de Usuario para matrícula plena sem `NomeProvisorio` — débito técnico
  pré-existente de `AlunosProvisoriosController`, não introduzido nem resolvido aqui.
- Otimização de N+1 em `IHorarioRepository.BuscarPorIdAsync` por alocação — aceitável na escala
  atual (ver `implementation.md#edge-points`).
- Qualquer dependência de frequência real/presença (item 14, ainda não implementado).
