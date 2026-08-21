# Implementação: Professor e Aluno entram com login do Google (#65)

Fluxo paralelo ao login OTP (issue #18, `LoginService`) — não o substitui,
não o modifica. Mesma identidade única por `Contato` (`Usuario.Contato`):
login Google só autentica quem já existe, nunca cria conta implicitamente
(mesma decisão de produto do OTP — ver RN do card).

## Entidades/classes afetadas

- **Domain** (`Synclass.Domain.Autenticacao`):
  - `IValidadorDeIdTokenGoogle` (novo): `Task<InformacoesIdTokenGoogle?> ValidarAsync(string idToken, CancellationToken)`.
    `InformacoesIdTokenGoogle` (record): `string Email`, `bool EmailVerificado`.
  - `LoginComGoogleService` (novo, sealed, injeção via construtor:
    `IUsuarioRepository`, `IValidadorDeIdTokenGoogle`, `IGeradorDeTokenSessao`):
    `Task<ResultadoLoginGoogle> AutenticarAsync(string idToken, CancellationToken)`.
    Fluxo: valida token (null → `TokenGoogleInvalidoException`) → checa
    `EmailVerificado` (false → `EmailGoogleNaoVerificadoException`) →
    normaliza e-mail (`Contato.Normalizar`, já suporta e-mail) → busca por
    `IUsuarioRepository.BuscarPorContatoAsync` → encontrado: gera token via
    `IGeradorDeTokenSessao.Gerar` (reaproveitado do OTP, sem duplicar) e
    retorna `ResultadoLoginGoogle(Login: resultado, CadastroPendente: false, EmailNormalizado)`;
    não encontrado: retorna `ResultadoLoginGoogle(Login: null, CadastroPendente: true, EmailNormalizado)`.
  - `ResultadoLoginGoogle` (record): `ResultadoLogin? Login`, `bool CadastroPendente`, `string EmailNormalizado`.
  - `TokenGoogleInvalidoException`, `EmailGoogleNaoVerificadoException` (novas):
    herdam `LoginRejeitadoException`, mesma família de `ContatoSemIdentidadePlenaException`/`CodigoOtpInvalidoException`.
  - **Não toca** `LoginService`, `ContatoSemIdentidadePlenaException`, nem
    `Usuario`/`Contato` — reaproveita tudo sem alterar contrato existente.
- **Infrastructure** (`Synclass.Infrastructure.Autenticacao`):
  - `ValidadorDeIdTokenGoogle` (novo): implementa `IValidadorDeIdTokenGoogle`
    envolvendo `Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken, new ValidationSettings { Audience = [GoogleClientId] })`
    (pacote NuGet `Google.Apis.Auth`, adicionar a `Synclass.Infrastructure.csproj`).
    Captura `InvalidJwtException` e devolve `null` (nunca deixa a exceção do
    SDK cruzar a fronteira Domain/Infrastructure).
  - Config: `GoogleClientId` lido de `IConfiguration` (`GOOGLE_CLIENT_ID`),
    injetado no construtor (sem acesso estático a `IConfiguration` dentro
    da classe — mesma regra de DI do resto do projeto).
- **Api** (`Synclass.Api.Controllers.AutenticacaoController`):
  - Novo `POST /auth/google`, mesmo controller dos outros dois endpoints
    (mesmo recurso `auth`). `LoginGoogleRequest(string IdToken)`.
    `LoginGoogleResponse(bool CadastroPendente, string? Token, Guid? UsuarioId, string? Nome, string[]? Papeis, string? Email)`
    — quando `CadastroPendente = true`, só `Email` vem preenchido; quando
    `false`, os campos de sessão vêm preenchidos e `Email` fica `null`
    (contrato único evita dois DTOs quase-idênticos).
  - Exceptions tratadas: `LoginRejeitadoException` (`TokenGoogleInvalidoException`,
    `EmailGoogleNaoVerificadoException`) → 400, mesmo padrão de log
    (`LoginGoogleRejeitado {TrackId} {Motivo}` — sem mascarar contato aqui
    porque o e-mail já vem do próprio Google, não é input não-confiável do
    usuário; mas ainda não logar o e-mail em texto claro, só o motivo).
  - `Program.cs`: `AddScoped<IValidadorDeIdTokenGoogle, ValidadorDeIdTokenGoogle>()`,
    `AddScoped<LoginComGoogleService>()`.
- **Frontend**:
  - `frontend/src/lib/auth/google.ts` (novo): `obterIdTokenGoogle(): Promise<string | null>`.
    Ramificação `Platform.OS === 'web'` (mesmo padrão inline de
    `lib/auth/sessao.ts`, não arquivo `.native.ts`/`.web.ts` separado):
    web usa Google Identity Services (`google.accounts.id`, script
    carregado sob demanda); nativo usa `expo-auth-session`
    (`Google.useAuthRequest` ou equivalente). Cancelamento do usuário →
    `null`, nunca lança.
  - `frontend/src/lib/api/auth.ts`: `loginComGoogle(idToken: string)`,
    mesmo contrato de erro tipado das outras duas funções, mais o caso de
    sucesso com `cadastroPendente`.
  - `frontend/src/components/molecules/BotaoLoginGoogle.tsx` (novo):
    encapsula `obterIdTokenGoogle` + `loginComGoogle`; props
    `onAutenticado(token, usuarioId, nome, papeis)`,
    `onCadastroPendente(email)`, `onErro(mensagem)` — a tela decide
    navegação, o componente só orquestra a chamada.
  - `login/index.tsx`, `app/professor/cadastro.tsx`, `app/aluno/cadastro.tsx`:
    incluem `BotaoLoginGoogle`. Em `login/index.tsx`, como o login não sabe
    o papel de quem está entrando, `onCadastroPendente` mostra as duas
    opções de cadastro (Professor/Aluno) com o e-mail já pronto pra
    encaminhar via `?email=`; nos dois `cadastro.tsx`, `onCadastroPendente`
    não deveria disparar (o próprio formulário já está aberto) — usar o
    e-mail só para pré-preencher se o campo de contato ainda estiver vazio.

## Contrato de API

- `POST /auth/google`
  - Request: `{ "idToken": string }`
  - 200 (usuário existente): `{ "cadastroPendente": false, "token": string, "usuarioId": guid, "nome": string, "papeis": string[], "email": null }`
  - 200 (sem usuário correspondente): `{ "cadastroPendente": true, "token": null, "usuarioId": null, "nome": null, "papeis": null, "email": string }`
  - 400: `{ "mensagem": string }` (token inválido, ou e-mail não verificado)

## Modelo de dados

Nenhuma migration. O login Google não introduz coluna nova — a
correspondência é feita contra `Usuario.Contato` já existente (quando o
contato de cadastro é um e-mail, o mesmo valor normalizado do e-mail do
Google bate direto). Não há `GoogleSub`/`GoogleId` armazenado nesta Task —
fora de escopo dos critérios de aceite (que só pedem correspondência por
e-mail).

## Edge points

- Contato de cadastro por telefone (não e-mail): login Google nunca vai
  corresponder (o Google só devolve e-mail) — cai automaticamente no
  caminho "sem usuário correspondente" e manda pro cadastro. Não é um caso
  de erro, é o comportamento correto sem tratamento especial.
- Normalização do e-mail do Google: mesma função `Contato.Normalizar` já
  usada em todo o resto do projeto (minúsculas) — sem lógica nova de
  comparação.
- Um usuário que já tem conta via OTP e usa Google pela primeira vez:
  autentica a mesma identidade automaticamente (é só uma busca por
  contato) — nenhuma migração de dado necessária, funciona no primeiro uso.
- `GoogleClientId` vazio em ambiente de dev sem credenciais reais: o
  endpoint deve continuar funcionando nos testes (fake
  `IValidadorDeIdTokenGoogle` na suíte, sem chamar a lib real) — só o SDK
  real exige a env var configurada em produção.

## Dependência de outras Tasks

Nenhuma — não depende de nenhuma Task do épico #60 (convite por código),
conforme já indicado no card. Só reaproveita `IGeradorDeTokenSessao`/
`Usuario`/`Contato` já existentes desde a issue #18.
