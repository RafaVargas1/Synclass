# Task: Rate limiting/throttle nos endpoints anônimos de convite (#89)

Card: https://github.com/RafaVargas1/Synclass/issues/89

## Ordem de execução

- [x] Teste de fumaça (Api): `POST /convites/{token}/aceite` devolve
  `429 Too Many Requests` ao exceder o limite configurado (mesmo IP) dentro
  da mesma janela — ainda falha (endpoint sem rate limit configurado).
- [x] Configuração + registro do limiter: leitura obrigatória (fail-fast, mesmo
  padrão de `LerDiasValidadeConviteObrigatoria` em `Program.cs`) de
  `RateLimiting:ConvitesAnonimos:PermissoesPorJanela` e
  `RateLimiting:ConvitesAnonimos:JanelaEmSegundos`; registra
  `builder.Services.AddRateLimiter(...)` com uma policy nomeada
  `ConvitesAnonimos` (fixed window, partição por
  `HttpContext.Connection.RemoteIpAddress`); `app.UseRateLimiter()` no
  pipeline, antes de `app.MapControllers()`.
- [x] Implementação mínima: aplica `[EnableRateLimiting("ConvitesAnonimos")]`
  em `ConvitesController.Aceitar` — faz o teste do item 1 passar.
- [x] Teste de fumaça (Api): `POST /convites/codigo/{codigo}/aceite` devolve
  `429` ao exceder o mesmo limite, na mesma janela — ainda falha.
- [x] Implementação mínima: aplica `[EnableRateLimiting("ConvitesAnonimos")]`
  em `ConvitesController.AceitarPorCodigo` — faz o teste anterior passar.
- [x] Teste de fumaça (Api): dentro do limite configurado, `Aceitar` e
  `AceitarPorCodigo` continuam respondendo normalmente (200 ou 400, conforme
  os cenários já cobertos por `ConvitesEndpointTests`) — prova que o limiter
  não interfere no caso não excedido.
- [x] `OnRejected`: resposta `429` usa o mesmo contrato `ConviteErrorResponse`
  já usado pelos dois endpoints + header `Retry-After` (segundos restantes
  da janela) + log estruturado `ConviteAceiteBloqueadoPorLimite {TrackId}
  {Rota}` (sem IP/payload no log, ver `security-rules.md`). Teste de fumaça
  cobre status/corpo e a presença do header `Retry-After`.
- [x] `appsettings.json`: valores padrão de
  `RateLimiting:ConvitesAnonimos` (`PermissoesPorJanela: 5`,
  `JanelaEmSegundos: 60` — ver decisão em `implementation.md`).
