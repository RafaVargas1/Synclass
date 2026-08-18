# Task: Aluno marca aula em horário vago (#9)

Card: https://github.com/RafaVargas1/Synclass/issues/9

## Ordem de execução

- [x] `OrigemAlocacao` (enum `Professor|Aluno`) + `AlocacaoHorario.Criar` passa a exigir origem; `AlocarAsync` (Professor, #8) passa `OrigemAlocacao.Professor` — teste existente de #8 continua verde
- [x] Migration: `OrigemAlocacao` em `AlocacoesHorario` (`not null default 0`)
- [x] `IAlocacaoHorarioRepository.PossuiAlocacaoOrigemProfessorAsync` + implementação EF Core + fake em memória (`FakeAlocacaoHorarioRepository`)
- [ ] Teste unidade (Domain): Aluno marca horário no modelo Vago com vaga → sucesso, `OrigemAlocacao.Aluno`
- [ ] Teste unidade (Domain): Aluno tenta marcar no modelo Fixo → `ModeloNaoPermiteMarcacaoLivreException`
- [ ] Teste unidade (Domain): Aluno marca no Híbrido, horário sem atribuição fixa do Professor → sucesso
- [ ] Teste unidade (Domain): Aluno tenta marcar no Híbrido, horário já com alocação `OrigemAlocacao.Professor` → `ModeloNaoPermiteMarcacaoLivreException`
- [ ] Teste unidade (Domain): horário no limite de `LimiteAlunos` → `HorarioLotadoException` (reaproveita `GarantirVagaDisponivelAsync`)
- [ ] Teste unidade (Domain): matrícula não vinculada ao Professor → `MatriculaNaoVinculadaAoProfessorException` (reaproveita)
- [ ] Teste unidade (Domain): Aluno já marcado neste horário → `AlocacaoJaExisteException` (reaproveita)
- [ ] Implementação: `AlocacaoHorarioService.MarcarAsync` + `GarantirModeloPermiteMarcacaoAsync`
- [ ] Teste unidade (Domain): `ListarVagosAsync` no Vago retorna todos os horários com vaga
- [ ] Teste unidade (Domain): `ListarVagosAsync` no Híbrido filtra os horários com atribuição fixa do Professor
- [ ] Teste unidade (Domain): `ListarVagosAsync` no Fixo (ou sem `ConfiguracaoProfessor`) retorna lista vazia, sem lançar
- [ ] Teste unidade (Domain): `ListarVagosAsync` exclui horários sem vaga (`vagasRestantes == 0`)
- [ ] Implementação: `AlocacaoHorarioService.ListarVagosAsync`
- [ ] Teste de fumaça (Api): `POST /professores/{professorId}/horarios/{horarioId}/marcacoes` — sucesso e cada rejeição (400/404)
- [ ] Teste de fumaça (Api): `GET /professores/{professorId}/horarios/vagos?matriculaId=` — lista filtrada
- [ ] Implementação: `MarcacoesHorarioController` (`POST .../marcacoes`, `GET .../horarios/vagos`)
- [ ] Log estruturado: reaproveita `AlunoAlocadoEmHorario`/`AlocacaoRejeitada` (payload já inclui `AlocacaoId`; adicionar `OrigemAlocacao` ao log de criado — ver architecture.md#logs-estruturados-e-track-id)
- [ ] `dotnet format && dotnet test` verde
- [ ] `src/lib/api/marcacoes.ts` (`listarHorariosVagos`, `marcarHorario`)
- [ ] Componente frontend: `HorarioVagoCard` (organism) + tela `aluno/[matriculaId]/professores/[professorId]/horarios.tsx`
- [ ] `npm run lint && npm run typecheck && npm test` verde
- [ ] Editar issue `#23` (ou abrir nova) para incluir débito de sessão real também nas rotas do Aluno criadas aqui
