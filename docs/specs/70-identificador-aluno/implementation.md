# Implementação: Todo Aluno recebe um identificador único e human-readable (#70)

## Entidades/classes afetadas

| Camada | Classe/arquivo | Ação |
|---|---|---|
| Domain | `Synclass.Domain/Usuarios/Usuario.cs` | Nova propriedade `IdentificadorAluno` (string?, sem setter público). `Cadastrar` e `AdicionarPapel` passam a receber `string? identificadorAluno` e gravam quando `papel == Aluno`. |
| Domain | `Synclass.Domain/Matriculas/Matricula.cs` | Nova propriedade `IdentificadorAluno` (string?, sem setter público, independente de `IdentificadorProvisorio`). `CriarProvisoria` passa a receber `identificadorAluno`. |
| Domain (novo) | `Synclass.Domain/Alunos/IGeradorDeIdentificadorAluno.cs` | Interface fina sobre a fonte de aleatoriedade real, mesmo racional de `IGeradorDeTokenConvite`/`IGeradorDeCodigoConvite`. |
| Domain (novo) | `Synclass.Domain/Alunos/IIdentificadorAlunoUnicidadeChecker.cs` | Interface que checa colisão contra `Usuario.IdentificadorAluno` **e** `Matricula.IdentificadorAluno` (unicidade global, permanente — não expira). |
| Domain (novo) | `Synclass.Domain/Alunos/IdentificadorAlunoService.cs` | Orquestra geração + retry em colisão (mesmo desenho de `ConviteService.GerarCodigoUnicoAsync`), expõe `Task<string> GerarUnicoAsync(CancellationToken)`. |
| Domain (novo) | `Synclass.Domain/Alunos/LimiteDeTentativasDeIdentificadorAlunoExcedidoException.cs` | Guardrail contra loop indefinido (mesmo padrão de `LimiteDeTentativasDeCodigoConviteExcedidoException`). |
| Infrastructure (novo) | `Synclass.Infrastructure/Alunos/GeradorDeIdentificadorAluno.cs` | `RandomNumberGenerator`, alfabeto sem caracteres ambíguos. |
| Infrastructure (novo) | `Synclass.Infrastructure/Alunos/IdentificadorAlunoUnicidadeChecker.cs` | Consulta `UsuarioRepository`/`MatriculaRepository` (ou `DbContext` direto) pelas duas colunas. |
| Infrastructure | Migration nova | Ver "Modelo de dados". |
| Infrastructure | `Configurations/UsuarioConfiguration.cs` | Mapeia `IdentificadorAluno`, índice único parcial. |
| Infrastructure | `Configurations/MatriculaConfiguration.cs` | Mapeia `IdentificadorAluno`, índice único parcial (distinto do índice já existente de `IdentificadorProvisorio`). |
| Domain (existente) | `Synclass.Domain/Usuarios/CadastroUsuarioService.cs` (#61) | Injeta `IdentificadorAlunoService`; gera antes de `Usuario.Cadastrar`/`AdicionarPapel` quando `papel == Aluno`. |
| Domain (existente) | `Synclass.Domain/Convites/ConviteService.cs` (#63, e #2 pelo mesmo code path) | Injeta `IdentificadorAlunoService`; `ObterOuCriarUsuarioAsync` gera antes de criar/atualizar o `Usuario`, só quando o papel Aluno é de fato anexado (idempotência já existente é preservada — ver `AdicionarPapelAlunoIdempotente`). |
| Domain (existente) | `Synclass.Domain/Matriculas/CadastroAlunoProvisorioService.cs` | Injeta `IdentificadorAlunoService`; gera antes de `Matricula.CriarProvisoria`. |
| Api | `Program.cs` | Registro DI dos três novos tipos (Domain interface → Infrastructure implementação) e do `IdentificadorAlunoService`. |

## Modelo de dados

```
Usuario
+ IdentificadorAluno (varchar, nullable)
  índice único parcial: WHERE "IdentificadorAluno" IS NOT NULL

Matriculas
+ IdentificadorAluno (varchar, nullable)
  índice único parcial: WHERE "IdentificadorAluno" IS NOT NULL
  (coluna independente de IdentificadorProvisorio já existente —
  não reaproveitar/confundir)
```

Migration nova (nunca editar `20260820092650_AdicionaCodigoConvite` nem
qualquer outra já aplicada).

## Regra de geração

- Formato: `ALU-` + 4 caracteres do alfabeto
  `23456789ABCDEFGHJKMNPQRSTUVWXYZ` (31 símbolos, sem `0`, `O`, `1`, `I`,
  `L`) — mesmo racional de "pensado para leitura/digitação humana" do
  card.
- Fonte: `RandomNumberGenerator` (mesma técnica de
  `GeradorDeTokenConvite`/`GeradorDeCodigoConvite`).
- Unicidade permanente (nunca expira, diferente do código de convite de
  5 dígitos): checagem cobre `Usuario.IdentificadorAluno` e
  `Matricula.IdentificadorAluno` simultaneamente, já que ambos podem
  conter um identificador de Aluno.
- Retry em colisão com teto de tentativas (mesmo valor de referência de
  `ConviteService`, 20 tentativas) — acima disso,
  `LimiteDeTentativasDeIdentificadorAlunoExcedidoException`.

## Edge points

1. **Identificador é por papel Aluno, não por Usuario.** Um `Usuario`
   que nunca teve papel Aluno não recebe identificador (coluna
   permanece `null`). Um `Usuario` com papéis Professor e Aluno tem um
   único `IdentificadorAluno` (gerado quando o papel Aluno foi anexado,
   independente de já ser Professor antes ou depois).
2. **Imutabilidade.** Nenhum método público de `Usuario`/`Matricula`
   permite reatribuir `IdentificadorAluno` depois de gravado — mesma
   técnica de propriedade com setter privado já usada no resto do
   Domain.
3. **`AdicionarPapel` idempotente (issue #4).** Quando
   `ConviteService.AdicionarPapelAlunoIdempotente` engole
   `PapelJaAtribuidoException` (papel já presente), nenhum identificador
   novo é gerado — o `Usuario` já tinha um. Evita gerar (e descartar) um
   identificador à toa numa chamada que não muta nada.
4. **Fora de escopo, registrado como inconsistência conhecida** (ver
   `task.md`): reconciliação entre o `IdentificadorAluno` de uma
   `Matricula` provisória e o do `Usuario` criado/reaproveitado quando
   essa Matrícula é promovida via `ConviteService` (issue #2, já
   mergeada em `main`, mas não listada pelo card #70 como consumidora a
   atualizar). Pode resultar em dois identificadores distintos para a
   mesma pessoa física. Não implementado neste card — precisa de decisão
   de produto em issue própria (qual dos dois prevalece, ou se a
   promoção deveria carregar o identificador da Matrícula provisória
   para o Usuario).
5. **Exposição em tela.** O card cita telas ("Dado um identificador já
   existente, quando exibido em qualquer tela...") só como propriedade
   do dado (legibilidade), não como um critério que exige alterar uma
   tela específica neste card — nenhuma tela hoje lista Alunos com um
   ID técnico visível para trocar. Fica como próximo passo natural, não
   implementado aqui (ver `task.md`, "Fora de escopo").

## Dependência de outras Tasks

- Issues #61 e #63 já mergeadas em `main` (`CadastroUsuarioService` e
  `ConviteService.AceitarPorCodigoAsync` já existem) — este card só
  adiciona a geração do identificador nos pontos de criação/atualização
  de `Usuario` que elas já chamam, sem alterar o contrato/comportamento
  existente delas além disso.
