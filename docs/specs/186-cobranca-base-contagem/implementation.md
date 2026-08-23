# Implementação: base de contagem (agendamento vs. presença confirmada) para cobrança por aula (#186)

## Entidades/classes afetadas

- `backend/src/Synclass.Domain/Cobrancas/BaseDeContagemAula.cs` (novo) —
  `public enum BaseDeContagemAula { Agendamento = 0, PresencaConfirmada = 1 }`.
  `Agendamento = 0` é o default/comportamento atual, para uma migration com
  `DEFAULT 0` não mudar o valor cobrado de ninguém já configurado.
- `backend/src/Synclass.Domain/Cobrancas/RegraFixoPorAula.cs` e
  `RegraValorPorAula.cs` — cada um ganha `BaseDeContagemAula BaseDeContagemAula
  { get; private set; }`, parâmetro em `Criar(...)` com default
  `BaseDeContagemAula.Agendamento` quando o chamador não especifica. **Não**
  adicionar este campo em `RegraDeCobranca` (base) nem em `RegraFixoMensal`
  — ele só faz sentido pra regra que conta aula (RN da issue). Para o
  `ConsultaCobrancaService` acessar o campo sem `is RegraFixoPorAula or
  RegraValorPorAula` duplicado em vários lugares, crie a interface
  `IRegraComBaseDeContagemAula { BaseDeContagemAula BaseDeContagemAula { get; } }`
  no mesmo arquivo de `BaseDeContagemAula.cs`, implementada por
  `RegraFixoPorAula`/`RegraValorPorAula` (`RegraFixoMensal` não implementa).
- `backend/src/Synclass.Domain/Frequencias/IRegistroFrequenciaRepository.cs`
  — novo método
  `Task<int> ContarPresencasNoPeriodoAsync(Guid matriculaId, PeriodoConsulta periodo, CancellationToken cancellationToken)`.
  Implementação em
  `backend/src/Synclass.Infrastructure/Persistence/RegistroFrequenciaRepository.cs`
  — join `RegistroFrequencia` × `Aula` (via `AulaId`), filtra
  `StatusProfessor == StatusFrequencia.Presente`, `Aula.HorarioId` entre os
  horários alocados à matrícula (mesma lista que
  `ConsultaCobrancaService.ContarAulasNoPeriodoAsync` já resolve via
  `_alocacoes.ListarPorMatriculaAsync`), e `Aula.Data` dentro de
  `[periodo.Inicio, periodo.FimExclusivo)`. `PeriodoConsulta` é `Domain`
  puro (sem EF) — a implementação de Infrastructure recebe os limites já
  como `DateOnly`/`DateOnly` (expor `periodo.Inicio`/`periodo.FimExclusivo`,
  confirmar que já são público antes de assumir).
- `backend/src/Synclass.Domain/Cobrancas/ConsultaCobrancaService.cs` —
  `ContarAulasNoPeriodoAsync` (privado, linha ~124) passa a receber a
  `RegraDeCobranca regra` já resolvida (o chamador `CalcularParaMatriculaAsync`,
  linha ~86, já tem `regra` em mãos antes de chamar) e ganha um branch:
  ```csharp
  private async Task<int> ContarAulasNoPeriodoAsync(
      Guid matriculaId, RegraDeCobranca regra, PeriodoConsulta periodo, CancellationToken cancellationToken)
  {
      if (regra is IRegraComBaseDeContagemAula regraPorAula
          && regraPorAula.BaseDeContagemAula == BaseDeContagemAula.PresencaConfirmada)
      {
          return await _registrosFrequencia.ContarPresencasNoPeriodoAsync(matriculaId, periodo, cancellationToken);
      }

      // implementação atual (loop de alocações × ContarOcorrencias), inalterada
  }
  ```
  Construtor de `ConsultaCobrancaService` ganha `IRegistroFrequenciaRepository registrosFrequencia`
  — atualizar todo lugar que instancia a classe diretamente (testes, DI em `Program.cs`).
