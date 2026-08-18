# Implementação: acumular papéis de Professor e Aluno (#4)

## Entidades/classes afetadas

**Domain** — nenhuma mudança em `Usuario`/`PapelAtribuido`/`PapelUsuario`
(`Synclass.Domain/Usuarios/`): o suporte estrutural a múltiplos papéis na
mesma identidade já foi entregue pela issue #1 (`AdicionarPapel`, índice
único `PapeisUsuario(UsuarioId, Papel)`) e já é reaproveitado tanto por
`CadastroProfessorService` quanto por `ConviteService`. O que falta é só
sinalizar, de volta para a Api, quando um papel foi de fato anexado a uma
identidade já existente (para o log `PapelAdicionado`) — hoje esse dado é
descartado nas duas orquestrações:

- `CadastroProfessorService.CadastrarProfessorAsync`: passa a retornar
  `ResultadoCadastroProfessor(Usuario Usuario, bool UsuarioReaproveitado)`
  em vez de `Usuario` puro.
- `ConviteService.AceitarAsync`: `ResultadoAceiteConvite` ganha o campo
  `PapelAdicionado` (bool) — `AdicionarPapelAlunoIdempotente` passa a
  retornar se conseguiu anexar (`true`) ou foi no-op por já existir
  (`false`, `PapelJaAtribuidoException` capturada).

**Api** (`Synclass.Api/`):

- `Program.cs`: registra `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`
  reaproveitando `Jwt:SigningKey` (mesma chave já usada por
  `GeradorDeTokenSessaoJwt` para assinar) + `AddAuthorization()`. Adiciona
  `app.UseAuthentication()` logo antes do `app.UseAuthorization()` já
  existente. Isso é o middleware que a issue #18 deixou deliberadamente de
  fora ("Fora de escopo desta Task", `docs/specs/18-login-otp/implementation.md`)
  por não ter, até então, nenhum endpoint que precisasse validar o token —
  a issue #4 é a primeira que precisa.
- `Controllers/HorariosController.cs`, `AlocacoesHorarioController.cs`,
  `AlunosProvisoriosController.cs`, `RegraDeCobrancaController.cs`,
  `ConfiguracoesController.cs`: ganham `[Authorize(Roles = "Professor")]`
  a nível de classe — todos vivem sob `professores/{professorId}/...` e já
  eram, na intenção do produto, exclusivos de quem tem o papel Professor.
- `Controllers/ConvitesController.cs`: `[Authorize(Roles = "Professor")]`
  só no método `Gerar` (`POST professores/{professorId}/convites` — quem
  gera convite precisa ser Professor); `[AllowAnonymous]` explícito no
  método `Aceite` (`POST convites/{token}/aceite` — quem aceita ainda não
  tem sessão, é o próprio fluxo que cria/reaproveita a identidade).
- `Controllers/ProfessoresController.cs` (`POST /professores/cadastro`) e
  `Controllers/AutenticacaoController.cs` (`/auth/*`): sem mudança —
  seguem públicos por não terem `[Authorize]`, e não há política global de
  fallback sendo introduzida (decisão de escopo, ver Edge points).
- `ProfessoresController`/`ConvitesController`: novo evento de log
  `PapelAdicionado {TrackId} {UsuarioId} {Papel}` (Information), disparado
  quando o serviço correspondente indica que o papel foi de fato anexado a
  uma identidade já existente (distinto do já existente
  `UsuarioCadastrado`, que continua cobrindo o caso de identidade nova).

**Frontend** (`frontend/src/`):

- `lib/auth/sessao.ts`: ganha `salvarPapeis(papeis: string[])`/
  `lerPapeis(): Promise<string[] | null>`, mesmo wrapper fino sobre
  `expo-secure-store` já usado para o token.
- `lib/auth/contexto-sessao.tsx` (novo): `SessaoProvider` + hook
  `useSessao()` — carrega token/papéis salvos ao montar, expõe `papeis`,
  `papelAtivo` (persistido separadamente; default o primeiro papel) e
  `definirPapelAtivo(papel)`.
- `components/organisms/AlternadorDePapel.tsx` (novo): abas
  "Professor"/"Aluno"; não renderiza nada (`null`) quando `papeis.length
  <= 1` — Critério técnico explícito do card.
- `app/login/verificar.tsx`: além de `salvarToken`, agora chama
  `salvarPapeis(resultado.papeis)` e navega para `/painel` (`router.replace`)
  em vez de só exibir a confirmação estática.
