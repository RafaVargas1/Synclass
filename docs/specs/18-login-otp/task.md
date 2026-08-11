# Task: Professor ou Aluno autentica-se na plataforma (login) (#18)

Card: https://github.com/RafaVargas1/Synclass/issues/18

## Ordem de execução

### Domain

- [x] Teste unidade (Domain): `HashDeCodigoOtp` gera hash estável e nunca
      expõe o código em texto puro.
- [x] Implementação mínima: `HashDeCodigoOtp` (SHA-256).
- [x] Teste unidade (Domain): `CodigoOtp.Gerar` calcula `ExpiraEm` = agora +
      10min e nasce não usado; `Corresponde`/`Expirado`/`Invalidar`.
- [x] Implementação mínima: entidade `CodigoOtp`.
- [x] Teste unidade (Domain): `LoginService.SolicitarCodigoAsync` — contato
      sem identidade plena (usuário inexistente ou sem papel) rejeita com
      `ContatoSemIdentidadePlenaException`, sem criar código.
- [x] Teste unidade (Domain): `LoginService.SolicitarCodigoAsync` — contato
      com identidade plena gera código, persiste via
      `ICodigoOtpRepository`, e aciona `INotificador`.
- [x] Teste unidade (Domain): `LoginService.SolicitarCodigoAsync` — segunda
      solicitação antes da expiração invalida o código anterior (só um
      código válido por vez).
- [x] Implementação mínima: `LoginService.SolicitarCodigoAsync` +
      `ContatoSemIdentidadePlenaException`.
- [x] Teste unidade (Domain): `LoginService.ConfirmarCodigoAsync` — código
      correto dentro do prazo gera sessão (token) e invalida o código
      (uso único).
- [x] Teste unidade (Domain): `LoginService.ConfirmarCodigoAsync` — código
      incorreto, expirado, ou já usado rejeita com
      `CodigoOtpInvalidoException`, sem gerar token.
- [x] Teste unidade (Domain): `LoginService.ConfirmarCodigoAsync` — contato
      sem identidade plena rejeita com `ContatoSemIdentidadePlenaException`.
- [x] Implementação mínima: `LoginService.ConfirmarCodigoAsync` +
      `CodigoOtpInvalidoException` + `IGeradorDeTokenSessao`.

### Infrastructure

- [x] Migration `CriaCodigoOtp`: tabela `CodigosOtp` (`Id`, `UsuarioId` FK →
      `Usuarios`, `CodigoHash`, `ExpiraEm`, `UsadoEm` nullable, `CreatedAt`)
      + `CodigoOtpConfiguration`.
- [x] Implementação: `CodigoOtpRepository` (EF Core).
- [x] Implementação: `GeradorDeCodigoOtp` (`RandomNumberGenerator`, 6 dígitos
      numéricos).
- [x] Implementação: `NotificadorDeLog` (`INotificador` — loga o código em
      ambiente de dev, conforme autorizado explicitamente pelo card).
- [x] Implementação: `GeradorDeTokenSessaoJwt` (`IGeradorDeTokenSessao` —
      JWT HMAC-SHA256, papéis embutidos como claims, expiração de 30 dias).

### Api

- [ ] Teste de fumaça (Api): `POST /auth/codigo` — 200 quando contato tem
      identidade plena; rejeita (mesma mensagem genérica) para contato sem
      identidade plena ou nunca cadastrado.
- [ ] Teste de fumaça (Api): `POST /auth/confirmacao` — 200 + token quando
      código correto; rejeita código incorreto/expirado/reutilizado.
- [ ] Implementação: `AutenticacaoController` + DTOs de request/response.
- [ ] Log estruturado: `CodigoOtpSolicitado` (Information, nunca o código),
      `LoginConfirmado` (Information, papéis), `LoginRejeitado` (Warning,
      motivo) — ver `architecture.md#logs-estruturados-e-track-id`.
- [ ] Wiring de DI (`Program.cs`) + configuração `Jwt:SigningKey`/
      `Jwt:ExpiracaoDias` em `appsettings*.json`.

### Frontend

- [ ] Teste (`lib/api/auth.ts`): `solicitarCodigo` e `confirmarCodigo`
      contra o contrato já estabilizado no backend (sucesso, erro de
      negócio, erro de conexão/timeout).
- [ ] Implementação: `lib/api/auth.ts`.
- [ ] Teste (`lib/auth/sessao.ts`): salvar/ler/limpar token em
      armazenamento seguro (`expo-secure-store`).
- [ ] Implementação: `lib/auth/sessao.ts`.
- [ ] Teste de componente: `SolicitarCodigoForm` (organism) — estados
      enviando/erro.
- [ ] Implementação: `SolicitarCodigoForm`.
- [ ] Teste de componente: `VerificarCodigoForm` (organism) — estados
      enviando/erro/reenvio.
- [ ] Implementação: `VerificarCodigoForm`.
- [ ] Teste de tela: `app/login/index.tsx` (solicitar contato).
- [ ] Implementação: `app/login/index.tsx`.
- [ ] Teste de tela: `app/login/verificar.tsx` (confirmar código, persiste
      sessão, mostra confirmação).
- [ ] Implementação: `app/login/verificar.tsx`.
- [ ] Entrada de navegação: link "Entrar" na Home (`HomeTemplate`/`HomeHero`)
      para `/login`.
