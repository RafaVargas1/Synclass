# Implementação: Professor edita a política de marcação de um Horário já cadastrado (#71)

## Entidades/classes afetadas

- **Domain** (`backend/src/Synclass.Domain/Horarios/`):
  - `Horario.cs` — novo método público `AlterarTipoMarcacao(TipoMarcacao novo)`, espelhando o `AlterarLimiteAlunos` já existente na mesma classe: valida com o `ValidarTipoMarcacao` privado já usado por `Criar` e atribui direto (sem checagem de posse — isso é responsabilidade do `HorarioService`, não da entidade).
  - `HorarioService.cs` — novo método `AlterarPoliticaAsync(Guid professorId, Guid horarioId, TipoMarcacao novo, CancellationToken cancellationToken)`: busca com `BuscarDoProfessorAsync` (já `internal`, mesma checagem de posse de `RemoverAsync`), chama `horario.AlterarTipoMarcacao(novo)`, e `SalvarAsync`. Não consulta `PossuiAlunosAlocadosAsync` — ver "Edge points" abaixo.
  - Nenhuma classe nova de exceção: reaproveita `TipoMarcacaoInvalidoException` (validação) e `HorarioNaoEncontradoException` (posse/existência), ambas já existentes.
- **Api** (`backend/src/Synclass.Api/Controllers/HorariosController.cs`):
  - Novo endpoint `PATCH professores/{professorId}/horarios/{horarioId}`, chamando `HorarioService.AlterarPoliticaAsync`.
  - Novo log `HorarioTipoMarcacaoAlterado`, mesmo padrão de `LogLimiteAlunosAlterado` (guarda o valor anterior antes de chamar o serviço, já que a entidade retornada só tem o valor novo).
- **Frontend**:
  - `frontend/src/lib/api/horarios.ts` — nova função `alterarTipoMarcacaoHorario`.
  - `frontend/src/components/organisms/HorarioCard.tsx` — modo de edição inline (estado local `editando`), reaproveita `ChipSelector` com as mesmas `OpcoesTipoMarcacao` de `HorarioForm.tsx` (considerar extrair para um módulo compartilhado se a duplicata incomodar o dev-review — decisão de code-style no momento da implementação, não bloqueia o card).
  - `frontend/src/app/professor/[professorId]/horarios.tsx` — novo `handleAlterarPolitica`, mesma forma de `criarHandleSubmit`/`criarHandleRemover` (função fábrica de closure, não nested function).

## Contrato de API

`PATCH /professores/{professorId}/horarios/{horarioId}`

Request:
```json
{ "tipoMarcacao": 1 }
```

Response 200 (mesmo shape de `HorarioResponse` já usado por `Criar`/`Listar`):
```json
{
  "id": "guid",
  "diaSemana": 2,
  "horaInicio": "10:00:00",
  "duracaoMinutos": 60,
  "tipoMarcacao": 1,
  "limiteAlunos": 1
}
```

Erros:
- 404 (`HorarioErrorResponse`) — horário não existe ou não pertence ao `professorId` da rota (mesma semântica de não revelar existência usada em `Remover`).
- 400 (`HorarioErrorResponse`) — `tipoMarcacao` fora do enum (0/1/2).

Sem autenticação real ainda (issue #18 em paralelo) — `professorId` continua vindo da rota, mesmo padrão de `Criar`/`Listar`/`Remover` já existentes neste controller.

## Modelo de dados

Nenhuma migration nova — `TipoMarcacao` já é uma coluna existente na tabela `Horarios` desde a issue #73/#75. Esta Task só adiciona um caminho de update para essa coluna já existente.

## Edge points

- `AlterarPoliticaAsync` **não** verifica `PossuiAlunosAlocadosAsync` nem qualquer outra condição de bloqueio — ao contrário de `RemoverAsync`. Isso é intencional (ver RN do card): a troca de política é sempre permitida e nunca retroativa, mesmo princípio já documentado quando a política era por Professor (issue #7).
- Duração, dia da semana, hora de início e `LimiteAlunos` **não** ganham endpoint de update nesta Task — continuam seguindo a RN original da issue #6 ("remover e recriar"). Não expandir escopo para um `PATCH` genérico de Horário.
- `ChipSelector`/`OpcoesTipoMarcacao` de `HorarioForm.tsx` podem ser extraídos para um módulo compartilhado (`lib/opcoesTipoMarcacao.ts` ou similar) se `HorarioCard.tsx` precisar da mesma lista — evita duplicar o array de opções entre os dois componentes. Decisão de implementação, não critério de aceite.
- Concorrência: dois `PATCH` simultâneos no mesmo horário resolvem por "último a escrever vence" (mesmo comportamento implícito já existente em `AlterarLimiteAlunos`/EF Core sem controle de concorrência otimista neste agregado) — não introduzir versionamento/`RowVersion` nesta Task, fora de escopo.

## Dependência de outras Tasks

Nenhuma — depende só do que já está em `main` (issues #6, #73, #75, #76, todas já mergeadas). Não há Task-irmã pendente para esta.
