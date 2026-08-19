# Task: Aluno acompanha seu histórico de frequência (#16)

Card: https://github.com/RafaVargas1/Synclass/issues/16

## Ordem de execução

- [ ] Teste unidade (Domain): `PeriodoConsulta.GerarDatas(diaSemana)` —
  método novo (irmão de `ContarOcorrencias`), gera cada `DateOnly` do
  período `[Inicio, FimExclusivo)` cujo dia da semana bate.
- [ ] Implementação mínima: `PeriodoConsulta.GerarDatas`.
- [ ] Teste unidade (Domain): `FrequenciaService.ListarHistoricoAsync` —
  Aluno com 1 aula agendada e nenhum registro ainda → status
  `NaoRegistrada` (AC5, nunca assume `Ausente` por padrão).
- [ ] Teste unidade (Domain): Professor registrou frequência (issue #14)
  para uma aula → histórico reflete `StatusProfessor` (AC2).
- [ ] Teste unidade (Domain): Aluno confirmou presença (issue #15) e
  Professor também marcou presente → histórico mostra `Presente` (AC3,
  mesmo fato por duas fontes).
- [ ] Teste unidade (Domain): divergência (Aluno confirmou presente,
  Professor marcou ausente) → histórico mostra `Ausente`, sem indicar
  conflito (AC4, `StatusProfessor` prevalece).
- [ ] Teste unidade (Domain): aula cancelada pelo próprio Aluno (issue #10)
  → status `Cancelada`, não aparece como `NaoRegistrada` nem `Ausente`
  (edge point do card).
- [ ] Teste unidade (Domain): Aluno com matrículas em mais de um Professor
  → histórico agrupado por Professor, sem misturar/somar entre eles (mesma
  RN de `ConsultaCobrancaService`, issue #13).
- [ ] Implementação mínima: `FrequenciaService.ListarHistoricoAsync`
  (percorre `Matricula` do Aluno → `AlocacaoHorario` de cada uma → datas do
  período via `GerarDatas` → status por data).
- [ ] Log estruturado: evento `HistoricoFrequenciaConsultado`
  (Information, `{TrackId} {UsuarioId} {PeriodoInicio} {PeriodoFim}`) — ver
  architecture.md#logs-estruturados-e-track-id.
- [ ] Teste de fumaça (Api): `GET alunos/historico-frequencia` — 200 com
  período default (mês corrente) e com `inicio`/`fim` explícitos, 400
  período incompleto/invertido (mesmo padrão de `ValorDevidoAlunoController`).
- [ ] Componente frontend: `lib/api/historicoFrequencia.ts`
  (`listarHistoricoFrequenciaDoAluno`, contrato já estabilizado pelo smoke
  test acima).
- [ ] Refactor pequeno: extrair `SeletorDePeriodo` (hoje função privada em
  `aluno/valor-devido.tsx`) para `components/molecules/SeletorDePeriodo.tsx`
  — evita duplicar o mesmo par de `Input`+`Button` nesta tela (code-style.md,
  sem duplicação de código). Atualizar `valor-devido.tsx` para importar do
  novo local.
- [ ] Componente frontend: tela `aluno/historico-frequencia.tsx`,
  reaproveitando o `SeletorDePeriodo` extraído e um organism novo
  `HistoricoFrequenciaCard.tsx` (aula + status, indicador visual por
  status).
