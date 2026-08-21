# Implementation: Horário ganha política de marcação própria (#73)

## Entidades/classes afetadas

- **Domain** (`backend/src/Synclass.Domain/Horarios/`):
  - `TipoMarcacao` (novo enum): `Livre = 0`, `Fixo = 1`, `Hibrido = 2` — mesmos
    nomes/semântica de `ModeloAgendamento` (`Vago`→`Livre`, `Fixo`→`Fixo`,
    `Hibrido`→`Hibrido`), mas escopado ao `Horario`, não ao `Professor`.
  - `TipoMarcacaoInvalidoException` (novo, mesmo padrão de
    `DiaSemanaInvalidoException`/`ModeloAgendamentoInvalidoException`).
  - `Horario`: novo parâmetro obrigatório `tipoMarcacao` em `Criar` (sem
    default — força a escolha, RN explícita do card) e nova propriedade
    `TipoMarcacao { get; private set; }`, validada com
    `Enum.IsDefined`/`ValidarTipoMarcacao` (mesmo padrão de
    `ValidarDiaSemana`).
  - `HorarioService.CadastrarAsync`: novo parâmetro obrigatório
    `tipoMarcacao`, repassado a `Horario.Criar`.
- **Infrastructure** (`backend/src/Synclass.Infrastructure/Persistence/`):
  migration nova adicionando a coluna; `HorarioConfiguration` (EF mapping, se
  existir configuração explícita de coluna) ganha o mapeamento do novo
  enum-como-int, mesmo padrão de `DiaSemana`/`LimiteAlunos`.
- **Api** (`backend/src/Synclass.Api/Controllers/HorariosController.cs`):
  `CriarHorarioRequest` ganha `TipoMarcacao` (int, sem valor default —
  obrigatório no contrato); `HorarioResponse` ganha `TipoMarcacao` (int) para
  o cliente conseguir ler a política ao listar; `Criar` passa o valor
  castado para `HorarioService.CadastrarAsync` e captura
  `TipoMarcacaoInvalidoException` retornando 400, mesmo padrão de
  `HorarioRejeitadoException`.

## Contrato de API

`POST /professores/{professorId}/horarios` — request ganha campo obrigatório:

```json
{ "diaSemana": 2, "horaInicio": "10:00:00", "duracaoMinutos": 60, "tipoMarcacao": 0, "limiteAlunos": 4 }
```

Response (`GET`/`POST`) ganha `tipoMarcacao` (int) no corpo de `HorarioResponse`.

## Modelo de dados

Tabela `Horarios`: nova coluna `TipoMarcacao` (int, not null, sem default no
banco — cada linha nova exige o valor explícito da aplicação). Migration
`AdicionaTipoMarcacaoHorario`, seguindo o padrão de
`AdicionaLimiteAlunosHorario`.

## Edge points (não cobertos por Gherkin)

- `TipoMarcacaoInvalidoException` na Api retorna 400 (não 500) — mesmo
  tratamento dado a `DiaSemanaInvalidoException`/`DuracaoInvalidaException`
  hoje (ver `HorarioRejeitadoException`, exceção base já capturada pelo
  controller — se `TipoMarcacaoInvalidoException` já herdar dela, nenhum
  `catch` novo é necessário).
- Não migra dados de horários já existentes no banco (linhas antigas não
  existem ainda em produção — este card só define o schema para linhas
  novas; a migração de dados de fato é a Task #75, que depende desta).
- Não altera `AlocacaoHorarioService`/`MarcacoesHorarioController` para
  decidir com base em `Horario.TipoMarcacao` — a política ainda não é
  *usada* em lugar nenhum após esta Task, só existe e é validada na
  criação. Isso é a Task #74.

## Dependência de outras Tasks

Nenhuma (pré-requisito do Épico #72 — `#74`, `#75`, `#76` dependem desta).
