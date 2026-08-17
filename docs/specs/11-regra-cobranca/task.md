# Task: Professor define regra de cobrança dos seus Alunos (#11)

Card: https://github.com/RafaVargas1/Synclass/issues/11

## Ordem de execução

- [x] Teste unidade (Domain): `RegraFixoMensal.CalcularValorDevido` retorna sempre `Valor`, independente de `quantidadeDeAulasNoPeriodo`
- [x] Implementação mínima: `IRegraDeCobranca` (contrato: `Id`, `MatriculaId`, `CalcularValorDevido(int quantidadeDeAulasNoPeriodo)`) + classe abstrata `RegraDeCobranca` (estado comum) + `RegraFixoMensal`
- [x] Teste unidade (Domain): `RegraFixoPorAula.CalcularValorDevido` retorna `Valor * quantidadeDeAulasNoPeriodo`
- [x] Implementação mínima: `RegraFixoPorAula`
- [x] Teste unidade (Domain): `RegraValorPorAula.CalcularValorDevido` retorna `Valor * quantidadeDeAulasNoPeriodo` (mesma fórmula de `RegraFixoPorAula`, mas carrega `FrequenciaSemanalContratada` — regressão que garante que o valor da faixa 3x/semana não se confunde com o de 2x/semana quando duas instâncias da regra têm `Valor` diferente para `FrequenciaSemanalContratada` diferente)
- [x] Teste unidade (Domain): `RegraValorPorAula` construída com `FrequenciaSemanalContratada` fora de 1-7 rejeita com `FrequenciaSemanalContratadaInvalidaException`
- [x] Implementação mínima: `RegraValorPorAula` + validação de `FrequenciaSemanalContratada`
- [x] Teste unidade (Domain): teste parametrizado que instancia `RegraValorPorAula`, `RegraFixoMensal`, `RegraFixoPorAula` e chama `CalcularValorDevido` pela mesma referência `IRegraDeCobranca` — garante contrato estável entre implementações (critério de aceite 4)
- [x] Teste unidade (Domain): `RegraDeCobrancaService.DefinirAsync` cria uma regra nova quando a matrícula não tem nenhuma
- [x] Teste unidade (Domain): `RegraDeCobrancaService.DefinirAsync` substitui a regra existente (upsert, FK única em `MatriculaId`) quando a matrícula já tem uma regra de outro tipo
- [x] Teste unidade (Domain): `RegraDeCobrancaService.DefinirAsync` para matrícula inexistente rejeita com `MatriculaNaoEncontradaException`
- [x] Implementação mínima: `RegraDeCobrancaService` (`Synclass.Domain.Cobrancas`) + `IRegraDeCobrancaRepository`
- [x] Migration `CriaRegraDeCobranca`: tabela `RegrasDeCobranca` (TPH, discriminador `Tipo`), `RegraDeCobrancaConfiguration` com `HasDiscriminator`, índice único em `MatriculaId`
- [x] Implementação: `RegraDeCobrancaRepository` (`Synclass.Infrastructure.Persistence`)
- [x] Teste de fumaça (Api): `PUT /professores/{professorId}/matriculas/{matriculaId}/regra-de-cobranca` com `tipo=ValorPorAula` e `frequenciaSemanalContratada` válido devolve 200 com o corpo da regra criada
- [x] Teste de fumaça (Api): mesmo endpoint com `matriculaId` que não pertence a `professorId` (ou inexistente) devolve 404
- [x] Teste de fumaça (Api): `GET /professores/{professorId}/matriculas/{matriculaId}/regra-de-cobranca` sem regra configurada devolve 404 (critério de aceite 1 — "sistema indica que nenhuma regra foi configurada")
- [x] Teste de fumaça (Api): `GET` após um `PUT` bem-sucedido devolve 200 com os dados da regra vigente
- [x] Implementação mínima: `RegraDeCobrancaController` (rotas acima), DTOs de request/response
- [x] Log estruturado: evento `RegraDeCobrancaDefinida` (Information, `TrackId`, `MatriculaId`, `Tipo`, `ValorAnterior` nullable) emitido pelo controller — ver architecture.md#logs-estruturados-e-track-id
- [x] Componente frontend: `RegraDeCobrancaForm` (organism) — seletor de tipo (`ChipSelector`, mesmo padrão de `ModeloAgendamentoForm`) + campo `Valor` sempre visível + campo `FrequenciaSemanalContratada` condicional (só quando tipo = ValorPorAula), com teste de componente
- [x] Integração `lib/api/regraDeCobranca.ts` (cliente HTTP do contrato acima) + tela `frontend/src/app/professor/[professorId]/matriculas/[matriculaId]/regra-de-cobranca.tsx`, com teste

## Fora de escopo nesta Task (documentado, não esquecido)

- Endpoint de consulta "quanto o Aluno deve pagar" (itens 12 e 13 do backlog) — esta Task só cobre definir/consultar a regra vigente, não calcular e expor o valor devido agregado num período real; `CalcularValorDevido` já existe no Domain pronto para as issues futuras 12/13 consumirem.
- Qualquer integração com frequência real registrada (item 14, ainda não implementado) — `quantidadeDeAulasNoPeriodo` é um parâmetro explícito de `CalcularValorDevido`, não algo que este Domain calcula sozinho a partir de `Horario`/presença.
- Snapshot histórico de regra por período passado — mudar a regra não recalcula retroativamente (edge point do card).
