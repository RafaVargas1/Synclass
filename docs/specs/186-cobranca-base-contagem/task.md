# Task: base de contagem (agendamento vs. presença confirmada) para cobrança por aula (#186)

Card: https://github.com/RafaVargas1/Synclass/issues/186

## Ordem de execução

- [ ] Teste unidade (Domain, `RegraFixoPorAulaTests`/`RegraValorPorAulaTests`): `Criar` aceita `baseDeContagemAula` opcional, default `Agendamento`
- [ ] Implementação mínima: `BaseDeContagemAula` enum + `IRegraComBaseDeContagemAula` + campo em `RegraFixoPorAula`/`RegraValorPorAula`
- [ ] Teste unidade (Domain, `RegraDeCobrancaServiceTests`): `DefinirAsync` rejeita `baseDeContagemAula` para `FixoMensal`
- [ ] Teste unidade (Domain, `RegraDeCobrancaServiceTests`): `DefinirAsync` aceita e persiste `baseDeContagemAula` para `FixoPorAula`/`ValorPorAula`
- [ ] Implementação: `RegraDeCobrancaService.DefinirAsync`/`ConstruirRegra`/`GarantirBaseDeContagemCoerenteComTipo`
- [ ] Teste unidade (Infrastructure ou Domain com fake repo): `IRegistroFrequenciaRepository.ContarPresencasNoPeriodoAsync` conta só `StatusFrequencia.Presente` dentro do período
- [ ] Implementação: `IRegistroFrequenciaRepository.ContarPresencasNoPeriodoAsync` + `RegistroFrequenciaRepository` (EF)
- [ ] Teste unidade (Domain, `ConsultaCobrancaServiceTests`): cenário Gherkin 1 — `BaseDeContagemAula.Agendamento` conta ocorrências agendadas (comportamento atual)
- [ ] Teste unidade (Domain, `ConsultaCobrancaServiceTests`): cenário Gherkin 2 — `BaseDeContagemAula.PresencaConfirmada` conta só presenças confirmadas
- [ ] Teste unidade (Domain, `ConsultaCobrancaServiceTests`): cenário Gherkin 3 — aula sem frequência registrada não conta em nenhuma base quando `PresencaConfirmada`
- [ ] Teste unidade (Domain, `ConsultaCobrancaServiceTests`): cenário Gherkin 4 — `RegraFixoMensal` ignora a base de contagem, valor sempre fixo
- [ ] Implementação: `ConsultaCobrancaService.ContarAulasNoPeriodoAsync` ganha o branch por `BaseDeContagemAula`, construtor ganha `IRegistroFrequenciaRepository`
- [ ] Migration: coluna `BaseDeContagemAula int NULL` na tabela TPH de `RegraDeCobranca` (via `dotnet ef migrations add`)
- [ ] Teste de fumaça (Api, `RegraDeCobrancaEndpointTests`): `PUT`/`GET` aceitam e devolvem `BaseDeContagemAula`
- [ ] Teste de fumaça (Api): consulta de valor devido com uma matrícula `Agendamento` e outra `PresencaConfirmada` confirma os totais divergem
- [ ] Implementação: `RegraDeCobrancaController` — request/response ganham o campo
- [ ] Componente frontend: `frontend/src/lib/api/regraDeCobranca.ts` — `RegraDeCobranca`/`DefinirRegraDeCobrancaInput` ganham `baseDeContagemAula`
- [ ] Componente frontend: `RegraDeCobrancaForm.tsx` — `ChipSelector` "Como contar as aulas do período?" visível quando `tipo === 'ValorPorAula' || tipo === 'FixoPorAula'`, opções "Todas as aulas agendadas"/"Só aulas com presença confirmada", default "Todas as aulas agendadas"
- [ ] Rodar gate completo (`dotnet format && dotnet test`, `npm run lint && npm run typecheck && npm test`) antes do PR
