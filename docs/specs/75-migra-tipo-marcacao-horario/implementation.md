# Implementation: Migrar horários existentes para a política individual (#75)

## Entidades/classes afetadas

- **Infrastructure** (`backend/src/Synclass.Infrastructure/Persistence/Migrations/`):
  nova migration `MigraTipoMarcacaoHorarioExistente` (gerada vazia via
  `dotnet ef migrations add`, corpo do `Up()` escrito à mão com
  `migrationBuilder.Sql(...)`) — não mexe em `Domain`/`Api`, é só dado.
  Roda depois de `20260820022555_AdicionaTipoMarcacaoHorario` (Task #73), já
  em `main`.

## Derivação (RN do card)

Para cada linha de `Horarios` (`h`), olhando `ConfiguracoesProfessor` (`cp`,
por `cp."ProfessorId" = h."ProfessorId"`) e `AlocacoesHorario` (`a`, por
`a."HorarioId" = h."Id"`):

| `cp.ModeloAgendamento` | Tem alocação `OrigemAlocacao = 0` (Professor) neste horário? | `TipoMarcacao` resultante |
|---|---|---|
| `Vago` (0) | — | `Livre` (0) |
| `Fixo` (1) | — | `Fixo` (1) |
| `Hibrido` (2) | Sim | `Fixo` (1) — preserva o bloqueio de marcação livre que esse horário específico já tinha |
| `Hibrido` (2) | Não | `Livre` (0) |
| Sem `ConfiguracaoProfessor` (nenhuma linha em `cp`) | — | `Livre` (0) — ver "Edge points" abaixo |

SQL (idempotente — só recalcula, sem depender de estado anterior da própria
coluna). **Ajuste feito na implementação**: a versão original proposta aqui
usava `UPDATE "Horarios" h SET ... FROM "ConfiguracoesProfessor" cp WHERE
...` (sintaxe Postgres com alias no alvo do UPDATE) — o SQLite usado no teste
de integração (ver `MigraTipoMarcacaoHorarioExistenteTests`) não aceita
alias diretamente após o nome da tabela em `UPDATE`, então a migration usa
subquery correlacionada em vez de `UPDATE ... FROM`, portável entre Postgres
(produção) e SQLite (teste):

```sql
UPDATE "Horarios"
SET "TipoMarcacao" = (
    SELECT CASE
        WHEN cp."ModeloAgendamento" = 0 THEN 0
        WHEN cp."ModeloAgendamento" = 1 THEN 1
        WHEN cp."ModeloAgendamento" = 2 THEN
            CASE WHEN EXISTS (
                SELECT 1 FROM "AlocacoesHorario" a
                WHERE a."HorarioId" = "Horarios"."Id" AND a."OrigemAlocacao" = 0
            ) THEN 1 ELSE 0 END
        ELSE 0
    END
    FROM "ConfiguracoesProfessor" cp
    WHERE cp."ProfessorId" = "Horarios"."ProfessorId"
)
WHERE EXISTS (
    SELECT 1 FROM "ConfiguracoesProfessor" cp WHERE cp."ProfessorId" = "Horarios"."ProfessorId"
);

-- Horários sem ConfiguracaoProfessor correspondente (cenário defensivo,
-- inalcançável no fluxo normal — ver Edge points): default Livre.
UPDATE "Horarios"
SET "TipoMarcacao" = 0
WHERE NOT EXISTS (
    SELECT 1 FROM "ConfiguracoesProfessor" cp WHERE cp."ProfessorId" = "Horarios"."ProfessorId"
);
```

`Down()` não reverte dado (só faz sentido reverter schema — a Task #73 já
cobre isso); documentar com um comentário que o `Down()` é no-op
intencional para esta migration.

## Teste de integração

`EnsureCreatedAsync()` (usado pelos testes Sqlite existentes, ex.
`UsuarioRepositoryConcurrencyTests`) cria o schema a partir do **modelo
atual**, pulando o histórico de migrations — não serve para testar o `Up()`
desta migration especificamente. Use `Database.MigrateAsync()` contra uma
conexão Sqlite (`DataSource=:memory:`, mesma técnica de manter a conexão
aberta) para aplicar as migrations reais em ordem, popular os cenários da
tabela acima diretamente via SQL/EF antes desta migration rodar... **não é
possível popular dados "antes" de uma migration específica com
`MigrateAsync()` de uma vez só** — em vez disso:
1. Migre só até a migration anterior a esta (`MigrateAsync(nomeDaMigrationAnterior)`).
2. Insira as linhas de cenário (`ConfiguracoesProfessor`, `Horarios` sem
   `TipoMarcacao` ainda não se aplica — a coluna já existe desde a Task #73,
   então insira com um valor qualquer nela, será sobrescrito — e
   `AlocacoesHorario`) via SQL cru (`Database.ExecuteSqlRawAsync`) já que o
   `DbContext`/EF model atual não tem mais como gerar uma entidade `Horario`
   sem `TipoMarcacao` definido para popular o estado "pré-migration".
3. Rode `MigrateAsync()` sem argumento (avança até a última, incluindo
   esta).
4. Leia `Horarios.TipoMarcacao` via SQL cru e compare com o esperado da
   tabela de derivação.

## Edge points (não cobertos por Gherkin)

- **Horário sem `ConfiguracaoProfessor`**: no fluxo normal isso é
  inalcançável — `HorarioService.CadastrarAsync` já exige
  `GarantirConfiguracaoDefinidaAsync` (issue #7) antes de qualquer
  `Horario.Criar`, então nenhuma linha real em `Horarios` deveria existir
  sem uma `ConfiguracoesProfessor` correspondente. O card pede um default
  único e documentado mesmo assim (script defensivo, não confia
  cegamente na invariante da aplicação para uma migration de dados
  irreversível) — escolhido `Livre` (mais permissivo, e consistente com o
  default histórico já usado em `GarantirModeloPermiteAlocacaoAsync` antes
  da Task #74 remover essa checagem).
- Esta Task **não remove** `ConfiguracaoProfessor.ModeloAgendamento` nem a
  tabela/coluna correspondente — só para de ser a fonte de verdade
  (decisão explícita do card, remoção é Task separada fora deste escopo).
- Não precisa lidar com `TipoMarcacao` já preenchido corretamente (idempotência
  natural: reaplicar o mesmo `UPDATE` produz o mesmo resultado, já que a
  derivação não depende do valor anterior de `TipoMarcacao`).

## Dependência de outras Tasks

Depende de #73 (coluna `TipoMarcacao` existe) e #74 (nada consome mais mais
`ConfiguracaoProfessor.ModeloAgendamento` para decidir alocação/marcação —
ambas já mergeadas em `main`).
