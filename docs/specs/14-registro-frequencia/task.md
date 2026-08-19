# Task: Professor registra frequência da aula (#14)

Card: https://github.com/RafaVargas1/Synclass/issues/14

## Ordem de execução

- [ ] Migration: `dotnet ef migrations add CriaRegistroFrequencia` (tabela
  `RegistrosFrequencia`, índice único em `(AulaId, MatriculaId)`) — schema
  primeiro, sem lógica ainda.
- [ ] Refactor pequeno: promover `AulaService.ObterOuCriarAulaAsync` de
  `private` para `internal`, reaproveitado por `FrequenciaService` (mesma
  instanciação sob demanda da issue #10, não duplicar).
- [ ] Teste unidade (Domain): `FrequenciaService.RegistrarAsync` cria
  `RegistroFrequencia` para cada Aluno alocado no horário quando nenhum
  registro existia ainda (AC1).
- [ ] Implementação mínima: `RegistroFrequencia`, `IRegistroFrequenciaRepository`,
  `FrequenciaService.RegistrarAsync` (caminho "ainda não existe registro").
- [ ] Teste unidade (Domain): registrar para uma `Aula` que ainda não existe
  (mesma data nunca referenciada antes) cria a `Aula` sob demanda antes do
  registro.
- [ ] Teste unidade (Domain): Aluno já confirmou presença antes (linha
  pré-existente com `ConfirmadoPeloAluno = true`, `StatusProfessor = null`)
  — Professor marca presente → mesma linha é atualizada (`StatusProfessor =
  Presente`), sem criar uma segunda (AC2).
- [ ] Teste unidade (Domain): divergência — Aluno confirmou presente,
  Professor marca ausente → `StatusProfessor = Ausente` e
  `ConfirmadoPeloAluno` permanece `true` na mesma linha (preserva os dois
  valores, AC3).
- [ ] Teste unidade (Domain): registrar de novo para a mesma `(Aula,
  Matricula)` sobrescreve `StatusProfessor` anterior (upsert, AC4).
- [ ] Teste unidade (Domain): `matriculaId` do request que não está alocada
  neste horário → rejeitado (`AlocacaoNaoEncontradaException`, mesma exceção
  já usada pela issue #10 para o mesmo tipo de checagem).
- [ ] Log estruturado: evento `FrequenciaRegistrada` (Information, contagem
  de presentes/ausentes) e `FrequenciaDivergente` (Warning, por linha
  divergente) — ver architecture.md#logs-estruturados-e-track-id.
- [ ] Teste de fumaça (Api): `POST .../aulas/{data}/frequencias` — 200 com
  registro em lote, 400 para `matriculaId` não alocada, 404 horário
  inexistente/não pertence ao Professor.
- [ ] Componente frontend: `lib/api/frequencias.ts` (contrato já
  estabilizado pelos smoke tests acima).
- [ ] Componente frontend: tela de chamada do Professor
  (`professor/[professorId]/horarios/[horarioId]/chamada.tsx`) — lista de
  Alunos alocados naquele horário/data (menos os cancelados, issue #10) com
  toggle presente/ausente por Aluno e botão salvar (chamada em lote).