- `backend/src/Synclass.Domain/Cobrancas/RegraDeCobrancaService.cs` —
  `DefinirAsync`/`ConstruirRegra` ganham parâmetro
  `BaseDeContagemAula? baseDeContagemAula`. `GarantirFrequenciaCoerenteComTipo`
  ganha uma validação irmã (`GarantirBaseDeContagemCoerenteComTipo`): se
  `tipo == FixoMensal` e `baseDeContagemAula is not null`, rejeita (mesmo
  padrão de `FrequenciaSemanalContratadaNaoEsperadaException` — criar
  `BaseDeContagemAulaNaoEsperadaException` análoga). Quando `tipo` é
  `FixoPorAula`/`ValorPorAula` e `baseDeContagemAula is null`, resolve para
  `BaseDeContagemAula.Agendamento` (não lança — é opcional, tem default).
- `backend/src/Synclass.Api/Controllers/RegraDeCobrancaController.cs` —
  `DefinirRegraDeCobrancaRequest` ganha `string? BaseDeContagemAula` (nome
  do enum como string, mesmo padrão já usado para `Tipo`); parse com
  `Enum.Parse` só quando não nulo. `RegraDeCobrancaResponse` ganha
  `string? BaseDeContagemAula` — `ParaResponse` resolve via
  `regra is IRegraComBaseDeContagemAula regraPorAula ? regraPorAula.BaseDeContagemAula.ToString() : null`,
  mesmo padrão já usado pra `FrequenciaSemanalContratada` (linha ~92 do
  arquivo atual).
- Migration nova: coluna `BaseDeContagemAula int NULL` na tabela TPH de
  `RegraDeCobranca` (só populada para as subclasses que a usam — `NULL`
  para linhas de `RegraFixoMensal`, sem quebrar nada existente).
- `frontend/src/lib/api/regraDeCobranca.ts` — `RegraDeCobranca`/
  `DefinirRegraDeCobrancaInput` ganham
  `baseDeContagemAula: 'Agendamento' | 'PresencaConfirmada' | null`.
- `frontend/src/components/organisms/RegraDeCobrancaForm.tsx` — a condição
  `tipo === 'ValorPorAula'` (linha ~66) que hoje só mostra o campo de
  frequência vira uma condição maior: `tipo === 'ValorPorAula' ||
  tipo === 'FixoPorAula'` mostra também um novo `ChipSelector`
  "Como contar as aulas do período?" com opções
  `[{ valor: 'Agendamento', rotulo: 'Todas as aulas agendadas' }, { valor: 'PresencaConfirmada', rotulo: 'Só aulas com presença confirmada' }]`,
  default `'Agendamento'` quando `regraExistente` não tem o campo definido
  (mesmo padrão de pré-preenchimento já usado pros outros campos, linha
  ~44-48).

## Padrão de estilo a seguir

Siga `FrequenciaSemanalContratada` ponta a ponta (domínio → service →
controller → form) — é o precedente mais próximo de "campo condicional por
tipo de regra" já resolvido no repositório. Não invente uma segunda forma
de fazer validação condicional por `TipoRegraDeCobranca`.

## Contrato de API

`PUT /professores/{professorId}/matriculas/{matriculaId}/regra-de-cobranca`
- Request: `DefinirRegraDeCobrancaRequest(string Tipo, decimal Valor, int? FrequenciaSemanalContratada, string? BaseDeContagemAula)`
- Response 200: `RegraDeCobrancaResponse(Guid MatriculaId, string Tipo, decimal Valor, int? FrequenciaSemanalContratada, string? BaseDeContagemAula)`

`GET` no mesmo endpoint devolve `BaseDeContagemAula` no mesmo formato.

## Modelo de dados

Coluna nova `BaseDeContagemAula int NULL` na tabela TPH única de
`RegraDeCobranca` (confirmar nome da tabela lendo a migration mais recente
antes de escrever a nova). Sem tabela nova.

## Edge points

- Uma aula do período sem `RegistroFrequencia` lançado ainda não conta
  nem como presença nem como ausência — fica fora da contagem quando a
  base é `PresencaConfirmada` (RN explícita do card, não é bug).
- `RegraFixoMensal` ignora `BaseDeContagemAula` completamente — nunca
  chama `ContarAulasNoPeriodoAsync` no branch novo (a fórmula nem usa
  `quantidadeDeAulasNoPeriodo`), então nenhuma mudança de comportamento
  ali.
- N+1 aceitável na implementação de `ContarPresencasNoPeriodoAsync`
  (mesmo padrão já documentado em `ContarAulasNoPeriodoAsync` — "aceitável
  na escala atual").
