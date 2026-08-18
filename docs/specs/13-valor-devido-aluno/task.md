# Task: Aluno consulta o total devido com detalhamento por Professor (#13)

Card: https://github.com/RafaVargas1/Synclass/issues/13

Ver `implementation.md` para o contrato de API e a confirmação de reaproveitamento do serviço de
domínio criado na issue #12 (`ConsultaCobrancaService`/`PeriodoConsulta`/`ValorDevidoPorMatricula`).

## Ordem de execução

- [x] Teste unidade (Domain) + implementação: `IUsuarioRepository.BuscarPorIdAsync`
      (`UsuarioRepository` em `Synclass.Infrastructure.Persistence` + `FakeUsuarioRepository`)
- [x] Teste unidade (Domain) + implementação: `IMatriculaRepository.ListarPorAlunoAsync`
      (`MatriculaRepository` + `FakeMatriculaRepository`) — só matrículas plenas do
      `alunoUsuarioId` pedido
- [x] Teste unidade (Domain): `ConsultaCobrancaService.ConsultarPorAlunoAsync` retorna
      `ValorDevidoPorMatricula` com `SemRegraDefinida = true`/`Valor = null` para vínculo sem
      `RegraDeCobranca` (mesmo edge point da #12, agora do lado do Aluno)
- [x] Teste unidade (Domain): `ConsultaCobrancaService.ConsultarPorAlunoAsync` com dois vínculos
      (Professor A com `RegraFixoMensal`, Professor B com `RegraFixoPorAula`) retorna uma entrada
      por Professor, cada uma calculada pela própria strategy, sem somar num total único
- [x] Teste unidade (Domain): `ConsultaCobrancaService.ConsultarPorAlunoAsync` usa o `Nome` do
      Professor (via `IUsuarioRepository.BuscarPorIdAsync(matricula.ProfessorId)`), não o
      `NomeProvisorio` da matrícula
- [x] Teste unidade (Domain): `ConsultaCobrancaService.ConsultarPorAlunoAsync` para Aluno sem
      nenhuma `Matricula` retorna lista vazia (cobre "Aluno provisório sem vínculo pleno")
- [x] Implementação: `ConsultaCobrancaService.ConsultarPorAlunoAsync` + refatorar
      `CalcularParaMatriculaAsync` para receber a resolução de nome como parâmetro (função
      compartilhada entre as duas direções, sem duplicar cálculo/edge point de "sem regra")
- [ ] Teste de fumaça (Api): `GET /alunos/valor-devido` sem `inicio`/`fim` usa mês corrente e
      devolve 200 com a lista de Professores do Aluno autenticado (`ClienteAutenticadoComoAlunoPersistidoAsync`)
- [ ] Teste de fumaça (Api): Aluno vinculado a dois Professores com regras diferentes — a resposta
      tem uma entrada por Professor, cada uma com o valor certo (sem soma)
- [ ] Teste de fumaça (Api): vínculo sem regra aparece com `semRegraDefinida: true`/`valor: null`
- [ ] Teste de fumaça (Api): Aluno autenticado sem nenhuma `Matricula` recebe 200 com lista vazia
- [ ] Teste de fumaça (Api): só `inicio` informado (sem `fim`) devolve 400
- [ ] Implementação mínima: `ValorDevidoAlunoController` (rota `GET /alunos/valor-devido`,
      `[Authorize(Roles = "Aluno")]`, `alunoUsuarioId` de `User.GetUsuarioId()`)
- [ ] Log estruturado: evento `ConsultaTotalDevidoRealizada` (Information, `TrackId`,
      `UsuarioId`, `PeriodoInicio`, `PeriodoFim`)
- [ ] Registro de DI em `Program.cs`: novo parâmetro `IUsuarioRepository` de
      `ConsultaCobrancaService` (se o registro atual não resolver sozinho)
- [ ] Integração `lib/api/valorDevido.ts`: `listarValorDevidoDoAluno(periodo?)`, com teste
- [ ] Rota `frontend/src/app/aluno/valor-devido.tsx` (lista agregada por Professor, sem total
      somado), reaproveitando `ValorDevidoCard`, com teste

## Fora de escopo nesta Task (documentado, não esquecido)

- Navegação/link a partir de `painel/index.tsx` — a tela do Professor (#12) também não foi ligada
  lá (ainda placeholder textual), consistente.
- Simular literalmente "Aluno provisório chamando o endpoint" — não é alcançável sem login (ver
  `implementation.md#edge-points`); coberto pelo caso "Aluno autenticado sem Matricula".
- Qualquer dependência de frequência real/presença (item 14, ainda não implementado).
