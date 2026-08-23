# Task: base de contagem (agendamento vs. presença confirmada) para cobrança por aula (#186)

Card: https://github.com/RafaVargas1/Synclass/issues/186

## Ordem de execução

- [x] Teste unidade (Domain, `RegraFixoPorAulaTests`/`RegraValorPorAulaTests`): `Criar` aceita `baseDeContagemAula` opcional, default `Agendamento`
- [x] Implementação mínima: `BaseDeContagemAula` enum + `IRegraComBaseDeContagemAula` + campo em `RegraFixoPorAula`/`RegraValorPorAula`
- [x] Teste unidade (Domain, `RegraDeCobrancaServiceTests`): `DefinirAsync` rejeita `baseDeContagemAula` para `FixoMensal`
- [x] Teste unidade (Domain, `RegraDeCobrancaServiceTests`): `DefinirAsync` aceita e persiste `baseDeContagemAula` para `FixoPorAula`/`ValorPorAula`
- [x] Implementação: `RegraDeCobrancaService.DefinirAsync`/`ConstruirRegra`/`GarantirBaseDeContagemCoerenteComTipo`
- [x] Teste unidade (Infrastructure ou Domain com fake repo): `IRegistroFrequenciaRepository.ContarPresencasNoPeriodoAsync` conta só `StatusFrequencia.Presente` dentro do período
- [x] Implementação: `IRegistroFrequenciaRepository.ContarPresencasNoPeriodoAsync` + `RegistroFrequenciaRepository` (EF)
- [x] Teste unidade (Domain, `ConsultaCobrancaServiceTests`): cenário Gherkin 1 — `BaseDeContagemAula.Agendamento` conta ocorrências agendadas (comportamento atual)
- [x] Teste unidade (Domain, `ConsultaCobrancaServiceTests`): cenário Gherkin 2 — `BaseDeContagemAula.PresencaConfirmada` conta só presenças confirmadas
- [x] Teste unidade (Domain, `ConsultaCobrancaServiceTests`): cenário Gherkin 3 — aula sem frequência registrada não conta em nenhuma base quando `PresencaConfirmada`
- [x] Teste unidade (Domain, `ConsultaCobrancaServiceTests`): cenário Gherkin 4 — `RegraFixoMensal` ignora a base de contagem, valor sempre fixo
- [x] Implementação: `ConsultaCobrancaService.ContarAulasNoPeriodoAsync` ganha o branch por `BaseDeContagemAula`, construtor ganha `IRegistroFrequenciaRepository`
- [x] Migration: coluna `BaseDeContagemAula int NULL` na tabela TPH de `RegraDeCobranca` (via `dotnet ef migrations add`)
- [x] Teste de fumaça (Api, `RegraDeCobrancaEndpointTests`): `PUT`/`GET` aceitam e devolvem `BaseDeContagemAula`
- [x] Teste de fumaça (Api): consulta de valor devido com uma matrícula `Agendamento` e outra `PresencaConfirmada` confirma os totais divergem
- [x] Implementação: `RegraDeCobrancaController` — request/response ganham o campo
- [x] Componente frontend: `frontend/src/lib/api/regraDeCobranca.ts` — `RegraDeCobranca`/`DefinirRegraDeCobrancaInput` ganham `baseDeContagemAula`
- [x] Componente frontend: `RegraDeCobrancaForm.tsx` — `ChipSelector` "Como contar as aulas do período?" visível quando `tipo === 'ValorPorAula' || tipo === 'FixoPorAula'`, opções "Todas as aulas agendadas"/"Só aulas com presença confirmada", default "Todas as aulas agendadas"
- [x] Rodar gate completo (`dotnet format && dotnet test`, `npm run lint && npm run typecheck && npm test`) antes do PR
