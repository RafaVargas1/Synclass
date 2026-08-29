# Desenho técnico — Login com Apple na web (#212)

## Entidades/classes afetadas

### Domain (novo, `Synclass.Domain.Autenticacao` — mesmo namespace do Google)

- `InformacoesIdTokenApple` (record): `Email`, `EmailVerificado` — espelha `InformacoesIdTokenGoogle` (`backend/src/Synclass.Domain/Autenticacao/IValidadorDeIdTokenGoogle.cs:12`).
- `IValidadorDeIdTokenApple` (interface): `Task<InformacoesIdTokenApple?> ValidarAsync(string idToken, CancellationToken ct)` — mesmo contrato do Google (nunca lança para token malformado, devolve `null`).
- `IClienteJwksApple` (interface nova — **não existe wrapper de JWKS/HTTP igual a este no repo ainda**, é decisão nova): `Task<IReadOnlyCollection<SecurityKey>> ObterChavesAsync(CancellationToken ct)` — abstrai a busca das chaves públicas da Apple, para `ValidadorDeIdTokenApple` (Domain) não depender de `HttpClient` diretamente (mesmo racional de `IValidadorDeIdTokenGoogle` abstrair o SDK).
- `LoginComAppleService`: mesma estrutura de `LoginComGoogleService.cs:13-52` — construtor recebe `IUsuarioRepository`, `IValidadorDeIdTokenApple`, `IGeradorDeTokenSessao`; método `AutenticarAsync(string idToken, CancellationToken ct)` retornando `ResultadoLoginApple` (record espelhando `ResultadoLoginGoogle`, mesmos campos: `Login`, `CadastroPendente`, `EmailNormalizado`).
- Reaproveita sem mudança: `TokenGoogleInvalidoException`/`EmailGoogleNaoVerificadoException` — **não** (nomes específicos do Google não fazem sentido pro Apple) — criar `TokenAppleInvalidoException`/`EmailAppleNaoVerificadoException`, mesmo padrão de mensagem das exceções já existentes (ver `docs/spec/code-style.md#mensagens-de-exceção`).

### Infrastructure (novo, `Synclass.Infrastructure.Autenticacao`)

