# Implementação técnica — Regra de cobrança (#11)

## Entidades/classes afetadas

**Domain** (`Synclass.Domain.Cobrancas`, namespace novo — nenhum precedente de
Strategy existe hoje no projeto; esta Task estabelece o padrão de
nomenclatura `I<Nome>` na raiz do contexto, seguido pelos repositórios
existentes):

- `IRegraDeCobranca` — contrato: `Guid Id { get; }`, `Guid MatriculaId { get; }`,
  `decimal CalcularValorDevido(int quantidadeDeAulasNoPeriodo)`.
- `RegraDeCobranca` (classe abstrata) — estado comum mapeável por EF Core
  (`Id`, `MatriculaId`, `Valor`, `CreatedAt`, `UpdatedAt`), implementa
  `IRegraDeCobranca`, deixa `CalcularValorDevido` abstrato.
- `RegraValorPorAula : RegraDeCobranca` — adiciona `FrequenciaSemanalContratada`
  (int, 1-7, validado no construtor/factory). `CalcularValorDevido` =
  `Valor * quantidadeDeAulasNoPeriodo` (o `Valor` já é o preço resolvido para
  a faixa de frequência escolhida pelo Professor — não há tabela de preço por
  frequência neste escopo, o Professor informa o valor da faixa diretamente).
- `RegraFixoMensal : RegraDeCobranca` — sem parâmetro extra.
  `CalcularValorDevido` ignora `quantidadeDeAulasNoPeriodo` e retorna sempre
  `Valor` (RN: não depende de frequência real).
- `RegraFixoPorAula : RegraDeCobranca` — sem parâmetro extra.
  `CalcularValorDevido` = `Valor * quantidadeDeAulasNoPeriodo` (mesma fórmula
  de `RegraValorPorAula`, mas sem o parâmetro de frequência contratada — a
  diferença entre as duas regras é semântica/de configuração, não de cálculo).
- `FrequenciaSemanalContratadaInvalidaException`, `MatriculaNaoEncontradaException`
  (se ainda não existir um equivalente reutilizável — conferir
  `Synclass.Domain.Matriculas` antes de criar um novo).
- `IRegraDeCobrancaRepository` — `BuscarPorMatriculaAsync(Guid matriculaId, CancellationToken)`,
  `SalvarAsync(RegraDeCobranca regra, CancellationToken)` (upsert: substitui
  a regra existente da mesma `MatriculaId`, nunca duas linhas).
- `RegraDeCobrancaService` — orquestra `DefinirAsync(Guid matriculaId, TipoRegraDeCobranca tipo, decimal valor, int? frequenciaSemanalContratada, CancellationToken)`:
  valida que a `Matricula` existe (via `IMatriculaRepository`, já existente),
  busca regra anterior (para o log com `ValorAnterior`), constrói a
  implementação concreta certa a partir de `tipo`, persiste via
  `IRegraDeCobrancaRepository.SalvarAsync`.
- `TipoRegraDeCobranca` (enum: `ValorPorAula = 0, FixoMensal = 1, FixoPorAula = 2`)
  — só usado como parâmetro de entrada do Service/Controller para escolher a
  classe concreta; não é a mesma coisa que o discriminador de string do EF
  Core (ver Modelo de dados).

**Infrastructure** (`Synclass.Infrastructure.Persistence`):

- `Configurations/RegraDeCobrancaConfiguration.cs` — TPH via
  `HasDiscriminator<string>("Tipo")`, `HasValue<RegraValorPorAula>("ValorPorAula")`,
  `HasValue<RegraFixoMensal>("FixoMensal")`, `HasValue<RegraFixoPorAula>("FixoPorAula")`.
  Nenhum precedente de TPH no projeto — esta Task é o primeiro caso; seguir o
  estilo de comentário XML explicativo já usado em `MatriculaConfiguration`/
  `ConfiguracaoProfessorConfiguration` documentando a decisão. `Id` com
  `ValueGeneratedNever()` (convenção do projeto — todo Guid é gerado em
  código). Índice único em `MatriculaId` (`builder.HasIndex(r => r.MatriculaId).IsUnique()`).
  FK para `Matriculas` com `OnDelete(DeleteBehavior.Cascade)` (regra de
  cobrança não faz sentido órfã se a matrícula for removida — não há hoje
  exclusão de matrícula implementada, mas mantém consistência com o padrão
  de `ConfiguracaoProfessorConfiguration`).
- `RegraDeCobrancaRepository : IRegraDeCobrancaRepository` — usa
  `SynclassDbContext`, segue o padrão de `MatriculaRepository`.
- `SynclassDbContext.cs` — adiciona `DbSet<RegraDeCobranca> RegrasDeCobranca`.

**Api** (`Synclass.Api.Controllers`):

- `RegraDeCobrancaController` — rota
  `professores/{professorId:guid}/matriculas/{matriculaId:guid}/regra-de-cobranca`,
  mesmo padrão de aninhamento de `ConfiguracoesController`/`HorariosController`.
  Antes de qualquer operação, resolve a `Matricula` por `matriculaId` e
  confere `ProfessorId == professorId` da rota — 404 se não bater ou não
  existir (nenhuma `MatriculasController` existe ainda; esta é a primeira
  rota aninhada sob `matriculas/{matriculaId}`, mas segue a mesma convenção
  de nested resource já usada para `horarios`/`configuracao`).

