# Task: Login com Apple na web (#212)

Card: https://github.com/RafaVargas1/Synclass/issues/212

## Ordem de execução

- [x] Teste unidade (Domain): `ValidadorDeIdTokenApple.ValidarAsync` (com `IClienteJwksApple` fake, ver `implementation.md`) devolve `InformacoesIdTokenApple` para um JWT assinado com uma chave de teste presente no JWKS fake
- [x] Teste unidade (Domain): assinatura inválida (JWT assinado com chave que não está no JWKS) devolve `null`
- [x] Teste unidade (Domain): `iss` diferente de `https://appleid.apple.com` devolve `null`
- [x] Teste unidade (Domain): `aud` diferente do `AppleClientId` configurado devolve `null`
- [x] Teste unidade (Domain): token expirado (`exp` no passado) devolve `null`
- [x] Teste unidade (Domain): `email_verified` como string `"true"` (formato real do token da Apple, não booleano) é interpretado corretamente como verificado — edge point, ver `implementation.md#edge-points`
- [x] Implementação: `IValidadorDeIdTokenApple`/`ValidadorDeIdTokenApple` completo (Domain + Infrastructure)
- [x] Teste unidade (Domain): `LoginComAppleService.AutenticarAsync` — usuário existente autentica; e-mail sem conta devolve `CadastroPendente`; e-mail não verificado lança exceção (mesmo padrão de `LoginComGoogleService`)
- [x] Implementação: `LoginComAppleService` (mesma estrutura de `LoginComGoogleService.cs`, sem abstração compartilhada — ver `implementation.md#decisão-sem-serviço-genérico-compartilhado`)
- [x] Implementação: endpoint `POST /auth/apple` em `AutenticacaoController` (mesmo padrão de `POST /auth/google`, mesmo shape de request/response)
- [x] Teste de fumaça (Api): `POST /auth/apple` com token válido de usuário existente → 200 com token de sessão; e-mail sem conta → 200 com `CadastroPendente: true`
- [x] `.env.example` (raiz): adicionar `APPLE_CLIENT_ID` (Services ID da Apple, vazio no exemplo) — lido via `IConfiguration["AppleClientId"]`, mesmo padrão de leitura de `GoogleClientId`
- [x] Frontend: botão "Continuar com Apple" na tela de login/cadastro/Home, ao lado do `BotaoLoginGoogle` já existente — copie o desenho exato de `implementation.md#frontend-botão-e-integração-com-sign-in-with-apple-js`, incluindo a ressalva sobre a forma real de obter o `id_token` (não confirmada com certeza, ver o mesmo arquivo)
- [x] Log estruturado: reaproveita os nomes de evento já usados por `LogCadastroPendente`/`LogLoginGoogleConfirmado` no controller, adaptados pro provedor Apple (mesmo padrão, não invente formato novo — confira os nomes exatos no controller antes de escrever)
- [x] Suíte de testes completa (Domain + Infrastructure + Api + frontend) verde antes do PR

## Decisões já tomadas (não re-abrir)

- **Sem serviço genérico compartilhado entre Google e Apple**: `LoginComAppleService` é uma classe própria, estruturalmente parecida com `LoginComGoogleService.cs` mas não generalizada atrás de uma abstração comum — só dois provedores existem hoje, e cada um tem sua própria mecânica de validação de token (SDK pronto pro Google, validação manual de JWKS pra Apple). Extrair abstração agora seria premature — YAGNI, mesmo racional de `docs/spec/code-style.md`.
- **Apple não emite conta nova, só autentica quem já existe** — mesma RN do Google (#65), sem novidade aqui.