- `ClienteJwksApple` (implementa `IClienteJwksApple`): `HttpClient` puro (mesmo padrão de `ClienteOAuthMercadoPago.cs`/`WhatsAppHttpClient.cs` — primeira pasta de HTTP puro foi criada em #203, este é mais um caso do mesmo padrão), `GET https://appleid.apple.com/auth/keys`, desserializa como `Microsoft.IdentityModel.Tokens.JsonWebKeySet` (classe já existente no pacote `Microsoft.IdentityModel.Tokens`, usado pela própria autenticação JWT do projeto — `Program.cs` já referencia esse namespace para o JWT de sessão, ver `IssuerSigningKey`/`SymmetricSecurityKey`) — `JsonWebKeySet.GetSigningKeys()` devolve os `SecurityKey` prontos para validação, sem precisar converter manualmente RSA/JWK.
  - **Cache**: as chaves da Apple raramente rotacionam, mas podem mudar — cachear em memória (`IMemoryCache` ou campo estático com expiração, decisão de implementação) por um período curto (ex: 1 hora) para não bater na API da Apple a cada login. Não é requisito do card, mas evita rate-limit desnecessário — se não der tempo, implementar sem cache é aceitável para esta Task (edge point, não critério de aceite).
- `ValidadorDeIdTokenApple` (implementa `IValidadorDeIdTokenApple`): usa `JwtSecurityTokenHandler().ValidateToken` (mesmo pacote `System.IdentityModel.Tokens.Jwt` já usado no projeto para o JWT de sessão) com `TokenValidationParameters { ValidIssuer = "https://appleid.apple.com", ValidAudience = <AppleClientId configurado>, IssuerSigningKeys = <chaves do IClienteJwksApple>, ValidateLifetime = true }`. Captura `SecurityTokenException` (classe base de todas as exceções de validação do pacote — assinatura inválida, issuer errado, audience errada, token expirado caem todas aqui) e devolve `null`, nunca deixa a exceção cruzar a fronteira Domain/Infrastructure (mesmo padrão do Google).

### Api

- `AutenticacaoController.cs` — endpoint novo `[HttpPost("apple")]`, mesmo padrão exato de `EntrarComGoogle` (`AutenticacaoController.cs:72-93`): recebe `LoginAppleRequest { string IdToken }`, chama `LoginComAppleService.AutenticarAsync`, mapeia pra `LoginAppleResponse` (mesmo shape de `LoginGoogleResponse`: `Token`, `UsuarioId`, `Nome`, `Papeis`, `CadastroPendente`, `Email`).
- `Program.cs`: registrar `IClienteJwksApple`/`ClienteJwksApple` via `AddHttpClient` (mesmo padrão dos outros clientes HTTP), `IValidadorDeIdTokenApple`/`ValidadorDeIdTokenApple`, `LoginComAppleService`.

### Frontend — botão e integração com Sign in with Apple JS

**Só a plataforma web** (o nativo é #213, task separada). Segue exatamente o padrão de `BotaoLoginGoogle.tsx`: molécula própria (`BotaoLoginApple.tsx`), estilo seguindo a diretriz de marca da Apple (botão preto/branco padrão "Sign in with Apple", não o `Button` genérico do app — mesma exceção documentada de `BotaoLoginGoogle.tsx` linha do comentário sobre diretrizes de marca).

**Carregamento do SDK**: `<script src="https://appleid.cdn-apple.com/appleauth/static/jsappleauth/appleid.auth.js">` (confirmado, URL oficial da Apple) carregado condicionalmente só em `Platform.OS === 'web'` (mesmo racional de `google.ts` ramificar por plataforma — o SDK da Apple só existe pra web).

```typescript
// Init, uma vez, análogo ao GoogleClientId de google.ts
AppleID.auth.init({
  clientId: process.env.EXPO_PUBLIC_APPLE_CLIENT_ID, // Services ID da Apple, não o AppleClientId do backend (são registros diferentes no Apple Developer, mesmo conceito do Google ter Client ID web vs Client ID iOS)
  scope: 'email name',
  redirectURI: `${process.env.EXPO_PUBLIC_APP_URL}/auth/apple/callback`, // precisa existir e estar cadastrada nos "Return URLs" do Services ID no Apple Developer Console — ação operacional, não só código
  usePopup: true,
});
```

**ATENÇÃO — forma exata de obter o `id_token` NÃO confirmada com certeza** (pesquisa nas docs oficiais da Apple não trouxe o snippet definitivo): pode ser via `AppleID.auth.signIn()` retornando uma `Promise` com o resultado, OU via `document.addEventListener('AppleIDSignInOnSuccess', (event) => ...)`/`AppleIDSignInOnFailure` (padrão de evento DOM, mais comum em versões antigas da doc da Apple). **Antes de implementar**: carregue o script real num browser de teste e inspecione `window.AppleID.auth` (`Object.keys`/`console.log`) pra confirmar qual API a versão atual expõe, em vez de assumir uma das duas. Estruture `obterIdTokenApple()` (novo arquivo `frontend/src/lib/auth/apple.ts`, mesmo contrato de `obterIdTokenGoogle()`: nunca lança, devolve `string | null`) em torno da que for confirmada.

## Contrato de API

**Endpoint novo**: `POST /auth/apple`

```
Request:  { "idToken": string }
Response 200 (login):            { "token": string, "usuarioId": string, "nome": string, "papeis": string[], "cadastroPendente": false, "email": null }
Response 200 (cadastro pendente): { "token": null, "usuarioId": null, "nome": null, "papeis": null, "cadastroPendente": true, "email": string }
```

Mesmo shape de `POST /auth/google` (`LoginGoogleResponse`) — mudar só o nome do tipo pra `LoginAppleResponse`, sem inventar campo novo.

## Modelo de dados

**Nenhuma migration** — não toca tabela nenhuma, mesmo perfil do login Google.

## Edge points não cobertos por critério de aceite Gherkin

- **`email_verified` como string, não booleano**: o token de identidade da Apple traz `email_verified` como a STRING `"true"`/`"false"` (não um JSON boolean) em pelo menos algumas versões documentadas do formato — se o parsing assumir `bool` direto, pode falhar silenciosamente ou lançar. Ao ler essa claim do `ClaimsPrincipal` resultante da validação, trate como string e compare (`claim.Value == "true"`), não assuma conversão automática de tipo.
- **E-mail de relay privado** (`@privaterelay.appleid.com`): se o usuário escolheu "Ocultar meu e-mail" no primeiro login, o e-mail retornado é um relay estável pra essa combinação app+usuário, não o e-mail real. O matching de conta continua sendo por e-mail normalizado (`Contato.Normalizar`), sem tratamento especial — se o Professor/Aluno cadastrou a conta com o e-mail real e depois usa Apple com relay oculto, cai (corretamente) em `CadastroPendente`, não é bug.
- **E-mail pode vir ausente em logins subsequentes**: a Apple só devolve `email`/nome na PRIMEIRA autorização do usuário para aquele app — em logins seguintes, o token pode não trazer o e-mail. Como o Synclass casa conta por e-mail a cada login (não guarda o `sub` do Apple), **isso pode quebrar reautenticação depois da primeira vez** — se o e-mail vier ausente num login que não é o primeiro, `informacoes.Email` seria vazio/nulo e o matching falharia. **Decisão**: se `email` vier ausente na claim, tratar como `TokenAppleInvalidoException` (mesmo caminho de token malformado) em vez de tentar prosseguir sem e-mail — documentar isso como limitação conhecida (usuário precisaria usar outro método de login se isso acontecer), já que resolver de verdade exigiria guardar o `sub` da Apple por usuário (mudança de modelo de dados, fora do escopo deste card).

## Dependência de outras Tasks

- **#65** (login Google): referência de desenho, `LoginComGoogleService`/`IValidadorDeIdTokenGoogle` são o padrão a espelhar.
- **Não depende de #192 nem de #195/#194** — feature de web, independente do app nativo.
- **Bloqueia #213** (Apple nativo) — o endpoint `POST /auth/apple` e o contrato desta Task são a base que #213 consome, mesma relação que #65→#192 já teve.
