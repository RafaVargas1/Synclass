# Task: Entrega de código OTP por WhatsApp (#193)

Card: https://github.com/RafaVargas1/Synclass/issues/193

**Nota de correção de baseline (2026-08-27)**: a versão anterior deste
`task.md`/`implementation.md` usava nomes/caminhos de um layout de
repositório diferente do `main` real (ver histórico do arquivo). Esta
revisão corrige apenas nomenclatura/localização/mecanismo de erro contra o
código real — nenhum critério de aceite, regra de negócio ou decisão de
produto foi alterado. Ver `implementation.md` para o detalhe de cada
correção.

## Ordem de execução

- [ ] Teste unidade (Domain): `INotificador.EnviarCodigoOtpAsync` (assinatura real: `contatoNormalizado, codigo, cancellationToken`) — sucesso, sem retorno de erro
- [ ] Teste unidade (Domain): `INotificador.EnviarCodigoOtpAsync` recebe falha do provedor (HttpRequestException/TaskCanceledException); exceção `OtpEnvioException` com `Motivo` sem detalhe técnico
- [ ] Implementação mínima: `WhatsAppNotificador` (`Synclass.Domain/Autenticacao/`, `Synclass.Infrastructure/Autenticacao/`) com `IWhatsAppHttpClient` mockado (sucesso e falha) — sem rede real
- [ ] Teste unidade (Domain): `TelefoneUtils.NormalizarParaE164` converte formatos variados (com espaço, hífen, com/sem +55) para E.164
- [ ] Implementação mínima: `TelefoneUtils.NormalizarParaE164` (`Synclass.Domain/Autenticacao/`) — edge point explícito do card (RN #193); reutiliza `Synclass.Domain.Usuarios.ContatoInvalidoException` para inválidos
- [ ] Log estruturado: evento `OtpEnviado` (sucesso) não inclui código OTP; `OtpEnvioFalhou` (falha) inclui `Motivo` mas não código — via `_logger.LogInformation`/`LogError` dentro de `WhatsAppNotificador` (sem classe de evento dedicada — mesmo padrão de log estruturado por mensagem já usado em `AutenticacaoController`)
- [ ] Implementação `Infrastructure`: `WhatsAppHttpClient` (wrapper de `HttpClient` com timeout de 3s) e `WhatsAppNotificador` concreto — resolve DI em `Program.cs` (fábrica substituível)
- [ ] Implementação `Api`: `Program.cs` registra `INotificador` real (`WhatsAppNotificador`) por padrão; `NotificadorDeLog` só quando `AssinaturaDigital:ModoDev=true`
- [ ] Configuração: `Program.cs` valida `WhatsApp:ApiKey` e `WhatsApp:NumeroRemetente` no startup (falha explícita — mesmo padrão de `Jwt:SigningKey`); adiciona `WhatsApp__ApiKey=`/`WhatsApp__NumeroRemetente=` (sem valor) ao `.env.example`
- [ ] Teste de fumaça (Api): `POST /auth/codigo` (rota real, `AutenticacaoController`) com contato inválido — retorna 400 `ContatoInvalidoException`; com provedor mockado de falha (`INotificador` substituído via DI no teste) — retorna 502 sem detalhe técnico do provedor
- [ ] Implementação `Api`: `AutenticacaoController.SolicitarCodigo` adiciona `catch (OtpEnvioException ex)` retornando `StatusCode(502, new AutenticacaoErrorResponse(ex.Motivo))` — mesmo padrão local de try/catch já usado ali, sem filtro/middleware global novo
- [ ] Componente frontend: **nenhum** — fluxo permanece na `LoginScreen.tsx` existente, exibindo o erro retornado pela API (sem alteração de layout)

**Questão aberta (não bloqueia implementação, é decisão de provedor)**:
escolher entre Twilio WhatsApp API e WhatsApp Business Cloud API — afeta
`WhatsAppHttpClient` (rota e formato do payload). A implementação parte com
**Twilio como padrão** (SDK maduro, timeout declarável), mas
`IWhatsAppHttpClient` é a fronteira para troca sem tocar `INotificador` ou
`Program.cs` além da fábrica.
