# Implementação: Convite via WhatsApp (#2)

## Entidades/classes afetadas

**Domain** (`Synclass.Domain.Convites`, novo módulo — paralelo a
`Usuarios`/`Matriculas`, consumindo os dois):

- `Convite` — entidade nova. `Id`, `ProfessorId`, `Contato` (normalizado),
  `ContatoTipo`, `Token`, `MatriculaId?` (origem, quando gerado a partir de
  um Aluno provisório específico), `ExpiraEm`, `UsadoEm?`, `CreatedAt`.
  Factory `Gerar(...)`; método `MarcarUsado(IClock)` (auto-guardado, mesmo
  estilo de `Matricula.Promover`): rejeita com `ConviteInvalidoException` se
  já usado, com `ConviteExpiradoException` se expirado, senão define
  `UsadoEm`.
- `IConviteRepository` — `BuscarPorTokenAsync`, `AdicionarAsync`,
  `SalvarAsync`.
- `IGeradorDeTokenConvite` — abstrai a geração do token (implementado em
  Infrastructure com `RandomNumberGenerator`, mesmo racional de
  `IGeradorDeCodigoOtp` da issue #18, mas com muito mais entropia: o token
  vira parte de uma URL pública, não um código digitado).
- `ConviteService` — orquestra `GerarAsync` e `AceitarAsync`, espelhando a
  forma de `LoginService` (dois métodos relacionados no mesmo serviço,
  injeta `IConviteRepository` + `IMatriculaRepository` + `IUsuarioRepository`
  + `IGeradorDeTokenConvite` + `IClock` + `diasValidade` configurável).
- `ConviteRejeitadoException` (abstract, mesmo padrão de
  `CadastroProfessorRejeitadoException`/`MatriculaRejeitadaException`) e
  subtipos: `ConviteExpiradoException`, `ConviteInvalidoException` (token
  não encontrado ou já usado), `ConviteContatoDivergenteException`,
  `ContatoJaVinculadoException`, `MatriculaOrigemInvalidaException`.

**Extensão pequena e não disruptiva em módulos existentes**:

- `Contato` (`Usuarios/Contato.cs`) ganha `TipoContato` (enum
  `Email`/`Telefone`) e `IdentificarTipo(string contatoNormalizado)` — não
  reimplementa a distinção já feita internamente por `Normalizar`, só a
  expõe para quem precisa persistir o tipo separadamente (coluna
  `ContatoTipo` de `Convites`).
- `IMatriculaRepository`/`MatriculaRepository` ganham `BuscarPorIdAsync`
  (validar/promover a matrícula de origem) e `BuscarVinculoAsync(professorId,
  alunoUsuarioId)` (checar se o Aluno já está vinculado pleno a este
  Professor — critério de aceite 4). A issue #3, única outra consumidora
  desta interface, já está mergeada em `main`, então não há mais risco de
  colisão de worktree paralela ao estendê-la (diferente da precaução tomada
  naquela Task em relação a `Usuarios/`).

**Infrastructure**: `ConviteConfiguration` (índice único em `Token`),
`ConviteRepository` (EF Core), `GeradorDeTokenConvite`, `DbSet<Convite>
Convites` em `SynclassDbContext`, migration `CriaConvite`.

**Api**: `ConvitesController` —
`POST /professores/{professorId}/convites` (gerar) e
`POST /convites/{token}/aceite` (aceitar).

**Frontend**: `frontend/src/lib/api/convites.ts` (mesmo padrão de
`alunosProvisorios.ts`); `frontend/src/lib/whatsapp.ts` (monta o link
`wa.me`, client-side, sem integração real de envio); tela
`frontend/src/app/professor/[professorId]/convites/novo.tsx` (gerar +
compartilhar); rota pública `frontend/src/app/convite/[token].tsx`
reaproveitando `CadastroProfessorForm` (nome + contato) tal como está, só
trocando a chamada de Api (`aceitarConvite` em vez de `cadastrarProfessor`).

