# Implementation: Código de convite curto (#62)

## Entidades/classes afetadas

- `Domain/Convites/Convite.cs`: `Gerar(...)` ganha parâmetro `codigo`
  (string, já gerado/validado por quem chama, mesmo racional de `token`) e
  nova propriedade `Codigo { get; private set; }`.
- `Domain/Convites/IGeradorDeCodigoConvite.cs` (novo): `string Gerar()` —
  interface fina, mesmo padrão de `IGeradorDeTokenConvite`/`IGeradorDeCodigoOtp`.
- `Infrastructure/Convites/GeradorDeCodigoConvite.cs` (novo): implementação
  real, mesma técnica de `GeradorDeCodigoOtp`
  (`RandomNumberGenerator.GetInt32(0, 100_000).ToString("D5")`) — a RN do
  card já resolveu a dúvida do critério técnico original ("reaproveita OTP
  ou implementação própria?"): mesma técnica de geração, mas é uma classe
  própria porque a regra de unicidade (contra convites ativos, não contra
  usuários) e o consumidor (`ConviteService`, não `LoginService`) são
  diferentes.
- `Domain/Convites/IConviteRepository.cs`: novo método
  `Task<bool> ExisteCodigoAtivoAsync(string codigo, CancellationToken ct)` —
  "ativo" = `UsadoEm is null && ExpiraEm > agora`. Como a checagem depende
  de "agora", o repositório recebe o instante via parâmetro do `ConviteService`
  (que já tem `IClock`) em vez de o repositório depender de `IClock`
  diretamente — mesmo racional de manter `IClock` só na camada de domínio/serviço.
  Assinatura final: `ExisteCodigoAtivoAsync(string codigo, DateTimeOffset agora, CancellationToken ct)`.
- `Infrastructure/Persistence/ConviteRepository.cs`: implementa o método
  acima com uma query EF (`AnyAsync`).
- `Infrastructure/Persistence/Configurations/ConviteConfiguration.cs`:
  `builder.Property(c => c.Codigo).IsRequired().HasMaxLength(5)` — sem
  índice único (unicidade é só contra convites ativos, não pode ser
  constraint de banco).
- `Domain/Convites/ConviteService.cs`: `GerarAsync` chama
  `GerarCodigoUnicoAsync` (novo método privado) antes de montar o convite —
  loop chamando `_geradorDeCodigo.Gerar()` e `_convites.ExisteCodigoAtivoAsync`
  até achar um código livre, contando tentativas para o log. Construtor
  ganha `IGeradorDeCodigoConvite geradorDeCodigo`.
- `Api/Controllers/ConvitesController.cs`: `GerarConviteResponse` ganha
  `Codigo`; `LogConviteGerado` ganha `Tentativas` (nome do parâmetro
  estruturado, ex: `{Tentativas}`).
- `Program.cs`: registra `IGeradorDeCodigoConvite` →
  `GeradorDeCodigoConvite`.
- Frontend: `lib/api/convites.ts` (`GerarConviteResultado` ganha `codigo:
  string`), `components/molecules/ConviteGerado.tsx` (novo prop `codigo:
  string`, exibido como `Paragraph` acima/ao lado do link),
  `app/professor/[professorId]/convites/novo.tsx` (repassa `resultado.codigo`).

## Contrato de API

`POST /professores/{professorId}/convites` — sem mudança de request.
Response ganha campo novo:

```
{ "conviteId": "...", "token": "...", "codigo": "12345", "expiraEm": "..." }
```

## Modelo de dados

Migration nova: coluna `Codigo` (`nvarchar(5)`, `NOT NULL`) na tabela
`Convites`. Sem índice único — a unicidade é uma regra de negócio contra o
subconjunto "convites ativos", não uma constraint global de banco (dois
convites finalizados podem compartilhar código).

## Edge points (não cobertos por Gherkin)

- Loop de geração com tentativas: não há teto explícito no card, mas o
  espaço de 100.000 combinações e o volume esperado de convites simultâneos
  tornam colisões repetidas extremamente raras — sem necessidade de um
  limite de tentativas com fallback de erro. Se dev-review apontar isso como
  risco, um teto alto (ex: 20 tentativas → erro) é aceitável como ajuste.
- `AceitarAsync` (aceite por link) não muda nesta Task — o código só é
  consumido pela Task #63 ("Aluno usa o código"), que reaproveita o mesmo
  `Convite`/`MarcarUsado`, não duplica a regra de expiração/uso único.

## Dependência de outras Tasks

Nenhuma (independente dentro do épico #60) — mas é pré-requisito da Task
#63 ("Aluno usa o código de convite para entrar na turma do Professor").
