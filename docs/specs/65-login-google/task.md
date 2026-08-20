# Task: Professor e Aluno entram com login do Google (#65)

Card: https://github.com/RafaVargas1/Synclass/issues/65

## Ordem de execução

- [ ] `IValidadorDeIdTokenGoogle` (Domain, `Synclass.Domain.Autenticacao`):
      interface fina (`Task<InformacoesIdTokenGoogle?> ValidarAsync(string idToken, CancellationToken)`),
      mesmo padrão de `IGeradorDeTokenSessao`/`IClock` (wrapper de terceiro).
      `InformacoesIdTokenGoogle` é um `record` com `Email` e `EmailVerificado`.
      Retorna `null` quando a assinatura/issuer não valida (nunca lança para
      esse caso — token malformado é esperado vindo de um client não
      confiável).
- [ ] `EmailGoogleNaoVerificadoException` (Domain): herda `LoginRejeitadoException`,
      mesma família de `ContatoSemIdentidadePlenaException`.
- [ ] Teste unidade (Domain): `LoginComGoogleService` — token válido, e-mail
      corresponde a usuário existente → retorna `ResultadoLoginGoogle` com
      `Login` preenchido (mesmo `IGeradorDeTokenSessao.Gerar` do login OTP).
- [ ] Teste unidade (Domain): token válido, e-mail não corresponde a
      nenhum usuário → retorna `ResultadoLoginGoogle` com
      `CadastroPendente = true` e `EmailNormalizado` preenchido, `Login = null`
      (nunca cria conta implicitamente).
- [ ] Teste unidade (Domain): `EmailVerificado = false` → lança
      `EmailGoogleNaoVerificadoException`.
- [ ] Teste unidade (Domain): token inválido (`ValidarAsync` retorna `null`)
      → lança `TokenGoogleInvalidoException` (nova, mesma família).
- [ ] `LoginComGoogleService` (Domain): orquestra os 4 cenários acima,
      reaproveitando `Contato.Normalizar` (e-mail sempre bate no mesmo
      formato salvo em `Usuario.Contato`) e `IUsuarioRepository.BuscarPorContatoAsync`
      — sem tocar `LoginService` existente (fluxo paralelo, não substitui OTP).
- [ ] `ValidadorDeIdTokenGoogle` (Infrastructure, `Synclass.Infrastructure.Autenticacao`):
      implementa `IValidadorDeIdTokenGoogle` envolvendo
      `Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync` (pacote NuGet
      `Google.Apis.Auth`), validando `Audience` contra `GoogleClientId`
      (config, ver `.env.example`). Retorna `null` em
      `InvalidJwtException` (não deixa a exceção do SDK vazar pro domínio).
- [ ] `Program.cs`: registra `IValidadorDeIdTokenGoogle` e
      `LoginComGoogleService`; lê `GoogleClientId` de configuração (nova
      variável em `.env.example`, valor vazio).
- [ ] Teste de fumaça (Api): `POST /auth/google` — token válido de usuário
      existente → 200 com `Token`/`UsuarioId`/`Papeis` (mesmo shape de
      `ConfirmarCodigoResponse`).
- [ ] Teste de fumaça (Api): `POST /auth/google` — token válido sem usuário
      correspondente → 200 com `CadastroPendente = true` e `Email`.
- [ ] Teste de fumaça (Api): `POST /auth/google` — `EmailVerificado = false`
      → 400 com mensagem clara.
- [ ] Teste de fumaça (Api): `POST /auth/google` — token inválido → 400.
- [ ] `AutenticacaoController`: novo `POST /auth/google`, mesmo padrão de
      log/TrackId dos dois endpoints existentes (`LoginGoogleConfirmado`/
      `LoginGoogleRejeitado`, contato mascarado no log de rejeição).
- [ ] `.env.example`: adiciona `GOOGLE_CLIENT_ID=` (vazio).
- [ ] Frontend: `frontend/src/lib/auth/google.ts` — wrapper único
      (`Platform.OS`, mesmo padrão de `lib/auth/sessao.ts`) expondo
      `obterIdTokenGoogle(): Promise<string | null>`; nativo via
      `expo-auth-session`/Google Sign-In, web via Google Identity Services.
      Teste de componente/unidade cobrindo só a ramificação testável sem
      browser real (contrato de retorno, tratamento de cancelamento pelo
      usuário → `null`, não exceção).
- [ ] Frontend: `frontend/src/lib/api/auth.ts` — `loginComGoogle(idToken)`,
      mesmo contrato de retorno tipado (`{ sucesso, ... } | { sucesso: false, mensagem }`)
      de `solicitarCodigo`/`confirmarCodigo`, mais o caso
      `{ cadastroPendente: true, email }`.
- [ ] Frontend: `frontend/src/components/molecules/BotaoLoginGoogle.tsx` —
      botão único reaproveitado nas 3 telas (login, cadastro Professor,
      cadastro Aluno), recebe callback `onAutenticado`/`onCadastroPendente`
      via props, teste de componente cobrindo os 3 desfechos (sucesso, vai
      pra cadastro, erro de e-mail não verificado).
- [ ] Frontend: `login/index.tsx` — adiciona `BotaoLoginGoogle`, sucesso
      chama `useSessao().definirSessao` e navega pra `/painel` (mesmo
      destino do OTP); cadastro pendente navega pra
      `/professor/cadastro` ou `/aluno/cadastro` com `?email=` — como o
      login não sabe o papel, mostra as duas opções (mesma escolha que já
      existe na Home hoje).
- [ ] Frontend: `app/professor/cadastro.tsx` e `app/aluno/cadastro.tsx` —
      adiciona `BotaoLoginGoogle` e lê `?email=` da rota pra pré-preencher
      o campo de contato (sem sobrescrever se o usuário já digitou algo).