## Decisão documentada: `professorId` como parâmetro de rota, não de sessão

Mesma decisão e mesma justificativa já registradas em
`docs/specs/3-aluno-provisorio/implementation.md`: não existe hoje
middleware de autenticação (`AddAuthentication`/`AddJwtBearer`) consumido
pela Api — o login por OTP (issue #18) emite um JWT, mas nenhum controller
lê `ClaimsPrincipal`. Adicionar esse middleware é escopo da issue #23, não
desta. O endpoint de geração de convite recebe `professorId` explicitamente
na rota (`POST /professores/{professorId}/convites`), sem validar que o
chamador é aquele Professor. O frontend espelha isso com o segmento dinâmico
`[professorId]`, igual à tela de cadastro de Aluno provisório.

## Decisão documentada: sem integração real de WhatsApp

A RN só pede um link `wa.me` (link universal do WhatsApp, aberto pelo
próprio dispositivo do Professor) — não uma API de envio de mensagem. O
backend nunca conhece WhatsApp; ele só devolve `token` (e `expiraEm`) para o
frontend montar a URL do link de convite e, a partir dela, o link `wa.me`
com uma mensagem pré-preenchida. `frontend/src/lib/whatsapp.ts` embrulha
essa montagem (conforme `code-style.md#dependências` — nenhuma lib de
terceiros aqui, só `string` puro, mas ainda assim isolado num módulo
próprio, testável, para a tela não montar a URL inline).

## Decisão documentada: contato no aceite deve corresponder ao convite

A RN (Gherkin, critério de aceite 2) descreve o aceite como "completa o
cadastro com o mesmo contato do convite". A tela de aceite reaproveita
`CadastroProfessorForm` (nome + contato) tal como pedido pelo card, então o
contato chega como texto livre, não como um valor fixo vindo do link. A
`ConviteService.AceitarAsync` normaliza o contato submetido e exige que seja
**igual** ao `Convite.Contato` (already normalizado na geração) —
divergência rejeita com `ConviteContatoDivergenteException`, sem tocar em
nenhuma identidade. Essa igualdade obrigatória é o que torna o edge point dos
Critérios técnicos ("se o contato informado já pertence a outro usuário
pleno diferente do dono do convite, rejeita") estruturalmente garantido:
como o contato aceito precisa ser exatamente `Convite.Contato`, o usuário
resolvido por `IUsuarioRepository.BuscarPorContatoAsync(Convite.Contato)` é,
por construção, sempre "o dono do convite" — não existe caminho de código em
que um usuário pleno *diferente* seja alcançado. O teste deste edge point
cobre a rejeição por divergência (nenhuma mutação ocorre), não um branch de
"dono diferente" separado, que seria inalcançável dado este desenho.

## Decisão documentada: aceite não emite sessão/token de login

O card não pede login automático após o aceite — só criação/promoção do
usuário e do vínculo. `POST /convites/{token}/aceite` devolve
`usuarioId`/`nome`/`papeis`, sem token de sessão (issue #18 é um fluxo
separado; o Aluno recém-criado faz login normalmente depois, se quiser).
Mantém o escopo deste card fechado em "convite + vínculo", igual ao
precedente do cadastro de Professor (issue #1), que também não loga
automaticamente.

## Contrato de API

### `POST /professores/{professorId}/convites`

Request:
```json
{ "contato": "11987654321", "matriculaId": null }
```

Response 200:
```json
{ "conviteId": "guid", "token": "string-alta-entropia", "expiraEm": "2026-08-20T00:00:00Z" }
```

Response 400 (contato inválido, `matriculaId` de origem inválido, ou Aluno
já vinculado pleno a este Professor):
```json
{ "mensagem": "..." }
```

Response 404 (`professorId` sem `Usuario` correspondente):
```json
{ "mensagem": "..." }
```

### `POST /convites/{token}/aceite`

Request:
```json
{ "nome": "João Pedro", "contato": "11987654321" }
```

Response 200:
```json
{ "usuarioId": "guid", "nome": "João Pedro", "papeis": ["Aluno"] }
```

Response 400 (token inválido/já usado, expirado, nome inválido, ou contato
divergente do convite):
```json
{ "mensagem": "..." }
```

## Modelo de dados

Nova tabela `Convites`:

| Coluna | Tipo | Notas |
|---|---|---|
| `Id` | uuid PK | gerado pela aplicação |
| `ProfessorId` | uuid FK → `Usuarios.Id` | `Restrict` no delete |
| `Contato` | varchar(320) | normalizado, mesmo padrão de `Usuario.Contato` |
| `ContatoTipo` | int (enum) | `Email` = 0, `Telefone` = 1 |
| `Token` | varchar(64) | índice único |
| `MatriculaId` | uuid FK → `Matriculas.Id`, nullable | só quando originado de um Aluno provisório específico |
| `ExpiraEm` | timestamptz | `CreatedAt` + `Convites:DiasValidade` |
| `UsadoEm` | timestamptz, nullable | preenchido no primeiro aceite |
| `CreatedAt` | timestamptz | |

Configuração `Convites:DiasValidade` em `appsettings.json` (default `7`),
lida de forma obrigatória no `Program.cs` — mesmo padrão (falha explícita no
startup se ausente/inválida) de `Jwt:ExpiracaoDias`.

## Edge points

- Token gerado com `RandomNumberGenerator.GetBytes(32)` + base64url (43
  caracteres, ~256 bits de entropia) — não sequencial, não derivável do
  `ConviteId`/`ProfessorId`.
- Convite aceito não pode ser reutilizado: `Convite.MarcarUsado` rejeita se
  `UsadoEm` já preenchido, antes de qualquer mutação de `Usuario`/`Matricula`.
- Convite com `MatriculaId` de origem: a `ConviteService.GerarAsync` valida
  que a `Matricula` existe, pertence ao mesmo `ProfessorId`, e ainda não foi
  promovida (`AlunoUsuarioId is null`) — senão `MatriculaOrigemInvalidaException`.
  No aceite, a promoção chama `Matricula.Promover(usuario.Id)` diretamente
  sobre essa `Matricula` (nunca cria uma segunda linha nem casa por
  nome/contato), conforme a RN.
- Convite sem `MatriculaId` de origem: no aceite, se o `Usuario` resolvido
  (novo ou existente) ainda não tem nenhuma `Matricula` vinculada a este
  Professor, cria uma nova `Matricula` já plena via
  `Matricula.CriarVinculada(professorId, usuario.Id, clock)` — uma matrícula
  sem `NomeProvisorio`/`IdentificadorProvisorio`, já que nasceu de um
  cadastro completo, não de um registro provisório do Professor.
- Reaproveitamento de identidade no aceite segue exatamente o padrão de
  `CadastroProfessorService`: se já existe `Usuario` para o contato,
  reaproveita e adiciona o papel Aluno (rejeitando via
  `PapelJaAtribuidoException` se ele já for Aluno — capturado e tratado como
  sucesso idempotente pelo `ConviteService`, já que "já é Aluno" não deveria
  impedir a promoção de uma `Matricula` de origem específica).
- `professorId` inexistente é checado via `IUsuarioRepository.ExisteAsync`
  antes de criar o `Convite` — mesmo padrão e mesma justificativa (EF Core
  InMemory não aplica FK) de `CadastroAlunoProvisorioService`.

## Dependência de outras Tasks

Depende da identidade de usuário única (issue #1, mergeada) e do cadastro de
Aluno provisório (issue #3, mergeada) — ambas já em `main`, sem trabalho
paralelo pendente que compartilhe `SynclassDbContext.cs`/migration além da
issue #17 (limite de alunos por horário), que roda em paralelo numa
worktree separada e não tem relação de dados com `Convites`/`Matriculas`
além de tocar o mesmo `SynclassDbContext`/`ModelSnapshot` — ver aviso de
rebase no prompt de execução desta Task.
