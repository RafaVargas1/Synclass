# Task: Aluno desmarca aula com antecedência configurável (#10)

Card: https://github.com/RafaVargas1/Synclass/issues/10

## Ordem de execução

- [x] Migration: `dotnet ef migrations add CriaAulaECancelamento` (tabelas `Aulas`,
  `CancelamentosAula`, coluna `PrazoCancelamentoMinutos` em `ConfiguracoesProfessor`)
  — schema primeiro, sem lógica ainda.
- [x] Teste unidade (Domain): `AulaService.CancelarAsync` cria a `Aula` sob demanda
  quando ainda não existe registro para (HorarioId, Data).
- [x] Implementação mínima: `Aula`, `IAulaRepository`, `AulaService.CancelarAsync`
  (só o caminho "ainda não existe Aula").
- [x] Teste unidade (Domain): cancelamento permitido quando dentro do prazo
  (AC1 — 30h de antecedência, prazo 24h).
- [x] Implementação mínima: `GarantirDentroDoPrazoAsync`, cria `CancelamentoAula`.
- [x] Teste unidade (Domain): cancelamento rejeitado quando fora do prazo, com
  mensagem indicando até quando era possível cancelar (AC2 — 10h de antecedência,
  prazo 24h) → `PrazoCancelamentoExpiradoException`.
- [ ] Teste unidade (Domain): cancelamento não afeta a `AlocacaoHorario` (alocação
  recorrente permanece) — só cria `CancelamentoAula` para aquela `Aula` (AC3).
- [ ] Teste unidade (Domain): dois Alunos alocados no mesmo horário — cancelamento
  de um não cria `CancelamentoAula` para o outro (AC4, independência).
- [ ] Teste unidade (Domain): prazo aplicado é sempre o `PrazoCancelamentoMinutos`
  vigente no momento do cancelamento, não o vigente quando o Aluno foi alocado
  (AC5 — muda de 24h para 48h, cancelamentos antigos não são reavaliados).
- [ ] Teste unidade (Domain): cancelar uma aula já cancelada pelo mesmo Aluno é
  idempotente (edge point) — não lança, retorna o `CancelamentoAula` existente.
- [ ] Teste unidade (Domain): Aluno não alocado naquele horário tentando cancelar
  → rejeitado (mesmo padrão de `MatriculaNaoVinculadaAoProfessorException`/nova
  exceção equivalente).
- [ ] Teste unidade (Domain): `AulaService.ListarProximasAsync` — calcula a
  próxima ocorrência futura de cada `AlocacaoHorario` da matrícula (sem exigir
  `Aula` pré-existente), pulando ocorrências já canceladas.
- [ ] Log estruturado: evento `AulaCancelada` (Information) e
  `CancelamentoRejeitadoPorPrazo` (Warning) — ver architecture.md#logs-estruturados-e-track-id.
- [ ] Teste de fumaça (Api): `POST .../aulas/{data}/cancelamentos` — 200 dentro do
  prazo, 400 fora do prazo, 404 horário inexistente.
- [ ] Teste de fumaça (Api): `GET .../horarios/proximas-aulas?matriculaId=` — 200
  com lista de próximas aulas.
- [ ] Componente frontend: `lib/api/cancelamentos.ts` (contrato já estabilizado
  pelos smoke tests acima).
- [ ] Componente frontend: tela `minhas-aulas.tsx` (lista de próximas aulas do
  Aluno) + organism `AulaProximaCard` (botão cancelar, desabilitado com motivo
  quando `podeCancelar === false`).