- `app/painel/index.tsx` (novo): tela pós-login mínima — guarda de rota
  (redireciona para `/login` se não houver token salvo), `AlternadorDePapel`
  condicional, e uma lista de ações que muda conforme `papelAtivo`
  (placeholder textual por papel — as telas reais de "criar horário",
  "ver meus Alunos" etc. são objeto de issues já existentes/futuras, não
  desta).

## Contrato de API

Nenhuma rota nova. Mudança de contrato é só de **autorização** nas rotas já
existentes sob `professores/{professorId}/...` (issue #1, #6, #7, #8, #11,
#17) e em `POST professores/{professorId}/convites` (issue #2):

- Sem header `Authorization: Bearer <token>` → `401 Unauthorized`.
- Com token válido mas sem claim `role=Professor` → `403 Forbidden`.
- Com token válido e `role=Professor` → comportamento inalterado.

`POST /auth/confirmacao` mantém o contrato já entregue na issue #18
(`{ token, usuarioId, nome, papeis: string[] }`) — nenhuma mudança de
schema, só consumo pelo frontend que antes descartava `papeis`.

## Modelo de dados

Nenhuma migration nova — reaproveita `Usuarios`/`PapeisUsuario` (issue #1),
cujo índice único `(UsuarioId, Papel)` já impede duplicidade a nível de
banco.

## Edge points

- **`definirSessao` como único ponto de gravação de sessão** (achado do
  `qa-review` do PR #34): a primeira versão de `app/login/verificar.tsx`
  chamava `salvarToken`/`salvarPapeis` (`lib/auth/sessao.ts`) diretamente,
  contornando o `SessaoProvider` — o estado em memória do Provider só é
  carregado uma vez, no `useEffect` de montagem em `_layout.tsx`, então
  `token` continuava `null` até o próximo mount, e a guarda de rota de
  `app/painel/index.tsx` batia de volta para `/login` mesmo com o backend
  já tendo autenticado (2 dos 4 critérios de aceite falhavam no QA). Fix:
  `contexto-sessao.tsx` expõe `definirSessao(token, papeis)`, que persiste
  em `sessao.ts` **e** atualiza o estado em memória atomicamente;
  `verificar.tsx` chama só isso, nunca `salvarToken`/`salvarPapeis`
  diretamente. Qualquer tela futura que logue o usuário deve seguir o
  mesmo caminho.
- **Autorização por papel, não por posse do recurso**: `[Authorize(Roles =
  "Professor")]` garante que quem chama tem o papel Professor em algum
  lugar do sistema, mas não que `{professorId}` da rota é o próprio usuário
  autenticado — isso já não era verificado antes (rota livre) e continua
  fora do escopo desta issue (a RN/Critérios técnicos do card pedem
  "autorização por papel, não por rota fixa", não isolamento por
  propriedade do recurso). Fica registrado aqui como próximo gap de
  segurança, não resolvido por #4.
- **Endpoint Aluno-only**: `MarcacoesHorarioController` (issue #9, "Aluno
  marca horário vago" — mergeada em `main` depois do rascunho original
  desta spec) é hoje o único controller exclusivo do papel Aluno. Achado no
  `dev-review` deste PR: ele tinha ficado sem `[Authorize]` mesmo depois
  deste PR ligar `app.UseAuthentication()`/`app.UseAuthorization()`, ficando
  acessível sem token — corrigido com `[Authorize(Roles = "Aluno")]` a
  nível de classe, mais os três testes de fumaça (401/403/aceita)
  equivalentes aos de `HorariosController` em `AutorizacaoEndpointTests.cs`.
- **Sem policy global de fallback**: decisão deliberada de não adicionar
  `options.FallbackPolicy = RequireAuthenticatedUser` — isso exigiria
  `[AllowAnonymous]` explícito em todo endpoint público existente
  (`/professores/cadastro`, `/auth/*`, aceite de convite) e nenhum
  Critério técnico do card pede fechar tudo por padrão; autorização é
  aplicada só onde o produto já pede papel específico.
- **`AdicionarPapelAlunoIdempotente` muda de `void` para `bool`**: efeito
  colateral do log `PapelAdicionado` — o comportamento de negócio (engolir
  `PapelJaAtribuidoException`) não muda, só passa a informar o chamador.

## Dependência de outras Tasks

Nenhuma bloqueante. Depende conceitualmente das issues #1 (`Usuario`/
`PapeisUsuario`) e #18 (login retornando papéis, JWT com claims `role`),
ambas já mergeadas. A issue #5 (Aluno vinculado a vários Professores) roda
em paralelo mexendo em `Matriculas/` — sem sobreposição de arquivo esperada
com esta Task, exceto risco padrão de `Program.cs`/`ModelSnapshot.cs` se
ambas gerarem migration (esta Task não gera nenhuma).
