# Desenho técnico — Limite de alunos por horário (#17)

## Entidades/classes afetadas

**`Synclass.Domain.Horarios`** (estende o módulo da issue #6, sem novo bounded
context):

- `Horario` — ganha a propriedade `LimiteAlunos` (`int`). `Criar(...)` ganha
  um parâmetro opcional `int? limiteAlunos = null` (default `1` quando
  omitido, validado via `LimiteAlunosHorario.Validar` quando informado).
  Ganha também `AlterarLimiteAlunos(int novoLimite, int quantidadeAlunosAlocados)`
  — método de domínio puro (sem dependência de repositório), pronto para a
  issue #8 (ver "Dependência da issue #8" abaixo), mas não chamado por
  nenhum endpoint nesta issue.
- `LimiteAlunosHorario` (classe estática) — mesmo padrão de `DuracaoAula`:
  `Padrao = 1`, `MinimoAlunos = 1`, `Validar(int limiteAlunos)`.
- `LimiteAlunosInvalidoException` — subtipo de `HorarioRejeitadoException`,
  mesma família de `DuracaoInvalidaException`/`DiaSemanaInvalidoException`.
- `LimiteAlunosMenorQueAlocadosException` — subtipo de
  `HorarioRejeitadoException`, usada por `Horario.AlterarLimiteAlunos` quando
  o novo limite é menor que a quantidade de Alunos já alocados.

