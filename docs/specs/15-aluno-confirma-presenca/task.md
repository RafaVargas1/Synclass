# Task: Aluno confirma sua presença em uma aula (#15)

Card: https://github.com/RafaVargas1/Synclass/issues/15

## Ordem de execução

- [x] Teste unidade (Domain): `RegistroFrequencia.ConfirmarAluno` — método
  novo, seta `ConfirmadoPeloAluno = true` e `UpdatedAt`, sem mexer em
  `StatusProfessor` (mesma simetria de `RegistrarProfessor`, issue #14).
- [x] Teste unidade (Domain): `FrequenciaService.ConfirmarPresencaAsync`
  cria a `Aula` sob demanda (reaproveita `ObterOuCriarAulaAsync`) e cria
  `RegistroFrequencia` com `ConfirmadoPeloAluno = true` quando nenhuma linha
  existia ainda (AC1).
- [x] Teste unidade (Domain): Aluno confirma de novo para a mesma aula —
  upsert na mesma linha, não duplica (AC2, idempotência).
- [x] Teste unidade (Domain): Aluno confirma presença antes do Professor
  registrar — `StatusProfessor` permanece `null` na linha (leitura correta
  de "não registrada" vs. "ausente", ver RN do card).
- [x] Teste unidade (Domain): Professor registra frequência (issue #14)
  depois da confirmação do Aluno — reconciliação já coberta pelos testes de
  `FrequenciaServiceRegistrarAsyncTests` (AC3); adicionar só o teste que
  falta, se houver, cobrindo a ordem inversa desta issue.
- [x] Teste unidade (Domain): Aluno tenta confirmar presença numa aula que
  ele mesmo cancelou (issue #10) → rejeitado (edge point do card).
- [x] Teste unidade (Domain): Aluno não vinculado ao Professor/horário →
  rejeitado (mesma exceção de `AulaService.CancelarAsync`).
- [x] Log estruturado: evento `PresencaConfirmadaPeloAluno` (Information,
  `{TrackId} {MatriculaId} {AulaId}`) — ver
  architecture.md#logs-estruturados-e-track-id.
- [x] Teste de fumaça (Api): `POST
  professores/{professorId}/horarios/{horarioId}/aulas/{data}/confirmacao-presenca`
  — 200 no caminho feliz, 400 se cancelada pelo próprio Aluno, 404 se
  horário não existe/Aluno não vinculado.
- [x] Componente frontend: `lib/api/frequencias.ts` ganha
  `confirmarPresenca` (contrato já estabilizado pelo smoke test acima).
- [x] Componente frontend: botão "Confirmar presença" na tela de próximas
  aulas do Aluno (`minhas-aulas.tsx`, issue #10), com estado visual
  diferenciando "confirmado" de "sem ação".