**Frontend** (Expo Router + Atomic Design, mesma estrutura de
`frontend/src/app/professor/[professorId]/...` e
`frontend/src/components/organisms`):

- `components/organisms/RegraDeCobrancaForm.tsx` — `ChipSelector` (mesmo
  molecule de `ModeloAgendamentoForm.tsx`) para `tipo`; campo `valor`
  sempre visível; campo `frequenciaSemanalContratada` renderizado só quando
  `tipo === 'ValorPorAula'` — primeiro caso de campo condicional no
  frontend, não há padrão prévio para copiar.
- `lib/api/regraDeCobranca.ts` — cliente HTTP (`PUT`/`GET`), mesmo padrão de
  `lib/api/alunosProvisorios.ts`/`lib/api/horarios.ts`.
- `app/professor/[professorId]/matriculas/[matriculaId]/regra-de-cobranca.tsx`
  — tela, usa `useLocalSearchParams` para `professorId`/`matriculaId`, mesmo
  padrão de `alunos/cadastro.tsx`.

## Contrato de API

```
PUT /professores/{professorId}/matriculas/{matriculaId}/regra-de-cobranca
Body: {
  "tipo": "ValorPorAula" | "FixoMensal" | "FixoPorAula",
  "valor": number,                          // decimal, > 0
  "frequenciaSemanalContratada": number|null // obrigatório e 1-7 só quando tipo = ValorPorAula; null/omitido nos outros dois
}
-> 200 OK: RegraDeCobrancaResponse
-> 400 Bad Request: { "mensagem": string }   // valor inválido, frequência fora de 1-7 ou ausente/presente no tipo errado
-> 404 Not Found                              // matriculaId não existe ou não pertence a professorId

GET /professores/{professorId}/matriculas/{matriculaId}/regra-de-cobranca
-> 200 OK: RegraDeCobrancaResponse            // regra vigente
-> 404 Not Found                              // matrícula sem regra configurada (critério de aceite 1) OU matrícula/professor não batem

RegraDeCobrancaResponse: {
  "matriculaId": string (guid),
  "tipo": "ValorPorAula" | "FixoMensal" | "FixoPorAula",
  "valor": number,
  "frequenciaSemanalContratada": number|null
}
```

## Modelo de dados

Migration `CriaRegraDeCobranca` (TPH — uma tabela para as 3 implementações):

```
RegrasDeCobranca
  Id                            uuid PK (gerado em código)
  MatriculaId                   uuid FK -> Matriculas, UNIQUE, NOT NULL, ON DELETE CASCADE
  Tipo                          text NOT NULL   -- discriminador EF Core: "ValorPorAula" | "FixoMensal" | "FixoPorAula"
  Valor                         numeric NOT NULL
  FrequenciaSemanalContratada   int NULL        -- só preenchido quando Tipo = "ValorPorAula"
  CreatedAt                     timestamptz NOT NULL
  UpdatedAt                     timestamptz NOT NULL
```

`Tipo` (coluna discriminadora do EF Core) usa os mesmos literais de string do
enum `TipoRegraDeCobranca` do lado da Api/Domain para consistência de
leitura no banco, mas são conceitos tecnicamente distintos (discriminador
TPH vs. enum de transporte) — não compartilhar o mesmo tipo C# entre os
dois para não acoplar o schema do EF Core ao contrato HTTP.

## Edge points (não viram critério de aceite Gherkin)

- Trocar a regra de uma matrícula é upsert (delete lógico da linha anterior
  + insert da nova, dentro da mesma transação do `SalvarAsync`) — não há
  histórico versionado nesta Task (RN do card: sem snapshot retroativo).
- `FrequenciaSemanalContratada` fora de 1-7, ou informado quando `tipo` não é
  `ValorPorAula`, ou ausente quando `tipo` é `ValorPorAula` — todos 400 na
  Api, validado no Domain (`RegraValorPorAula` só aceita construção com
  frequência 1-7; o Service rejeita frequência informada para os outros
  dois tipos).
- `quantidadeDeAulasNoPeriodo` é um parâmetro explícito de
  `CalcularValorDevido`, não algo resolvido internamente — nenhuma
  integração com `Horario`/frequência real (item 14) nesta Task; fica para
  as issues 12/13 (consulta de valor devido) decidirem a fonte desse número.
- Sem endpoint de exclusão de regra nesta Task — trocar de regra é sempre
  via `PUT` com um novo `tipo`/`valor`; remover (voltar a "sem regra
  configurada") não é um caso de uso descrito no card.

## Dependência de outras Tasks

Nenhuma — issue independente de #8/#9 (alocação de horário) e de #4/#27
(sessão autenticada). `CalcularValorDevido` fica pronto para as issues
futuras 12 ("Professor vê quanto cada Aluno deve pagar") e 13 ("Aluno vê
quanto tem que pagar") consumirem sem precisar mudar o contrato desta Task.