**`Synclass.Infrastructure`**: `HorarioConfiguration` ganha o mapeamento de
`LimiteAlunos` (coluna `LimiteAlunos`, not null, default `1`); migration
`AdicionaLimiteAlunosHorario` (`ALTER TABLE` na tabela `Horarios` existente,
issue #6 — não cria tabela nova).

**`Synclass.Api`**: `HorariosController.Criar` — `CriarHorarioRequest` ganha
`LimiteAlunos` (`int?`, opcional); `HorarioResponse` ganha `LimiteAlunos`
(`int`); log novo `LimiteAlunosAlterado` emitido junto do `HorarioCriado` já
existente.

**Frontend**: `HorarioForm` ganha campo numérico "Limite de alunos" (default
`'1'`); `HorarioCard` passa a exibir o limite cadastrado; `lib/api/horarios.ts`
propaga `limiteAlunos` em `CriarHorarioInput`/`Horario`.

## Decisão documentada: sem endpoint de edição de horário

A issue #17 assume, na leitura literal do card, um fluxo de "editar horário"
— mas hoje (issue #6) só existe `POST` (criar) e `DELETE` (remover); não há
`PUT`/`PATCH` de horário. Criar esse endpoint geral de edição está fora do
escopo desta issue: a Regra de Negócio de #17 só exige que o limite seja
definido "no momento em que cadastra o horário" e que *reduções futuras*
(quando a issue #8 existir) sejam validadas — não pede um fluxo de edição de
horário em si.

Decisão: `LimiteAlunos` é exposto como parâmetro opcional do
`POST /professores/{professorId}/horarios` já existente (default `1` quando
omitido), sem criar update/edição geral de horário agora. Isso também
resolve o critério técnico "teste de fumaça na Api para o endpoint de
atualização de limite" do card original — reinterpretado aqui como o teste
de fumaça do `POST` existente cobrindo o parâmetro `limiteAlunos` (default,
valor informado, e rejeição de valor inválido), já que não há um segundo
endpoint de atualização nesta issue.

Consequência direta: o cenário de aceite "horário com 3 Alunos alocados,
Professor tenta reduzir o limite para menos de 3 → rejeitado" não é
exercitável via Api/UI nesta issue (não existe ação de "editar limite" na
interface hoje). Ele é coberto apenas no nível de Domain, via
`Horario.AlterarLimiteAlunos` testado diretamente — ver "Dependência da
issue #8". `qa-review` deve tratar esse critério Gherkin como não aplicável
(N/A) nesta Task, não como falha.

## Contrato de API

`POST /professores/{professorId}/horarios` (mesma rota da issue #6, campo
novo)

Request:
```json
{ "diaSemana": 0-6, "horaInicio": "HH:mm:ss", "duracaoMinutos": number, "limiteAlunos": number | null }
```
- `limiteAlunos` omitido ou `null` → aplica default `1`.

Response 200 (campo novo em `HorarioResponse`):
```json
{ "id": "guid", "diaSemana": 0-6, "horaInicio": "HH:mm:ss", "duracaoMinutos": number, "limiteAlunos": number }
```

Response 400 (`{ "mensagem": string }`) — duração inválida, conflito, ou
`limiteAlunos` menor que 1 (mesma família de `HorarioRejeitadoException` já
tratada genericamente pelo controller, sem `catch` novo).

`GET`/`DELETE` de horário (issue #6) não mudam de contrato — a resposta do
`GET` também passa a incluir `limiteAlunos` (mesmo `HorarioResponse`).

## Modelo de dados

Tabela `Horarios` (issue #6) ganha uma coluna:

| Coluna | Tipo | Observação |
|---|---|---|
| `LimiteAlunos` | `integer` | not null, default `1` |

Migration `AdicionaLimiteAlunosHorario`: `ALTER TABLE "Horarios" ADD
"LimiteAlunos" integer NOT NULL DEFAULT 1;` — linhas existentes (se houver)
recebem `1` automaticamente pelo default, mantendo o comportamento
"individual" que já era implícito antes deste card.

## Edge points (não cobertos por Gherkin)

- A contagem de Alunos alocados usada para validar redução é sobre
  `AlocacoesHorario` (vínculo recorrente, issue #8), não sobre `Aulas`
  (ocorrências datadas, issue #10) — um Aluno que só cancelou uma data
  específica continua contando para o limite do horário recorrente. Não há
  como implementar essa contagem de verdade nesta issue (a tabela não
  existe) — ver "Dependência da issue #8".
- `LimiteAlunos` não é validado contra `DuracaoMinutos` nem qualquer outro
  campo do horário — são atributos independentes.
- Não há teto máximo imposto pelo sistema (mesma decisão já tomada para
  `DuracaoAula` na issue #6) — só o mínimo de 1 é validado.

## Dependência da issue #8

Assim como `IHorarioRepository.PossuiAlunosAlocadosAsync` (issue #6) já é um
placeholder que sempre devolve `false` até a issue #8 modelar `AlocacoesHorario`,
aqui a validação de redução do limite é modelada só no nível de Domain:
`Horario.AlterarLimiteAlunos(int novoLimite, int quantidadeAlunosAlocados)`
recebe a contagem como parâmetro simples (não consulta repositório), e é
testado diretamente com valores arbitrários de "quantidade alocada" — sem
depender de uma tabela real.

Esse método **não é chamado por nenhum `HorarioService`/controller nesta
issue**: como não há endpoint de edição de horário (ver decisão acima) nem
tabela `AlocacoesHorario` para consultar a contagem real, não existe ainda
um "chamador" de produção para ele. Ele existe pronto para a issue #8 (ou
para uma futura issue de edição de horário) orquestrar: buscar o horário,
contar as alocações reais, chamar `AlterarLimiteAlunos`, persistir, e então
logar `LimiteAlunosAlterado`/`LimiteRejeitadoPorAlunosAlocados` a partir do
controller que for criado — o mesmo padrão de log já usado por
`HorariosController` (issue #6).

Consequência direta: o log `LimiteRejeitadoPorAlunosAlocados` (Warning, dos
Critérios técnicos do card) **não é emitido nesta issue** — não há caminho
de produção que o dispare (nenhum controller chama `AlterarLimiteAlunos`
ainda). Só `LimiteAlunosAlterado` (Information) é emitido de fato, no
momento da criação do horário (ver próxima seção). Fica documentado aqui
para não ser um item silenciosamente esquecido — nasce junto da issue #8.

## Decisão documentada: `LimiteAlunosAlterado` também cobre a definição inicial

O evento de log `LimiteAlunosAlterado` (Critérios técnicos do card) foi
desenhado pensando em um fluxo de edição que não existe nesta issue (ver
decisão acima). Decisão: o mesmo evento é emitido também na criação do
horário — tratando "definir o limite pela primeira vez" como o caso
degenerado de "alterar de nenhum valor para um valor" —, com
`LimiteAnterior = null` e `LimiteNovo = <valor definido, já com o default
aplicado se omitido>`. Isso mantém o evento no schema pedido pelo card sem
precisar de um endpoint de edição, e continua válido sem mudança quando a
issue #8 (ou uma futura edição de horário) passar a chamar
`AlterarLimiteAlunos` de verdade — nesse caso, `LimiteAnterior` deixa de ser
`null`.

## Dependência de outras Tasks

Nenhuma. Estende a tabela `Horarios` (issue #6, já mergeada). Não depende da
issue #7 (modelo de agendamento) nem toca `ConfiguracaoProfessor`. As
issues #8 e #9 (fora do escopo desta Task) é que vão consumir `LimiteAlunos`
para validar vaga — aqui só o dado é criado e validado.
